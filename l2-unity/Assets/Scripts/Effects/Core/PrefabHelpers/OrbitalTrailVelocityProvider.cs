using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrbitalTrailVelocityProvider : MonoBehaviour
{
    [Serializable]
    public sealed class TrailBinding
    {
        public string name;
        public Transform tailRoot;
        public Transform velocitySource;
        public string velocitySourceName = "waterball";
        public bool useOrbitRadiusOverride = true;
        public Vector3 orbitRadiusLocalOverride = new Vector3(0f, 0f, 0.35f);
        public bool preferSelfPositionForRadius;
        public Renderer[] targetRenderers;
        public bool autoCollectChildren = true;
        public bool setDirectionFlags;
        public bool convertWorldToLocal = true;
        public Transform localSpaceReference;
        public float velocityScale = 0.35f;
        public float rangeSpread = 0.2f;
        public bool invertTrailDirection = true;
        public float trailSign = 1f;

        [NonSerialized] public Vector3 lastPosition;
        [NonSerialized] public Vector3 smoothedVelocity;
        [NonSerialized] public bool hasLastPosition;
        [NonSerialized] public bool loggedFirstVelocity;
    }

    [Header("Shared Orbit Settings")]
    [SerializeField] private bool _useSyntheticOrbitVelocity = true;
    [SerializeField] private EffectPrefabRotator _rotatorSource;
    [SerializeField] private Transform _orbitCenter;
    [SerializeField] private bool _clockwise;
    [SerializeField] private float _minimumOrbitRadius = 0.01f;
    [SerializeField] private float _minimumAxisRange = 0.01f;
    [SerializeField] private float _smoothing = 16f;
    [SerializeField] private bool _debugLogs = true;

    [Header("Bindings (Tail -> Source)")]
    [SerializeField] private List<TrailBinding> _bindings = new();

    // UE2.5 macro-world: Unity meters * 52.5 = Unreal Units (L2Fx_UcPositionToUnityMeters).
    private const float UnityMetersToUu = 52.5f;

    private static readonly int UseDirectionAsID = Shader.PropertyToID("_UseDirectionAs");
    private static readonly int GetVelocityDirectionFromID = Shader.PropertyToID("_GetVelocityDirectionFrom");
    private static readonly int StartVelocityRangeXID = Shader.PropertyToID("_StartVelocityRangeX");
    private static readonly int StartVelocityRangeYID = Shader.PropertyToID("_StartVelocityRangeY");
    private static readonly int StartVelocityRangeZID = Shader.PropertyToID("_StartVelocityRangeZ");
    private static readonly int StartVelocityRangeXUcID = Shader.PropertyToID("_StartVelocityRangeXUc");
    private static readonly int StartVelocityRangeYUcID = Shader.PropertyToID("_StartVelocityRangeYUc");
    private static readonly int StartVelocityRangeZUcID = Shader.PropertyToID("_StartVelocityRangeZUc");

    private void OnEnable()
    {
        ResolveSharedSources();

        for (int i = 0; i < _bindings.Count; i++)
        {
            TrailBinding binding = _bindings[i];
            if (binding == null || binding.tailRoot == null)
            {
                continue;
            }

            ResolveBindingSources(binding);
            binding.hasLastPosition = false;
            binding.smoothedVelocity = Vector3.zero;
            binding.loggedFirstVelocity = false;

            if (_debugLogs)
            {
                int rendererCount = binding.targetRenderers != null ? binding.targetRenderers.Length : 0;
                string sourceName = binding.velocitySource != null ? binding.velocitySource.name : "null";
                Debug.Log($"[OrbitalTrailVelocityProvider] Enabled binding='{binding.name}' tail='{binding.tailRoot.name}' source={sourceName} renderers={rendererCount}", this);
            }
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

            if (_useSyntheticOrbitVelocity && TryGetSyntheticVelocity(binding, out Vector3 rawVelocity))
            {
                ProcessBinding(binding, rawVelocity, dt);
                continue;
            }

            Vector3 sourcePosition = GetVelocitySourcePosition(binding);
            if (!binding.hasLastPosition)
            {
                binding.lastPosition = sourcePosition;
                binding.hasLastPosition = true;
                continue;
            }

            rawVelocity = (sourcePosition - binding.lastPosition) / dt;
            binding.lastPosition = sourcePosition;
            ProcessBinding(binding, rawVelocity, dt);
        }
    }

    private void ProcessBinding(TrailBinding binding, Vector3 rawVelocity, float dt)
    {
        float lerpT = Mathf.Clamp01(_smoothing * dt);
        binding.smoothedVelocity = Vector3.Lerp(binding.smoothedVelocity, rawVelocity, lerpT);

        float sign = binding.trailSign >= 0f ? 1f : -1f;
        if (binding.invertTrailDirection)
        {
            sign *= -1f;
        }

        Vector3 shaderVelocity = binding.smoothedVelocity * (binding.velocityScale * sign);
        if (binding.convertWorldToLocal)
        {
            Transform localRef = binding.localSpaceReference != null ? binding.localSpaceReference : binding.tailRoot;
            shaderVelocity = localRef.InverseTransformDirection(shaderVelocity);
        }

        if (_debugLogs && !binding.loggedFirstVelocity && shaderVelocity.sqrMagnitude > 0.000001f)
        {
            binding.loggedFirstVelocity = true;
            Debug.Log($"[OrbitalTrailVelocityProvider] binding='{binding.name}' raw={rawVelocity:F4} shader={shaderVelocity:F4}", this);
        }

        ApplyVelocityToMaterials(ResolveBindingMaterials(binding), binding, shaderVelocity);
    }

    static Material[] ResolveBindingMaterials(TrailBinding binding)
    {
        if (binding.tailRoot != null)
        {
            ParticleGroupV2 group = binding.tailRoot.GetComponent<ParticleGroupV2>();
            if (group != null && group.IsGpuDraw)
            {
                Material[] gpuMaterials = group.GpuMaterials;
                if (gpuMaterials != null && gpuMaterials.Length > 0)
                {
                    return gpuMaterials;
                }
            }
        }

        if (binding.targetRenderers == null)
        {
            return null;
        }

        int count = 0;
        for (int i = 0; i < binding.targetRenderers.Length; i++)
        {
            Renderer renderer = binding.targetRenderers[i];
            if (renderer != null && renderer.sharedMaterial != null)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        Material[] materials = new Material[count];
        int write = 0;
        for (int i = 0; i < binding.targetRenderers.Length; i++)
        {
            Renderer renderer = binding.targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.sharedMaterial;
            if (material != null)
            {
                materials[write++] = material;
            }
        }

        return materials;
    }

    void ApplyVelocityToMaterials(Material[] materials, TrailBinding binding, Vector3 velocity)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null)
            {
                continue;
            }

            if (binding.setDirectionFlags)
            {
                if (mat.HasProperty(UseDirectionAsID))
                {
                    mat.SetFloat(UseDirectionAsID, 1f);
                }
                if (mat.HasProperty(GetVelocityDirectionFromID))
                {
                    mat.SetFloat(GetVelocityDirectionFromID, 1f);
                }
            }

            // Unified V2 sprite/mesh: StartVelocityRange*Uc is UU, Z-up.
            Vector3 velocityUe = UnityMetersToUeUu(velocity);
            SetRangeProperty(mat, StartVelocityRangeXUcID, velocityUe.x, binding.rangeSpread);
            SetRangeProperty(mat, StartVelocityRangeYUcID, velocityUe.y, binding.rangeSpread);
            SetRangeProperty(mat, StartVelocityRangeZUcID, velocityUe.z, binding.rangeSpread);
            // Legacy ice-bolt / vampiric shaders still read the Unity-meter names.
            SetRangeProperty(mat, StartVelocityRangeXID, velocity.x, binding.rangeSpread);
            SetRangeProperty(mat, StartVelocityRangeYID, velocity.y, binding.rangeSpread);
            SetRangeProperty(mat, StartVelocityRangeZID, velocity.z, binding.rangeSpread);
        }
    }

    void SetRangeProperty(Material mat, int id, float value, float rangeSpread)
    {
        if (mat.HasProperty(id))
        {
            mat.SetVector(id, BuildRangeVector(value, rangeSpread));
        }
    }

    static Vector3 UnityMetersToUeUu(Vector3 unityMeters)
    {
        return new Vector3(
            unityMeters.x * UnityMetersToUu,
            unityMeters.z * UnityMetersToUu,
            unityMeters.y * UnityMetersToUu);
    }

    private void ResolveSharedSources()
    {
        if (_rotatorSource == null)
        {
            _rotatorSource = GetComponent<EffectPrefabRotator>();
            if (_rotatorSource == null)
            {
                _rotatorSource = GetComponentInParent<EffectPrefabRotator>();
            }
        }

        if (_orbitCenter == null && _rotatorSource != null)
        {
            _orbitCenter = _rotatorSource.transform;
        }
    }

    private void ResolveBindingSources(TrailBinding binding)
    {
        if (binding.autoCollectChildren && (binding.targetRenderers == null || binding.targetRenderers.Length == 0))
        {
            binding.targetRenderers = binding.tailRoot.GetComponentsInChildren<Renderer>(true);
        }

        if (binding.localSpaceReference == null)
        {
            binding.localSpaceReference = _orbitCenter != null ? _orbitCenter : binding.tailRoot;
        }

        if (binding.velocitySource != null || string.IsNullOrEmpty(binding.velocitySourceName))
        {
            return;
        }

        Transform searchRoot = binding.tailRoot.parent != null ? binding.tailRoot.parent : transform;
        Transform[] candidates = searchRoot.GetComponentsInChildren<Transform>(true);
        Transform bestMatch = null;
        float bestSqrDistance = float.MaxValue;
        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null || !candidate.name.Equals(binding.velocitySourceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            float sqrDistance = (candidate.position - binding.tailRoot.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestMatch = candidate;
            }
        }

        binding.velocitySource = bestMatch;
    }

    private Vector3 GetVelocitySourcePosition(TrailBinding binding)
    {
        return binding.velocitySource != null ? binding.velocitySource.position : binding.tailRoot.position;
    }

    private bool TryGetSyntheticVelocity(TrailBinding binding, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (_rotatorSource == null || _orbitCenter == null)
        {
            return false;
        }

        Vector3 axisWorld = _rotatorSource.RotationAxis.sqrMagnitude > 0f
            ? _rotatorSource.RotationAxis.normalized
            : Vector3.up;
        if (_rotatorSource.RotationSpace == Space.Self)
        {
            axisWorld = _rotatorSource.transform.TransformDirection(axisWorld).normalized;
        }

        float angularSpeedRad = _rotatorSource.DegreesPerSecond * Mathf.Deg2Rad;
        if (_clockwise)
        {
            angularSpeedRad *= -1f;
        }

        Vector3 radius = ResolveOrbitRadiusWorld(binding);
        if (radius.sqrMagnitude < (_minimumOrbitRadius * _minimumOrbitRadius))
        {
            return false;
        }

        velocity = Vector3.Cross(axisWorld * angularSpeedRad, radius);
        return velocity.sqrMagnitude > 0f;
    }

    private Vector3 ResolveOrbitRadiusWorld(TrailBinding binding)
    {
        if (binding.useOrbitRadiusOverride)
        {
            Transform basis = _orbitCenter != null ? _orbitCenter : binding.tailRoot;
            return basis.TransformDirection(binding.orbitRadiusLocalOverride);
        }

        if (binding.preferSelfPositionForRadius && _orbitCenter != null)
        {
            Vector3 fromTail = binding.tailRoot.position - _orbitCenter.position;
            if (fromTail.sqrMagnitude > 0.000001f)
            {
                return fromTail;
            }
        }

        if (binding.velocitySource != null && _orbitCenter != null)
        {
            return binding.velocitySource.position - _orbitCenter.position;
        }

        return Vector3.zero;
    }

    private Vector4 BuildRangeVector(float value, float rangeSpread)
    {
        float spread = Mathf.Max(_minimumAxisRange, Mathf.Abs(value) * rangeSpread);
        return new Vector4(value - spread, value + spread, 0f, 0f);
    }
}
