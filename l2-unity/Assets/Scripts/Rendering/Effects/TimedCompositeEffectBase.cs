using System.Collections.Generic;
using System;
using UnityEngine;

public abstract class TimedCompositeEffectBase : BaseEffect
{
    protected class PendingCompositePart
    {
        public CompositePrefabPart Part;
        public float SpawnAtTime;
    }

    protected EffectSettings _settings;
    protected MagicCastData _castData;
    protected EffectSettings _rootRuntimeSettings;

    private readonly List<EffectSettings> _runtimeSettings = new List<EffectSettings>();

    protected abstract string DebugPrefix { get; }
    protected virtual float RuntimeLifeTimeTailSeconds => 0f;

    protected void InitializeTimedComposite(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        _rootRuntimeSettings = CreateRuntimeSettings(settings);
    }

    protected EffectSettings CreateRuntimeSettings(EffectSettings sourceSettings, bool applyTimedLifetime = true)
    {
        if (sourceSettings == null)
        {
            return null;
        }

        EffectSettings runtime = Instantiate(sourceSettings);
        _runtimeSettings.Add(runtime);

        if (!applyTimedLifetime || _castData == null || _castData.HitTime <= 0f)
        {
            return runtime;
        }

        runtime.defaultLifeTime = _castData.HitTime + Mathf.Max(0f, RuntimeLifeTimeTailSeconds);
        runtime.hideTime = Mathf.Min(runtime.hideTime, runtime.defaultLifeTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Runtime settings cloned. Source='{sourceSettings.name}' " +
            $"timed={applyTimedLifetime} " +
            $"hitTime={_castData.HitTime:F3}s tail={RuntimeLifeTimeTailSeconds:F3}s " +
            $"lifeTime={runtime.defaultLifeTime:F3}s hideTime={runtime.hideTime:F3}s.");
#endif

        return runtime;
    }

    protected EffectSettings SelectLifetimeSettings()
    {
        return _rootRuntimeSettings != null ? _rootRuntimeSettings : _settings;
    }

    protected void CleanupRuntimeSettings()
    {
        for (int i = 0; i < _runtimeSettings.Count; i++)
        {
            if (_runtimeSettings[i] != null)
            {
                Destroy(_runtimeSettings[i]);
            }
        }

        _runtimeSettings.Clear();
        _rootRuntimeSettings = null;
    }

    protected void StopAndClearCoroutine(ref Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        StopCoroutine(routine);
        routine = null;
    }

    protected void UnsubscribeShootEventSources(
        List<AnimationEventsBase> shootEventSources,
        Action<string> onAnimationShootHandler,
        ref AnimationEventsBase animationEvents,
        ref bool isSubscribedToAnyShoot
       )
    {
        if (shootEventSources != null)
        {
            for (int i = 0; i < shootEventSources.Count; i++)
            {
                AnimationEventsBase source = shootEventSources[i];
                if (source != null)
                {
                    Debug.Log("UnsubscribeShootEventSources: use un");
                    source.OnAnimationStartShoot -= onAnimationShootHandler;
                }
            }

            shootEventSources.Clear();
        }

        animationEvents = null;

        if (isSubscribedToAnyShoot)
        {
            //AnimationEventsBase.OnAnyAnimationShoot -= onAnyAnimationShootHandler;
            isSubscribedToAnyShoot = false;
        }
    }

    protected virtual void OnTimedCompositeDestroy()
    {
    }

    protected float ResolveAttachmentHeight(Transform resolvedTransform)
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

    protected void QueueImmediateAndDelayedParts(
        CompositePrefabPart[] parts,
        List<PendingCompositePart> pendingParts,
        MagicCastData castData,
        Action<CompositePrefabPart> spawnPart)
    {
        if (pendingParts == null || spawnPart == null)
        {
            return;
        }

        pendingParts.Clear();
        if (parts == null)
        {
            return;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            CompositePrefabPart part = parts[i];
            if (part == null || part.prefab == null)
            {
                continue;
            }

            float delay = CompositeEffectUtilities.ResolveSpawnDelay(part.spawnTiming, castData);
            if (delay <= 0f)
            {
                spawnPart(part);
                continue;
            }

            pendingParts.Add(new PendingCompositePart
            {
                Part = part,
                SpawnAtTime = Time.time + delay
            });
        }
    }

    protected void ApplyPartScale(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || instanceTransform == null || Mathf.Approximately(part.scale, 1f))
        {
            return;
        }

        instanceTransform.localScale *= part.scale;
    }

    protected void ApplyPartLoopOverrides(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || instanceTransform == null)
        {
            return;
        }

        ParticleGroup[] groups = instanceTransform.GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            ParticleGroup group = groups[i];
            if (group != null)
            {
                group.SetRuntimeContinuousLoopOverride(part.overrideContinuousLoop, part.continuousLoop);
            }
        }

        ParticleSingle[] singles = instanceTransform.GetComponentsInChildren<ParticleSingle>(true);
        for (int i = 0; i < singles.Length; i++)
        {
            ParticleSingle single = singles[i];
            if (single != null)
            {
                single.SetRuntimeContinuousLoopOverride(part.overrideContinuousLoop, part.continuousLoop);
            }
        }
    }

    protected void AttachToResolvedTransformIfNeeded(
        CompositePrefabPart part,
        Transform resolvedTransform,
        Transform instanceTransform,
        Vector3 adjustedOffset,
        Vector3 resolvedWorldPosition)
    {
        if (part == null || instanceTransform == null || !part.followResolvedTransform || resolvedTransform == null)
        {
            return;
        }

        instanceTransform.SetParent(resolvedTransform, true);
        // Match spawn math in CompositeEffectUtilities.ResolveSpawnPosition: anchor in world can differ from follow pivot.
        Vector3 localAnchor = resolvedTransform.InverseTransformPoint(resolvedWorldPosition);
        instanceTransform.localPosition = localAnchor + adjustedOffset;
    }

    protected override void OnDestroy()
    {
        OnTimedCompositeDestroy();
        CleanupRuntimeSettings();
        base.OnDestroy();
    }
}

