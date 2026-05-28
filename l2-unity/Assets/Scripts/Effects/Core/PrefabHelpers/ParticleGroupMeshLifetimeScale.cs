using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scales mesh quad transforms over shader lifetime (UE SizeScale), while the material uses
/// <c>_UseMeshQuadBounds</c> and keeps <c>_UseSizeScale</c> off to avoid double scaling.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleGroup))]
public sealed class ParticleGroupMeshLifetimeScale : MonoBehaviour
{
    private static readonly int UseSizeScaleId = Shader.PropertyToID("_UseSizeScale");
    private static readonly int HasLifetimeId = Shader.PropertyToID("_HasLifetime");
    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    private static readonly int InitialDelayRangeId = Shader.PropertyToID("_InitialDelayRange");
    private static readonly int LifetimeRangeId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int SizeScale0Id = Shader.PropertyToID("_SizeScale0");
    private static readonly int SizeScale1Id = Shader.PropertyToID("_SizeScale1");
    private static readonly int SizeScale2Id = Shader.PropertyToID("_SizeScale2");
    private static readonly int SizeScale3Id = Shader.PropertyToID("_SizeScale3");
    private static readonly int SizeScale4Id = Shader.PropertyToID("_SizeScale4");
    private static readonly int FadeoutId = Shader.PropertyToID("_Fadeout");
    private static readonly int FadeoutStartTimeId = Shader.PropertyToID("_FadeoutStartTime");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    [SerializeField] private bool _readCurveFromMaterial = true;
    [Tooltip("Used only when Expansion End Sec is 0: path speed = material lifetime / this value.")]
    [SerializeField] private float _curveTimeScale = 1f;
    [SerializeField] private bool _syncFadeOutToExpansionPath = true;
    [SerializeField, Range(0f, 1f)]
    private float _fadeOutStartPathT = 0.6f;
    [SerializeField] private bool _syncLifetimeToExpansion = true;
    [Tooltip("Seconds until SizeScale path reaches 100%. 0 = material lifetime / curve time scale.")]
    [SerializeField] private float _expansionEndSec = 0.6f;
    [Tooltip("Total shader lifetime after initial delay. Use a value greater than Expansion End Sec to let particles hover and fade after the burst.")]
    [SerializeField] private float _particleLifetimeSec = 1f;
    [SerializeField] private Vector2 _sizeScale0 = new Vector2(0f, 1f);
    [SerializeField] private Vector2 _sizeScale1 = new Vector2(0.25f, 1.25f);
    [SerializeField] private Vector2 _sizeScale2 = new Vector2(0.5f, 1.5f);
    [SerializeField] private Vector2 _sizeScale3 = new Vector2(0.75f, 1.75f);
    [SerializeField] private Vector2 _sizeScale4 = new Vector2(1f, 2f);

    [Header("Planar scatter (no forward)")]
    [SerializeField] private bool _usePlanarScatter = true;
    [Tooltip("Local axis treated as forward (toward caster). Scale/offset stay in the perpendicular plane.")]
    [SerializeField] private Vector3 _scatterForwardAxisLocal = Vector3.right;
    [SerializeField] private float _scatterDistance = 0f;
    [SerializeField] private bool _driftAfterExpansion = false;
    [SerializeField] private float _postBurstDriftSpeed = 0.12f;
    [Tooltip("Keeps outward motion after the burst by continuing uniform scale instead of moving quad centers.")]
    [SerializeField] private bool _scaleAfterExpansion = true;
    [SerializeField] private float _postBurstScaleSpeed = 0.25f;
    [Tooltip("Keep disabled for billboard particles: local X/Y map to screen right/up, so uniform scale preserves aspect.")]
    [SerializeField] private bool _scaleOnlyInScatterPlane = false;

    private readonly Dictionary<Transform, Vector3> _baseLocalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, PlanarScatterState> _scatterStates = new Dictionary<Transform, PlanarScatterState>();

    private struct PlanarScatterState
    {
        public Vector3 BaseLocalPosition;
        public Vector3 PlanarDirection;
    }

    public void CacheBaseScale(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (!_baseLocalScales.ContainsKey(target))
        {
            _baseLocalScales[target] = target.localScale;
        }
    }

    public void ResetAllCachedScales()
    {
        foreach (KeyValuePair<Transform, Vector3> entry in _baseLocalScales)
        {
            if (entry.Key != null)
            {
                entry.Key.localScale = entry.Value;
            }
        }

        foreach (KeyValuePair<Transform, PlanarScatterState> entry in _scatterStates)
        {
            if (entry.Key != null)
            {
                entry.Key.localPosition = entry.Value.BaseLocalPosition;
            }
        }

        _scatterStates.Clear();
    }

    public void SyncBeforePlay(ParticleGroup group)
    {
        if (!_syncLifetimeToExpansion || group == null)
        {
            return;
        }

        Material curveMat = ResolveCurveMaterial(group);
        float particleLifetime = ResolveParticleLifetimeSec(curveMat);
        float initialDelay = ReadInitialDelaySec(curveMat);
        group.SetSyncedParticleDuration(initialDelay + particleLifetime);
    }

    public float ResolveExpansionEndSecForSpawn(Material curveMat)
    {
        return ResolveExpansionEndSec(curveMat);
    }

    public float ResolveParticleLifetimeSecForSpawn(Material curveMat)
    {
        return ResolveParticleLifetimeSec(curveMat);
    }

    public void Apply(Renderer renderer, float now)
    {
        if (renderer == null || !renderer.gameObject.activeInHierarchy)
        {
            return;
        }

        Transform target = renderer.transform;
        CacheBaseScale(target);

        Material runtimeMat = renderer.material;
        if (runtimeMat == null)
        {
            return;
        }

        Material curveMat = renderer.sharedMaterial != null ? renderer.sharedMaterial : runtimeMat;
        SyncFadeOutStartTime(runtimeMat, curveMat);
        float pathT = ComputeExpansionPathT(runtimeMat, curveMat, now);
        float age = ComputeAgeAfterDelay(runtimeMat, now);
        float scaleMul = EvaluateSizeScale(curveMat, pathT);
        scaleMul += ComputePostBurstScaleDrift(curveMat, age);
        if (_usePlanarScatter)
        {
            CachePlanarScatter(target, runtimeMat);
            ApplyPlanarPosition(target, curveMat, pathT, age);
            ApplyPlanarScale(target, scaleMul);
        }
        else
        {
            target.localScale = _baseLocalScales[target] * Mathf.Max(0.01f, scaleMul);
        }
    }

    private void CachePlanarScatter(Transform target, Material runtimeMat)
    {
        if (_scatterStates.ContainsKey(target))
        {
            return;
        }

        float seed = runtimeMat.HasProperty(SeedId) ? runtimeMat.GetFloat(SeedId) : target.GetInstanceID();
        Vector3 forward = _scatterForwardAxisLocal.sqrMagnitude > 1e-6f
            ? _scatterForwardAxisLocal.normalized
            : Vector3.right;

        _scatterStates[target] = new PlanarScatterState
        {
            BaseLocalPosition = target.localPosition,
            PlanarDirection = RandomPlanarDirection(forward, seed),
        };
    }

    private void ApplyPlanarPosition(Transform target, Material curveMat, float pathT, float age)
    {
        if (!_scatterStates.TryGetValue(target, out PlanarScatterState state))
        {
            return;
        }

        float burstTravel = Mathf.Max(0f, _scatterDistance) * Mathf.Clamp01(pathT);
        float driftTravel = 0f;
        if (_driftAfterExpansion)
        {
            float expansionEnd = ResolveExpansionEndSec(curveMat);
            float lifetime = ResolveParticleLifetimeSec(curveMat);
            float driftAge = Mathf.Max(0f, age - expansionEnd);
            float driftLimit = Mathf.Max(0f, lifetime - expansionEnd);
            driftTravel = Mathf.Min(driftAge, driftLimit) * Mathf.Max(0f, _postBurstDriftSpeed);
        }

        float travel = burstTravel + driftTravel;
        target.localPosition = state.BaseLocalPosition + state.PlanarDirection * travel;
    }

    private float ComputePostBurstScaleDrift(Material curveMat, float age)
    {
        if (!_scaleAfterExpansion)
        {
            return 0f;
        }

        float expansionEnd = ResolveExpansionEndSec(curveMat);
        float lifetime = ResolveParticleLifetimeSec(curveMat);
        float driftAge = Mathf.Max(0f, age - expansionEnd);
        float driftLimit = Mathf.Max(0f, lifetime - expansionEnd);
        return Mathf.Min(driftAge, driftLimit) * Mathf.Max(0f, _postBurstScaleSpeed);
    }

    private void ApplyPlanarScale(Transform target, float scaleMul)
    {
        Vector3 baseScale = _baseLocalScales[target];
        float mul = Mathf.Max(0.01f, scaleMul);

        if (!_scaleOnlyInScatterPlane)
        {
            target.localScale = baseScale * mul;
            return;
        }

        Vector3 forward = _scatterForwardAxisLocal.sqrMagnitude > 1e-6f
            ? _scatterForwardAxisLocal.normalized
            : Vector3.right;
        Vector3 absForward = new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z));
        Vector3 scaled = baseScale;
        if (absForward.x >= absForward.y && absForward.x >= absForward.z)
        {
            scaled.y *= mul;
            scaled.z *= mul;
        }
        else if (absForward.y >= absForward.x && absForward.y >= absForward.z)
        {
            scaled.x *= mul;
            scaled.z *= mul;
        }
        else
        {
            scaled.x *= mul;
            scaled.y *= mul;
        }

        target.localScale = scaled;
    }

    private static Vector3 RandomPlanarDirection(Vector3 forward, float seed)
    {
        float angle = Mathf.Repeat(Mathf.Abs(seed) * 12.9898f, 1f) * Mathf.PI * 2f;
        Vector3 reference = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.92f ? Vector3.up : Vector3.forward;
        Vector3 tangent = Vector3.Cross(forward, reference);
        if (tangent.sqrMagnitude < 1e-6f)
        {
            tangent = Vector3.Cross(forward, Vector3.right);
        }

        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(forward, tangent).normalized;
        Vector3 dir = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);
        dir -= forward * Vector3.Dot(dir, forward);
        return dir.sqrMagnitude > 1e-6f ? dir.normalized : tangent;
    }

    private void SyncFadeOutStartTime(Material runtimeMat, Material curveMat)
    {
        if (!_syncFadeOutToExpansionPath || runtimeMat == null || !runtimeMat.HasProperty(FadeoutStartTimeId))
        {
            return;
        }

        Material lifetimeMat = curveMat != null ? curveMat : runtimeMat;
        float expansionEnd = ResolveExpansionEndSec(lifetimeMat);
        float fadeStart = expansionEnd * _fadeOutStartPathT;

        if (runtimeMat.HasProperty(FadeoutId))
        {
            runtimeMat.SetFloat(FadeoutId, 1f);
        }

        runtimeMat.SetFloat(FadeoutStartTimeId, fadeStart);
    }

    private float ResolveExpansionEndSec(Material curveMat)
    {
        if (_expansionEndSec > 0f)
        {
            return _expansionEndSec;
        }

        float materialLifetime = 0.8f;
        if (curveMat != null && curveMat.HasProperty(LifetimeRangeId))
        {
            materialLifetime = Mathf.Max(0.0001f, curveMat.GetVector(LifetimeRangeId).y);
        }

        float curveScale = Mathf.Max(0.01f, _curveTimeScale);
        return materialLifetime / curveScale;
    }

    private float ResolveParticleLifetimeSec(Material curveMat)
    {
        float expansionEnd = ResolveExpansionEndSec(curveMat);
        if (_particleLifetimeSec > 0f)
        {
            return Mathf.Max(expansionEnd, _particleLifetimeSec);
        }

        if (curveMat != null && curveMat.HasProperty(LifetimeRangeId))
        {
            return Mathf.Max(expansionEnd, curveMat.GetVector(LifetimeRangeId).y);
        }

        return expansionEnd;
    }

    private static float ReadInitialDelaySec(Material mat)
    {
        if (mat != null && mat.HasProperty(InitialDelayRangeId))
        {
            return Mathf.Max(0f, mat.GetVector(InitialDelayRangeId).x);
        }

        return 0f;
    }

    private static Material ResolveCurveMaterial(ParticleGroup group)
    {
        Renderer[] particles = group.Particles;
        if (particles == null || particles.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            Renderer renderer = particles[i];
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial;
            }
        }

        return null;
    }

    private float ComputeExpansionPathT(Material runtimeMat, Material curveMat, float now)
    {
        if (runtimeMat.HasProperty(HasLifetimeId) && runtimeMat.GetFloat(HasLifetimeId) < 0.5f)
        {
            return 1f;
        }

        float age = ComputeAgeAfterDelay(runtimeMat, now);
        Material lifetimeMat = curveMat != null ? curveMat : runtimeMat;
        float expansionEnd = ResolveExpansionEndSec(lifetimeMat);

        if (_expansionEndSec > 0f || _syncLifetimeToExpansion)
        {
            return Mathf.Clamp01(age / Mathf.Max(0.0001f, expansionEnd));
        }

        float lifetime = expansionEnd;
        if (runtimeMat.HasProperty(LifetimeRangeId))
        {
            lifetime = Mathf.Max(0.0001f, runtimeMat.GetVector(LifetimeRangeId).y);
        }

        float timeScale = Mathf.Max(0.01f, _curveTimeScale);
        return Mathf.Clamp01((age / lifetime) * timeScale);
    }

    private static float ComputeAgeAfterDelay(Material runtimeMat, float now)
    {
        float startTime = runtimeMat.HasProperty(StartTimeId) ? runtimeMat.GetFloat(StartTimeId) : now;
        float delay = 0f;
        if (runtimeMat.HasProperty(InitialDelayRangeId))
        {
            Vector4 delayRange = runtimeMat.GetVector(InitialDelayRangeId);
            delay = delayRange.x;
        }

        return Mathf.Max(0f, now - startTime - delay);
    }

    private float EvaluateSizeScale(Material mat, float normalizedAge)
    {
        Vector2 s0 = _sizeScale0;
        Vector2 s1 = _sizeScale1;
        Vector2 s2 = _sizeScale2;
        Vector2 s3 = _sizeScale3;
        Vector2 s4 = _sizeScale4;

        if (_readCurveFromMaterial && mat != null)
        {
            if (mat.HasProperty(SizeScale0Id))
            {
                Vector4 v = mat.GetVector(SizeScale0Id);
                s0 = new Vector2(v.x, v.y);
            }

            if (mat.HasProperty(SizeScale1Id))
            {
                Vector4 v = mat.GetVector(SizeScale1Id);
                s1 = new Vector2(v.x, v.y);
            }

            if (mat.HasProperty(SizeScale2Id))
            {
                Vector4 v = mat.GetVector(SizeScale2Id);
                s2 = new Vector2(v.x, v.y);
            }

            if (mat.HasProperty(SizeScale3Id))
            {
                Vector4 v = mat.GetVector(SizeScale3Id);
                s3 = new Vector2(v.x, v.y);
            }

            if (mat.HasProperty(SizeScale4Id))
            {
                Vector4 v = mat.GetVector(SizeScale4Id);
                s4 = new Vector2(v.x, v.y);
            }
        }
        else if (mat != null && mat.HasProperty(UseSizeScaleId) && mat.GetFloat(UseSizeScaleId) < 0.5f)
        {
            return 1f;
        }

        float t = Mathf.Clamp01(normalizedAge);
        float t0 = Mathf.Clamp01(s0.x);
        float t1 = Mathf.Clamp01(s1.x);
        float t2 = Mathf.Clamp01(s2.x);
        float t3 = Mathf.Clamp01(s3.x);
        float t4 = Mathf.Clamp01(s4.x);

        if (t <= t1)
        {
            return Mathf.Lerp(s0.y, s1.y, Mathf.InverseLerp(t0, Mathf.Max(t0 + 1e-5f, t1), t));
        }

        if (t <= t2)
        {
            return Mathf.Lerp(s1.y, s2.y, Mathf.InverseLerp(t1, Mathf.Max(t1 + 1e-5f, t2), t));
        }

        if (t <= t3)
        {
            return Mathf.Lerp(s2.y, s3.y, Mathf.InverseLerp(t2, Mathf.Max(t2 + 1e-5f, t3), t));
        }

        return Mathf.Lerp(s3.y, s4.y, Mathf.InverseLerp(t3, Mathf.Max(t3 + 1e-5f, t4), t));
    }
}
