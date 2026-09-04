using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameObject/Material side of ParticleGroup. Simulation stays Burst-only.
/// </summary>
public sealed class ParticleGroupRendererService
{
    const string OwnerWorldPosShaderProperty = "_OwnerWorldPos";
    static readonly int StartTimeShaderId = Shader.PropertyToID("_StartTime");
    static readonly int SeedShaderId = Shader.PropertyToID("_Seed");
    static readonly int DebugMeshPreviewShaderId = Shader.PropertyToID("_DebugMeshPreview");

    readonly EffectPart _host;
    readonly Transform _transform;
    Renderer[] _particles;
    MaterialPropertyBlock _propertyBlock;
    ParticleGroupSpawnSpin _spawnSpin;
    bool _spawnSpinResolved;
    Matrix4x4[] _objectToWorldMatrices;
    TransformOrigin[] _transformOrigins;

    struct TransformOrigin
    {
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    public ParticleGroupRendererService(EffectPart host)
    {
        _host = host;
        _transform = host.transform;
    }

    public Renderer[] Particles => _particles;
    public int Count => _particles != null ? _particles.Length : 0;

    public void SetParticles(Renderer[] particles)
    {
        RestoreCoordinateSystemSlots();
        _particles = particles;
        _transformOrigins = null;
    }

    public void CollectIfEmpty()
    {
        if (_particles == null || _particles.Length == 0)
            _particles = _transform.GetComponentsInChildren<Renderer>(true);
    }

    public void EnsureClones(bool cloneToMaxCount, int maxCount, int cloneLimit, ref bool clonesCreated)
    {
        if (!cloneToMaxCount || !Application.isPlaying || clonesCreated || _particles == null || _particles.Length == 0)
            return;

        int desiredCount = Mathf.Clamp(maxCount, _particles.Length, Mathf.Max(_particles.Length, cloneLimit));
        if (desiredCount <= _particles.Length)
            return;

        List<Renderer> particles = new List<Renderer>(_particles);
        int sourceCount = particles.Count;
        for (int i = particles.Count; i < desiredCount; i++)
        {
            Renderer source = particles[i % sourceCount];
            if (source == null)
                continue;

            GameObject clone = Object.Instantiate(source.gameObject, source.transform.parent);
            clone.name = $"{source.gameObject.name}_RuntimeClone";
            clone.SetActive(false);
            Renderer cloneRenderer = clone.GetComponent<Renderer>();
            if (cloneRenderer != null)
                particles.Add(cloneRenderer);
        }

        _particles = particles.ToArray();
        clonesCreated = true;
    }

    public void ExpandShaderDrivenBounds()
    {
        if (_particles == null)
            return;

        for (int i = 0; i < _particles.Length; i++)
        {
            Renderer renderer = _particles[i];
            if (renderer == null)
                continue;

            bool expand = false;
            Material[] materials = renderer.sharedMaterials;
            if (materials != null)
            {
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material != null &&
                        material.HasProperty("_ExpandShaderBounds") &&
                        material.GetFloat("_ExpandShaderBounds") > 0.5f)
                    {
                        expand = true;
                        break;
                    }
                }
            }

            if (!expand)
                continue;

            renderer.allowOcclusionWhenDynamic = false;
            renderer.localBounds = new Bounds(
                new Vector3(0f, 0.5f, 0f),
                new Vector3(8f, 8f, 8f));
        }
    }

    public void HideAll()
    {
        if (_particles == null)
            return;

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] != null)
                _particles[i].gameObject.SetActive(false);
        }
    }

    public void DisableForGpuDraw()
    {
        if (_particles == null)
            return;

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] == null)
                continue;
            _particles[i].enabled = false;
            _particles[i].gameObject.SetActive(false);
        }
    }

    public void EnableForGameObjectDraw()
    {
        if (_particles == null)
            return;

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] != null)
                _particles[i].enabled = true;
        }
    }

    public bool ApplyExpire(
        ParticleGroupSimulation simulation,
        bool[] managedActive,
        float[] managedTimes)
    {
        if (_particles == null || managedActive == null)
            return false;

        bool gpuDraw = simulation.GpuEnabled;
        bool anyActive = false;
        int count = _particles.Length;
        bool hasSim = simulation.HasLifetimeBuffers;
        for (int i = 0; i < count; i++)
        {
            bool wasActive = managedActive[i];
            bool nowActive = hasSim ? simulation.IsActive(i) : wasActive;
            if (wasActive && nowActive && !gpuDraw)
                UpdateWorldPositions(_particles[i]);

            if (wasActive && !nowActive)
            {
                if (!gpuDraw && _particles[i] != null)
                    _particles[i].gameObject.SetActive(false);
                managedActive[i] = false;
                continue;
            }

            if (hasSim)
            {
                managedActive[i] = nowActive;
                if (managedTimes != null)
                    managedTimes[i] = simulation.SpawnTime(i);
            }

            if (managedActive[i])
                anyActive = true;
        }

        return anyActive;
    }

    public void ActivateGoSlot(
        int slot,
        float shaderStartTime,
        float seed,
        uint meshRandBase,
        uint spriteRandBase)
    {
        if (_particles == null || slot < 0 || slot >= _particles.Length)
        {
            return;
        }

        Renderer renderer = _particles[slot];
        if (renderer == null)
        {
            return;
        }

        // A group can fall back after having used GPU drawing on an earlier
        // playback/domain reload. DisableForGpuDraw leaves the component off.
        renderer.enabled = true;
        ApplySpawnTiming(renderer, shaderStartTime, seed);
        Material[] runtimeMaterials = renderer.materials;
        Material[] sharedMaterials = renderer.sharedMaterials;
        for (int materialIndex = 0; materialIndex < runtimeMaterials.Length; materialIndex++)
        {
            Material material = runtimeMaterials[materialIndex];
            if (material == null)
                continue;

            Material shared = sharedMaterials != null && materialIndex < sharedMaterials.Length
                ? sharedMaterials[materialIndex]
                : null;
            if (shared != null)
            {
                L2MaterialPropertyCopier.CopyLifetimeFadeAndFxFromShared(material, shared);
                L2MaterialPropertyCopier.CopyMeshAppRandStartSpinFromBaseState(
                    material,
                    shared,
                    meshRandBase,
                    slot);
            }

            if (material.HasProperty("_Alpha") && shared != null && shared.HasProperty("_Alpha"))
                material.SetFloat("_Alpha", shared.GetFloat("_Alpha"));

            if (material.HasProperty(DebugMeshPreviewShaderId))
                material.SetFloat(DebugMeshPreviewShaderId, 0f);

            material.SetFloat(StartTimeShaderId, shaderStartTime);
            material.SetFloat(SeedShaderId, seed);
            ApplySpawnSpin(renderer, seed);
            uint spriteSpawnState = L2MaterialPropertyCopier.AdvanceAppRandState(
                spriteRandBase,
                slot * L2AppRand.SpriteMotionSlotStride);
            L2MaterialPropertyCopier.SetSpriteMotionRandState(material, spriteSpawnState);
            L2MaterialPropertyCopier.SetSpriteSpinRandState(
                material,
                L2MaterialPropertyCopier.AdvanceAppRandState(spriteSpawnState, L2AppRand.SpriteSpinDraws));
            L2MaterialPropertyCopier.ApplyHealingPotionSe0MotionReplay(material, slot);
            SetWorldPositions(material);

            if (_host.SurfaceNormal != Vector3.zero)
                material.SetVector("_SurfaceNormals", _host.SurfaceNormal);
        }
    }

    public void ApplyCoordinateSystemToGoSlot(
        int slot,
        L2ParticleCoordinateSystem coordinateSystem)
    {
        if (_particles == null || slot < 0 || slot >= _particles.Length)
        {
            return;
        }

        Renderer renderer = _particles[slot];
        if (renderer == null)
        {
            return;
        }

        EnsureTransformOrigins();
        RestoreCoordinateSystemSlot(slot);

        Transform particleTransform = renderer.transform;
        if (coordinateSystem == L2ParticleCoordinateSystem.Spray)
        {
            // Native PTCS_Spray rotates position, velocity and acceleration by
            // the owner rotation once at SpawnParticle, then remains world-space.
            particleTransform.SetParent(null, true);
        }
        else if (coordinateSystem == L2ParticleCoordinateSystem.Independent)
        {
            // Native PTCS_Independent inherits spawn translation but not owner
            // rotation, and no longer follows later owner movement.
            Vector3 worldPosition = particleTransform.position;
            Vector3 worldScale = particleTransform.lossyScale;
            particleTransform.SetParent(null, false);
            particleTransform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            particleTransform.localScale = worldScale;
        }
    }

    public void RestoreCoordinateSystemSlots()
    {
        if (_particles == null || _transformOrigins == null)
        {
            return;
        }

        int count = Mathf.Min(_particles.Length, _transformOrigins.Length);
        for (int i = 0; i < count; i++)
        {
            RestoreCoordinateSystemSlot(i);
        }
    }

    void EnsureTransformOrigins()
    {
        int count = Count;
        if (_transformOrigins != null && _transformOrigins.Length == count)
        {
            return;
        }

        _transformOrigins = new TransformOrigin[count];
        for (int i = 0; i < count; i++)
        {
            Transform particleTransform = _particles[i] != null ? _particles[i].transform : null;
            if (particleTransform == null)
            {
                continue;
            }

            _transformOrigins[i] = new TransformOrigin
            {
                parent = particleTransform.parent,
                localPosition = particleTransform.localPosition,
                localRotation = particleTransform.localRotation,
                localScale = particleTransform.localScale
            };
        }
    }

    void RestoreCoordinateSystemSlot(int slot)
    {
        if (_particles == null ||
            _transformOrigins == null ||
            slot < 0 ||
            slot >= _particles.Length ||
            slot >= _transformOrigins.Length ||
            _particles[slot] == null)
        {
            return;
        }

        TransformOrigin origin = _transformOrigins[slot];
        Transform particleTransform = _particles[slot].transform;
        particleTransform.SetParent(origin.parent, false);
        particleTransform.localPosition = origin.localPosition;
        particleTransform.localRotation = origin.localRotation;
        particleTransform.localScale = origin.localScale;
    }

    public float ReadShaderSlotDuration(float fallbackDuration)
    {
        float lifetime = ReadLifetimeMax(fallbackDuration);
        float maxDelay = 0f;
        if (_particles != null && _particles.Length > 0 && _particles[0] != null)
        {
            foreach (Material material in _particles[0].sharedMaterials)
            {
                if (material != null && material.HasProperty("_InitialDelayRange"))
                    maxDelay = material.GetVector("_InitialDelayRange").y;
            }
        }

        return lifetime + maxDelay + 0.03f;
    }

    public float ReadLifetimeMax(float fallbackDuration)
    {
        if (_particles == null || _particles.Length == 0 || _particles[0] == null)
            return fallbackDuration;

        foreach (Material material in _particles[0].sharedMaterials)
        {
            if (material != null && material.HasProperty("_LifetimeRange"))
                return material.GetVector("_LifetimeRange").y;
        }

        return 0.5f;
    }

    public float ReadLifetimeCenter(float fallbackDuration)
    {
        if (_particles == null || _particles.Length == 0 || _particles[0] == null)
        {
            return fallbackDuration;
        }

        foreach (Material material in _particles[0].sharedMaterials)
        {
            if (material == null || !material.HasProperty("_LifetimeRange"))
            {
                continue;
            }

            Vector4 range = material.GetVector("_LifetimeRange");
            float min = range.x;
            float max = range.y;
            if (min > 0f || max > 0f)
            {
                if (min <= 0f)
                {
                    min = max;
                }

                if (max <= 0f)
                {
                    max = min;
                }

                return (min + max) * 0.5f;
            }
        }

        return fallbackDuration;
    }

    public Vector4 ResolveGpuOwnerWorldPos(Material[] gpuMaterials)
    {
        Material material = gpuMaterials != null && gpuMaterials.Length > 0 ? gpuMaterials[0] : null;
        return material != null ? (Vector4)_host.ResolveOwnerWorldPosForShader(material) : Vector4.zero;
    }

    public Matrix4x4[] ResolveObjectToWorldMatrices()
    {
        int count = Count;
        if (_objectToWorldMatrices == null || _objectToWorldMatrices.Length != count)
            _objectToWorldMatrices = new Matrix4x4[count];

        Matrix4x4 fallback = _transform.localToWorldMatrix;
        for (int i = 0; i < count; i++)
        {
            Renderer renderer = _particles[i];
            _objectToWorldMatrices[i] = renderer != null
                ? renderer.transform.localToWorldMatrix
                : fallback;
        }

        return _objectToWorldMatrices;
    }

    public void UpdateWorldPositions(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
            SetWorldPositions(materials[i]);
    }

    void ApplySpawnTiming(Renderer renderer, float shaderStartTime, float seed)
    {
        if (renderer == null)
            return;

        _propertyBlock ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(StartTimeShaderId, shaderStartTime);
        _propertyBlock.SetFloat(SeedShaderId, seed);
        _propertyBlock.SetFloat(DebugMeshPreviewShaderId, 0f);
        renderer.SetPropertyBlock(_propertyBlock);
    }

    void ApplySpawnSpin(Renderer renderer, float seed)
    {
        if (renderer == null)
            return;

        if (!_spawnSpinResolved)
        {
            _spawnSpin = _transform.GetComponent<ParticleGroupSpawnSpin>();
            _spawnSpinResolved = true;
        }

        _spawnSpin?.Apply(renderer, seed);
    }

    void SetWorldPositions(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty(OwnerWorldPosShaderProperty))
            material.SetVector(OwnerWorldPosShaderProperty, _host.ResolveOwnerWorldPosForShader(material));

        if (!material.HasProperty(L2MaterialPropertyCopier.L2FxTargetWorldPosId))
            return;

        bool hasTarget = _host.TryResolveShaderTargetWorldPos(out Vector3 targetWorldPos);
        material.SetVector(L2MaterialPropertyCopier.L2FxTargetWorldPosId, targetWorldPos);
        if (material.HasProperty(L2MaterialPropertyCopier.UseExternalTargetPositionId))
            material.SetFloat(L2MaterialPropertyCopier.UseExternalTargetPositionId, hasTarget ? 1f : 0f);
    }
}
