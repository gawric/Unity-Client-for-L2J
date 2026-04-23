using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CompositePrefabPart
{
    public string name;
    public BaseEffect prefab;
    public EffectSettings settingsOverride;
    public EffectAttachmentPoint attachmentPoint = EffectAttachmentPoint.CasterRoot;
    public CompositePartSpawnTiming spawnTiming = CompositePartSpawnTiming.Immediate;
    public float manualDelaySeconds = 0f;
    // Local offset from resolved attachment point (in attachment transform space if available).
    public Vector3 positionOffset = Vector3.zero;
    // Scales positionOffset by model height to keep visual placement consistent across races.
    public bool normalizeOffsetByOwnerHeight = false;
    public float referenceHeight = 1.8f;
    public float scale = 1f;
    public bool followResolvedTransform = true;
    public bool inheritRotation = true;
}

public class CompositePrefabEffect : TimedCompositeEffectBase
{
    [SerializeField] private CompositePrefabPart[] _parts;

    private readonly IEffectAttachmentResolver _resolver = new DefaultEffectAttachmentResolver();
    private EffectResolveContext _context;
    private readonly List<PendingCompositePart> _pendingParts = new List<PendingCompositePart>();
    private Coroutine _pendingSpawnRoutine;
    protected override string DebugPrefix => "[CompositePrefabEffect]";

    private sealed class PendingCompositePart
    {
        public CompositePrefabPart Part;
        public float SpawnAtTime;
    }

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        base.Setup(settings, castData, owner);
        InitializeTimedComposite(settings, castData);
        _context = CompositeEffectUtilities.BuildContext(owner, castData);
    }

    public override void Play()
    {

        if (_parts == null || _parts.Length == 0)
        {
            Debug.LogWarning("CompositePrefabEffect: no parts configured.");
            return;
        }
    
        QueueImmediateAndDelayedParts();
        StartPendingPartsRoutineIfNeeded();
        DestroyCompositeByLifetime();
    }

    public override void SetProgress(float normalizedTime)
    {
        // Composite root delegates playback to spawned child effects.
    }

    private void SpawnPart(CompositePrefabPart part)
    {
        if (!IsPartSpawnable(part))
        {
            return;
        }

        if (!TryResolveAttachment(part, out Transform resolvedTransform, out Vector3 worldPosition))
        {
            return;
        }

        BaseEffect instance = SpawnPartInstance(part, resolvedTransform, worldPosition);
        if (instance == null)
        {
            return;
        }

        if (!TryResolvePartSettings(part, out EffectSettings partSettings))
        {
            return;
        }

        Transform setupOwner = ResolveSetupOwner(resolvedTransform, instance.transform);
        instance.Setup(partSettings, _castData, setupOwner);
        instance.Play();

        LogSpawnedPart(part, partSettings, setupOwner);
    }

    private bool IsPartSpawnable(CompositePrefabPart part)
    {
        return part != null && part.prefab != null;
    }

    private bool TryResolveAttachment(CompositePrefabPart part, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        if (_resolver.Resolve(part.attachmentPoint, _context, out resolvedTransform, out worldPosition))
        {
            return true;
        }

        Debug.LogWarning($"CompositePrefabEffect: could not resolve point {part.attachmentPoint} for part {part.name}.");
        return false;
    }

    private BaseEffect SpawnPartInstance(CompositePrefabPart part, Transform resolvedTransform, Vector3 worldPosition)
    {
        Vector3 adjustedOffset = GetAdjustedOffset(part, resolvedTransform);
        Vector3 spawnPosition = CompositeEffectUtilities.ResolveSpawnPosition(
            resolvedTransform,
            worldPosition,
            adjustedOffset);
        Quaternion rotation = CompositeEffectUtilities.ResolveSpawnRotation(part.inheritRotation, resolvedTransform);

        // Spawn without parent first, then attach with worldPositionStays=true.
        // This prevents inheriting oversized bone scale when following transforms.
        BaseEffect instance = Instantiate(part.prefab, spawnPosition, rotation);
        instance.gameObject.SetActive(true);

        AttachToResolvedTransformIfNeeded(part, resolvedTransform, instance.transform, adjustedOffset);
        ApplyPartScale(part, instance.transform);

        return instance;
    }

    private void AttachToResolvedTransformIfNeeded(
        CompositePrefabPart part,
        Transform resolvedTransform,
        Transform instanceTransform,
        Vector3 adjustedOffset)
    {
        if (!part.followResolvedTransform || resolvedTransform == null)
        {
            return;
        }

        instanceTransform.SetParent(resolvedTransform, true);
        // Keep stable offset while following attachment point movement/rotation.
        instanceTransform.localPosition = adjustedOffset;
    }

    private void ApplyPartScale(CompositePrefabPart part, Transform instanceTransform)
    {
        if (Mathf.Approximately(part.scale, 1f))
        {
            return;
        }

        instanceTransform.localScale *= part.scale;
    }

    private Transform ResolveSetupOwner(Transform resolvedTransform, Transform instanceTransform)
    {
        return resolvedTransform != null ? resolvedTransform : (_owner != null ? _owner : instanceTransform);
    }

    private bool TryResolvePartSettings(CompositePrefabPart part, out EffectSettings partSettings)
    {
        EffectSettings sourceSettings = part.settingsOverride != null ? part.settingsOverride : _settings;
        partSettings = CreateRuntimeSettings(sourceSettings);
        if (partSettings != null)
        {
            return true;
        }

        Debug.LogWarning($"CompositePrefabEffect: settings are null for part {part.name}.");
        return false;
    }

    private void LogSpawnedPart(CompositePrefabPart part, EffectSettings partSettings, Transform setupOwner)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Spawned part='{part.name}' point={part.attachmentPoint} " +
            $"follow={part.followResolvedTransform} hitTime={(_castData != null ? _castData.HitTime : -1f):F3}s " +
            $"lifeTime={partSettings.defaultLifeTime:F3}s scale={part.scale:F2} offset={part.positionOffset} owner='{(setupOwner != null ? setupOwner.name : "null")}'.");
#endif
    }

    private Vector3 GetAdjustedOffset(CompositePrefabPart part, Transform resolvedTransform)
    {
        if (part == null || !part.normalizeOffsetByOwnerHeight)
        {
            return part != null ? part.positionOffset : Vector3.zero;
        }

        float height = ResolveAttachmentHeight(resolvedTransform);
        float reference = Mathf.Max(0.01f, part.referenceHeight);
        float multiplier = Mathf.Max(0.01f, height / reference);
        return part.positionOffset * multiplier;
    }

    private float ResolveAttachmentHeight(Transform resolvedTransform)
    {
        Transform basis = resolvedTransform != null ? resolvedTransform : _owner;
        if (basis == null)
        {
            return 1.8f;
        }

        CharacterController controller = basis.GetComponentInParent<CharacterController>();
        if (controller != null && controller.height > 0f)
        {
            return controller.height;
        }

        Renderer[] renderers = basis.GetComponentsInParent<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.y > 0.01f)
            {
                return bounds.size.y;
            }
        }

        return 1.8f;
    }

    private void QueueImmediateAndDelayedParts()
    {
        _pendingParts.Clear();

        for (int i = 0; i < _parts.Length; i++)
        {
            CompositePrefabPart part = _parts[i];
            if (part == null || part.prefab == null)
            {
                continue;
            }

            float delay = CompositeEffectUtilities.ResolveSpawnDelay(
                part.spawnTiming,
                part.manualDelaySeconds,
                _castData);

            if (delay <= 0f)
            {
                SpawnPart(part);
                continue;
            }

            _pendingParts.Add(new PendingCompositePart
            {
                Part = part,
                SpawnAtTime = Time.time + delay
            });

        }
    }

    private void StartPendingPartsRoutineIfNeeded()
    {
        if (_pendingParts.Count > 0)
        {
            _pendingSpawnRoutine = StartCoroutine(SpawnPendingPartsRoutine());
        }
    }

    private void DestroyCompositeByLifetime()
    {
        EffectSettings lifeTimeSettings = SelectLifetimeSettings();
        if (lifeTimeSettings != null)
        {
            DestoryEffect(lifeTimeSettings, _castData);
        }
    }

    private IEnumerator SpawnPendingPartsRoutine()
    {
        while (_pendingParts.Count > 0)
        {
            float now = Time.time;
            for (int i = _pendingParts.Count - 1; i >= 0; i--)
            {
                PendingCompositePart pending = _pendingParts[i];
                if (pending == null || pending.Part == null)
                {
                    _pendingParts.RemoveAt(i);
                    continue;
                }

                if (now >= pending.SpawnAtTime)
                {
                    SpawnPart(pending.Part);
                    _pendingParts.RemoveAt(i);
                }
            }

            yield return null;
        }

        _pendingSpawnRoutine = null;
    }

    private void OnDestroy()
    {
        if (_pendingSpawnRoutine != null)
        {
            StopCoroutine(_pendingSpawnRoutine);
            _pendingSpawnRoutine = null;
        }

        _pendingParts.Clear();
        CleanupRuntimeSettings();
    }
}
