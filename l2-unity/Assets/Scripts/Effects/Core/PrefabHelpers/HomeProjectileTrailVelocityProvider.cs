using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomeProjectileTrailVelocityProvider : MonoBehaviour
{
    [Serializable]
    public sealed class TrailBinding
    {
        public string name;
        public Transform tailRoot;
        public Transform velocitySource;
        public string velocitySourceName = "SpriteEmitter5";
        public Renderer[] targetRenderers;
        public bool autoCollectChildren = true;
        public bool followSourcePosition = true;
        public bool placeRenderersOnHistory = true;
        public float historySeconds = 0.28f;
        [Range(0f, 0.5f)]
        public float headLagPercent = 0.05f;
        public bool useCylinderSpread = true;
        public float cylinderRadiusHead = 0.008f;
        public float cylinderRadiusTail = 0.04f;
        [Range(0.5f, 3f)]
        public float cylinderRadiusPower = 1.6f;
        public bool scaleOverTrail = true;
        public float particleScaleHead = 0.45f;
        public float particleScaleTail = 1.2f;
        [Range(0.5f, 3f)]
        public float particleScalePower = 1.4f;
        [Range(0.3f, 1.5f)]
        public float alongTrailPower = 0.75f;
        public bool placeByParticleAge = true;
        public float trailTravelSeconds = 0.333f;
        public bool fadeAlphaOverTrail = true;
        [Range(0f, 1f)]
        public float trailFadeHead = 1f;
        [Range(0f, 1f)]
        public float trailFadeTail = 0f;
        [Range(0.5f, 3f)]
        public float trailFadePower = 1.25f;
        public bool onlyActiveRenderers = false;
        public bool convertWorldToLocal = true;
        public Transform localSpaceReference;
        public float velocityScale = 0.08f;
        public float rangeSpread = 0.2f;
        public bool invertTrailDirection = true;
        public float trailSign = 1f;

        [NonSerialized] public Vector3 lastPosition;
        [NonSerialized] public Vector3 smoothedVelocity;
        [NonSerialized] public bool hasLastPosition;
        [NonSerialized] public List<TrailSample> history;
        [NonSerialized] public Dictionary<Transform, Vector3> baseLocalScales;
    }

    public struct TrailSample
    {
        public float time;
        public float distance;
        public Vector3 position;
    }

    [SerializeField] private float _minimumAxisRange = 0.002f;
    [SerializeField] private float _smoothing = 18f;
    [SerializeField] private bool _debugLogs;
    [SerializeField] private List<TrailBinding> _bindings = new List<TrailBinding>();

    public void CopySettingsFrom(HomeProjectileTrailVelocityProvider other)
    {
        if (other == null)
        {
            return;
        }

        _minimumAxisRange = other._minimumAxisRange;
        _smoothing = other._smoothing;
        _debugLogs = other._debugLogs;
        _bindings = new List<TrailBinding>();
        if (other._bindings == null)
        {
            return;
        }

        for (int i = 0; i < other._bindings.Count; i++)
        {
            TrailBinding source = other._bindings[i];
            if (source == null)
            {
                continue;
            }

            _bindings.Add(CloneBinding(source));
        }
    }

    public void ResetRuntimeState()
    {
        OnEnable();
    }

    public static void MoveFromRootToFlightRoot(Transform root, Transform flightRoot)
    {
        if (root == null || flightRoot == null)
        {
            return;
        }

        HomeProjectileTrailVelocityProvider source = root.GetComponent<HomeProjectileTrailVelocityProvider>();
        if (source == null)
        {
            return;
        }

        HomeProjectileTrailVelocityProvider dest = flightRoot.GetComponent<HomeProjectileTrailVelocityProvider>();
        if (dest == null)
        {
            dest = flightRoot.gameObject.AddComponent<HomeProjectileTrailVelocityProvider>();
        }

        dest.CopySettingsFrom(source);
        dest.RetargetBindingsTo(flightRoot);
        dest.ResetRuntimeState();
        source.enabled = false;
        UnityEngine.Object.Destroy(source);
    }

    public void RetargetBindingsTo(Transform searchRoot)
    {
        if (searchRoot == null || _bindings == null)
        {
            return;
        }

        for (int i = 0; i < _bindings.Count; i++)
        {
            TrailBinding binding = _bindings[i];
            if (binding == null)
            {
                continue;
            }

            string tailName = binding.tailRoot != null ? binding.tailRoot.name : "SpriteEmitter2";
            string coreName = string.IsNullOrEmpty(binding.velocitySourceName)
                ? "SpriteEmitter5"
                : binding.velocitySourceName;
            binding.tailRoot = FindChildByName(searchRoot, tailName);
            binding.velocitySource = FindChildByName(searchRoot, coreName);
            binding.localSpaceReference = binding.tailRoot;
            binding.targetRenderers = null;
            binding.hasLastPosition = false;
            binding.history = null;
            binding.baseLocalScales = null;
        }
    }

    private static Transform FindChildByName(Transform searchRoot, string childName)
    {
        if (searchRoot == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (searchRoot.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
        {
            return searchRoot;
        }

        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static TrailBinding CloneBinding(TrailBinding source)
    {
        return new TrailBinding
        {
            name = source.name,
            tailRoot = source.tailRoot,
            velocitySource = source.velocitySource,
            velocitySourceName = source.velocitySourceName,
            targetRenderers = null,
            autoCollectChildren = source.autoCollectChildren,
            followSourcePosition = source.followSourcePosition,
            placeRenderersOnHistory = source.placeRenderersOnHistory,
            historySeconds = source.historySeconds,
            headLagPercent = source.headLagPercent,
            useCylinderSpread = source.useCylinderSpread,
            cylinderRadiusHead = source.cylinderRadiusHead,
            cylinderRadiusTail = source.cylinderRadiusTail,
            cylinderRadiusPower = source.cylinderRadiusPower,
            scaleOverTrail = source.scaleOverTrail,
            particleScaleHead = source.particleScaleHead,
            particleScaleTail = source.particleScaleTail,
            particleScalePower = source.particleScalePower,
            alongTrailPower = source.alongTrailPower,
            placeByParticleAge = source.placeByParticleAge,
            trailTravelSeconds = source.trailTravelSeconds,
            fadeAlphaOverTrail = source.fadeAlphaOverTrail,
            trailFadeHead = source.trailFadeHead,
            trailFadeTail = source.trailFadeTail,
            trailFadePower = source.trailFadePower,
            onlyActiveRenderers = source.onlyActiveRenderers,
            convertWorldToLocal = source.convertWorldToLocal,
            localSpaceReference = source.localSpaceReference,
            velocityScale = source.velocityScale,
            rangeSpread = source.rangeSpread,
            invertTrailDirection = source.invertTrailDirection,
            trailSign = source.trailSign
        };
    }

    private const float GoldenAngleRad = 2.39996323f;
    private static readonly int StartVelocityRangeXID = Shader.PropertyToID("_StartVelocityRangeX");
    private static readonly int StartVelocityRangeYID = Shader.PropertyToID("_StartVelocityRangeY");
    private static readonly int StartVelocityRangeZID = Shader.PropertyToID("_StartVelocityRangeZ");
    private static readonly int TrailPathFadeTID = Shader.PropertyToID("_TrailPathFadeT");
    private static readonly int TrailPathFadeHeadID = Shader.PropertyToID("_TrailPathFadeHead");
    private static readonly int TrailPathFadeTailID = Shader.PropertyToID("_TrailPathFadeTail");
    private static readonly int StartTimeID = Shader.PropertyToID("_StartTime");
    private static readonly int LifetimeRangeID = Shader.PropertyToID("_LifetimeRange");

    private MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            TrailBinding binding = _bindings[i];
            if (binding == null || binding.tailRoot == null)
            {
                continue;
            }

            ResolveBinding(binding);
            binding.hasLastPosition = false;
            binding.smoothedVelocity = Vector3.zero;
            binding.history = new List<TrailSample>(32);
            binding.baseLocalScales = new Dictionary<Transform, Vector3>();
            CacheBaseLocalScales(binding);
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        for (int i = 0; i < _bindings.Count; i++)
        {
            TrailBinding binding = _bindings[i];
            if (binding == null || binding.tailRoot == null)
            {
                continue;
            }

            ResolveBinding(binding);
            CacheBaseLocalScales(binding);
            if (binding.velocitySource == null)
            {
                continue;
            }

            if (binding.followSourcePosition)
            {
                binding.tailRoot.position = binding.velocitySource.position;
            }

            Vector3 sourcePosition = binding.velocitySource.position;
            RecordHistory(binding, sourcePosition);
            if (!binding.hasLastPosition)
            {
                binding.lastPosition = sourcePosition;
                binding.hasLastPosition = true;
                PositionRenderersOnHistory(binding);
                continue;
            }

            Vector3 rawVelocity = (sourcePosition - binding.lastPosition) / dt;
            binding.lastPosition = sourcePosition;

            float lerpT = Mathf.Clamp01(_smoothing * dt);
            binding.smoothedVelocity = Vector3.Lerp(binding.smoothedVelocity, rawVelocity, lerpT);

            ApplyVelocity(binding);
            PositionRenderersOnHistory(binding);
        }
    }

    private void ResolveBinding(TrailBinding binding)
    {
        if (binding.autoCollectChildren)
        {
            Renderer[] childRenderers = binding.tailRoot.GetComponentsInChildren<Renderer>(true);
            if (binding.targetRenderers == null || binding.targetRenderers.Length != childRenderers.Length)
            {
                binding.targetRenderers = childRenderers;
            }
        }

        if (binding.localSpaceReference == null)
        {
            binding.localSpaceReference = binding.tailRoot;
        }

        if (binding.velocitySource != null || string.IsNullOrEmpty(binding.velocitySourceName))
        {
            return;
        }

        Transform searchRoot = binding.tailRoot.parent != null ? binding.tailRoot.parent : transform;
        Transform[] candidates = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate != null && candidate.name.Equals(binding.velocitySourceName, StringComparison.OrdinalIgnoreCase))
            {
                binding.velocitySource = candidate;
                return;
            }
        }
    }

    private void RecordHistory(TrailBinding binding, Vector3 sourcePosition)
    {
        if (binding.history == null)
        {
            binding.history = new List<TrailSample>(32);
        }

        float now = Time.time;
        float distance = 0f;
        if (binding.history.Count > 0)
        {
            TrailSample last = binding.history[binding.history.Count - 1];
            distance = last.distance + Vector3.Distance(last.position, sourcePosition);
        }

        binding.history.Add(new TrailSample { time = now, distance = distance, position = sourcePosition });

        float keepSeconds = Mathf.Max(0.02f, binding.historySeconds * 1.5f);
        float oldestAllowed = now - keepSeconds;
        while (binding.history.Count > 1 && binding.history[0].time < oldestAllowed)
        {
            binding.history.RemoveAt(0);
        }
    }

    private void PositionRenderersOnHistory(TrailBinding binding)
    {
        if (!binding.placeRenderersOnHistory || binding.targetRenderers == null || binding.history == null || binding.history.Count == 0)
        {
            return;
        }

        List<Renderer> renderers = CollectPositionedRenderers(binding);
        int count = renderers.Count;
        if (count == 0)
        {
            return;
        }

        float headLag = Mathf.Clamp01(binding.headLagPercent);
        float alongPower = Mathf.Max(0.01f, binding.alongTrailPower);
        float radiusPower = Mathf.Max(0.01f, binding.cylinderRadiusPower);
        float latestDistance = binding.history[binding.history.Count - 1].distance;
        float oldestDistance = binding.history[0].distance;
        float availableDistance = Mathf.Max(0.001f, latestDistance - oldestDistance);

        for (int i = 0; i < count; i++)
        {
            float fallbackLinearT = count <= 1 ? headLag : Mathf.Lerp(headLag, 1f, (float)i / (count - 1));
            float linearT = GetRendererTrailT(binding, renderers[i], fallbackLinearT, headLag);
            float trailT = Mathf.Pow(linearT, alongPower);
            float sampleDistance = latestDistance - availableDistance * trailT;
            Vector3 center = SampleHistoryByDistance(binding, sampleDistance);
            float spreadT = headLag >= 1f - 1e-4f
                ? 1f
                : Mathf.Clamp01((linearT - headLag) / (1f - headLag));

            ApplyTrailScale(binding, renderers[i].transform, spreadT);
            ApplyTrailPathFade(binding, renderers[i], spreadT);

            if (!binding.useCylinderSpread)
            {
                renderers[i].transform.position = center;
                continue;
            }

            float radius = Mathf.Lerp(
                binding.cylinderRadiusHead,
                binding.cylinderRadiusTail,
                Mathf.Pow(spreadT, radiusPower));

            BuildTrailPerpendicularBasis(GetTrailTangentByDistance(binding, sampleDistance), out Vector3 right, out Vector3 up);
            float angle = (i + 0.5f) * GoldenAngleRad;
            Vector3 radial = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            renderers[i].transform.position = center + radial;
        }
    }

    private List<Renderer> CollectPositionedRenderers(TrailBinding binding)
    {
        List<Renderer> result = new List<Renderer>(binding.targetRenderers.Length);
        for (int i = 0; i < binding.targetRenderers.Length; i++)
        {
            Renderer renderer = binding.targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (binding.onlyActiveRenderers &&
                !renderer.gameObject.activeInHierarchy &&
                !renderer.enabled)
            {
                continue;
            }

            result.Add(renderer);
        }

        return result;
    }

    private float GetRendererTrailT(TrailBinding binding, Renderer renderer, float fallbackLinearT, float headLag)
    {
        if (!binding.placeByParticleAge || renderer == null)
        {
            return fallbackLinearT;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        renderer.GetPropertyBlock(_propertyBlock);
        float startTime = _propertyBlock.GetFloat(StartTimeID);
        Material material = renderer.sharedMaterial;
        if (startTime <= 0f)
        {
            if (material == null || !material.HasProperty(StartTimeID))
            {
                return fallbackLinearT;
            }

            startTime = material.GetFloat(StartTimeID);
            if (startTime <= 0f)
            {
                return fallbackLinearT;
            }
        }

        float age = Mathf.Max(0f, Time.time - startTime);
        float travelSeconds = binding.trailTravelSeconds;
        if (travelSeconds <= 0f && material != null && material.HasProperty(LifetimeRangeID))
        {
            travelSeconds = material.GetVector(LifetimeRangeID).y;
        }

        float ageT = Mathf.Clamp01(age / Mathf.Max(0.001f, travelSeconds));
        return Mathf.Lerp(headLag, 1f, ageT);
    }

    private void CacheBaseLocalScales(TrailBinding binding)
    {
        if (binding.targetRenderers == null)
        {
            return;
        }

        if (binding.baseLocalScales == null)
        {
            binding.baseLocalScales = new Dictionary<Transform, Vector3>();
        }
        for (int i = 0; i < binding.targetRenderers.Length; i++)
        {
            Renderer renderer = binding.targetRenderers[i];
            if (renderer == null || renderer.transform == null)
            {
                continue;
            }

            if (!binding.baseLocalScales.ContainsKey(renderer.transform))
            {
                binding.baseLocalScales.Add(renderer.transform, renderer.transform.localScale);
            }
        }
    }

    private void ApplyTrailPathFade(TrailBinding binding, Renderer renderer, float spreadT)
    {
        if (!binding.fadeAlphaOverTrail || renderer == null)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        float fadeT = Mathf.Pow(Mathf.Clamp01(spreadT), Mathf.Max(0.01f, binding.trailFadePower));
        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(TrailPathFadeTID, fadeT);
        _propertyBlock.SetFloat(TrailPathFadeHeadID, binding.trailFadeHead);
        _propertyBlock.SetFloat(TrailPathFadeTailID, binding.trailFadeTail);
        renderer.SetPropertyBlock(_propertyBlock);
    }

    private static void ApplyTrailScale(TrailBinding binding, Transform target, float spreadT)
    {
        if (!binding.scaleOverTrail || target == null)
        {
            return;
        }

        Vector3 baseScale = target.localScale;
        if (binding.baseLocalScales != null && binding.baseLocalScales.TryGetValue(target, out Vector3 cachedScale))
        {
            baseScale = cachedScale;
        }

        float scaleT = Mathf.Pow(Mathf.Clamp01(spreadT), Mathf.Max(0.01f, binding.particleScalePower));
        float scale = Mathf.Lerp(binding.particleScaleHead, binding.particleScaleTail, scaleT);
        target.localScale = baseScale * Mathf.Max(0.01f, scale);
    }

    private static void BuildTrailPerpendicularBasis(Vector3 tangent, out Vector3 right, out Vector3 up)
    {
        if (tangent.sqrMagnitude < 1e-8f)
        {
            tangent = Vector3.forward;
        }
        else
        {
            tangent = tangent.normalized;
        }

        Vector3 worldUp = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
        right = Vector3.Cross(worldUp, tangent);
        if (right.sqrMagnitude < 1e-8f)
        {
            right = Vector3.right;
        }
        else
        {
            right.Normalize();
        }

        up = Vector3.Cross(tangent, right).normalized;
    }

    private Vector3 GetTrailTangentByDistance(TrailBinding binding, float sampleDistance)
    {
        const float delta = 0.01f;
        Vector3 before = SampleHistoryByDistance(binding, sampleDistance - delta);
        Vector3 after = SampleHistoryByDistance(binding, sampleDistance + delta);
        Vector3 deltaPos = after - before;
        if (deltaPos.sqrMagnitude > 1e-8f)
        {
            return deltaPos.normalized;
        }

        if (binding.smoothedVelocity.sqrMagnitude > 1e-8f)
        {
            return binding.smoothedVelocity.normalized;
        }

        return Vector3.forward;
    }

    private Vector3 SampleHistoryByDistance(TrailBinding binding, float sampleDistance)
    {
        List<TrailSample> history = binding.history;
        if (history == null || history.Count == 0)
        {
            return binding.velocitySource != null ? binding.velocitySource.position : binding.tailRoot.position;
        }

        if (sampleDistance <= history[0].distance)
        {
            return history[0].position;
        }

        int last = history.Count - 1;
        if (sampleDistance >= history[last].distance)
        {
            return history[last].position;
        }

        for (int i = 1; i < history.Count; i++)
        {
            TrailSample next = history[i];
            if (next.distance < sampleDistance)
            {
                continue;
            }

            TrailSample prev = history[i - 1];
            float span = Mathf.Max(0.0001f, next.distance - prev.distance);
            float t = Mathf.Clamp01((sampleDistance - prev.distance) / span);
            return Vector3.Lerp(prev.position, next.position, t);
        }

        return history[last].position;
    }

    private void ApplyVelocity(TrailBinding binding)
    {
        Vector3 velocity = binding.smoothedVelocity * binding.velocityScale;
        if (binding.invertTrailDirection)
        {
            velocity = -velocity;
        }

        velocity *= binding.trailSign >= 0f ? 1f : -1f;

        if (binding.convertWorldToLocal && binding.localSpaceReference != null)
        {
            velocity = binding.localSpaceReference.InverseTransformDirection(velocity);
        }

        if (_debugLogs && velocity.sqrMagnitude > 0.000001f)
        {
            Debug.Log($"[HomeProjectileTrailVelocityProvider] binding='{binding.name}' velocity={velocity:F4}", this);
        }

        if (binding.targetRenderers == null)
        {
            return;
        }

        for (int i = 0; i < binding.targetRenderers.Length; i++)
        {
            Renderer renderer = binding.targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material mat = materials[m];
                if (mat == null)
                {
                    continue;
                }

                if (mat.HasProperty(StartVelocityRangeXID))
                {
                    mat.SetColor(StartVelocityRangeXID, BuildRangeColor(velocity.x, binding.rangeSpread));
                }
                if (mat.HasProperty(StartVelocityRangeYID))
                {
                    mat.SetColor(StartVelocityRangeYID, BuildRangeColor(velocity.y, binding.rangeSpread));
                }
                if (mat.HasProperty(StartVelocityRangeZID))
                {
                    mat.SetColor(StartVelocityRangeZID, BuildRangeColor(velocity.z, binding.rangeSpread));
                }
            }
        }
    }

    private Color BuildRangeColor(float value, float rangeSpread)
    {
        float spread = Mathf.Max(_minimumAxisRange, Mathf.Abs(value) * rangeSpread);
        return new Color(value - spread, value + spread, 0f, 0f);
    }
}
