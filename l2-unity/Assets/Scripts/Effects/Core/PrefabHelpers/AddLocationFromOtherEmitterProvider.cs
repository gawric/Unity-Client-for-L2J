using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replicates engine.dll AddLocationFromOtherEmitter (+0x170): at each tail spawn,
/// add the referenced emitter's live particle location in owner-relative UE space.
/// </summary>
[DisallowMultipleComponent]
public sealed class AddLocationFromOtherEmitterProvider : MonoBehaviour, IParticleSpawnLocationAddProvider
{
    private const float UuToUnityMeters = 1f / 52.5f;

    [Serializable]
    public sealed class Binding
    {
        public ParticleGroupV2 tailEmitter;
        public ParticleGroupV2 sourceEmitter;
        [Tooltip("UC RevolutionsPerSecondRange.Z on the source waterball.")]
        public float revolutionsPerSecondZ;
    }

    [SerializeField] private List<Binding> _bindings = new();

    public bool ContainsTail(ParticleGroupV2 group)
    {
        if (group == null || _bindings == null)
        {
            return false;
        }

        for (int i = 0; i < _bindings.Count; i++)
        {
            Binding binding = _bindings[i];
            if (binding != null && binding.tailEmitter == group)
            {
                return true;
            }
        }

        return false;
    }

    private static readonly int StartLocationRangeXUcID = Shader.PropertyToID("_StartLocationRangeXUc");
    private static readonly int StartLocationRangeYUcID = Shader.PropertyToID("_StartLocationRangeYUc");
    private static readonly int StartLocationRangeZUcID = Shader.PropertyToID("_StartLocationRangeZUc");
    private static readonly int StartLocationOffsetUcID = Shader.PropertyToID("_StartLocationOffsetUc");

    private float _effectStartedAt = -1f;

    private void OnEnable()
    {
        _effectStartedAt = Time.time;
    }

    public bool TryGetSpawnLocationAddUe(EffectPart tailEmitter, float spawnTime, out Vector4 addUe)
    {
        addUe = Vector4.zero;
        if (tailEmitter == null || _bindings == null || _bindings.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < _bindings.Count; i++)
        {
            Binding binding = _bindings[i];
            if (binding == null || binding.tailEmitter != tailEmitter || binding.sourceEmitter == null)
            {
                continue;
            }

            if (!TryResolveSourceLocationUe(binding, spawnTime, out Vector3 locationUe))
            {
                return false;
            }

            Transform sourceTransform = ResolveSourceTransform(binding.sourceEmitter);
            Vector3 locationLocal = new Vector3(
                locationUe.x,
                locationUe.z,
                locationUe.y) * UuToUnityMeters;
            Vector3 locationWorld = sourceTransform.TransformPoint(locationLocal);

            // w=1 marks an absolute world-space position. The shader converts
            // it back through the current tail matrix, so later root rotation
            // cannot drag an already spawned PTCS_Spray particle with the ball.
            addUe = new Vector4(
                locationWorld.x,
                locationWorld.y,
                locationWorld.z,
                1f);
            return true;
        }

        return false;
    }

    bool TryResolveSourceLocationUe(Binding binding, float spawnTime, out Vector3 locationUe)
    {
        locationUe = Vector3.zero;
        Material sourceMat = ResolveSourceMaterial(binding.sourceEmitter);
        if (sourceMat == null)
        {
            return false;
        }

        locationUe = ReadRangeMid(sourceMat, StartLocationRangeXUcID)
            + ReadRangeMid(sourceMat, StartLocationRangeYUcID)
            + ReadRangeMid(sourceMat, StartLocationRangeZUcID);
        if (sourceMat.HasProperty(StartLocationOffsetUcID))
        {
            Vector4 offset = sourceMat.GetVector(StartLocationOffsetUcID);
            locationUe += new Vector3(offset.x, offset.y, offset.z);
        }

        if (Mathf.Abs(binding.revolutionsPerSecondZ) > 1e-6f)
        {
            float age = Mathf.Max(0f, spawnTime - _effectStartedAt);
            locationUe = RotateAroundUcZ(locationUe, age * binding.revolutionsPerSecondZ * Mathf.PI * 2f);
        }

        return locationUe.sqrMagnitude > 1e-6f;
    }

    static Material ResolveSourceMaterial(ParticleGroupV2 sourceEmitter)
    {
        if (sourceEmitter == null)
        {
            return null;
        }

        Material[] gpuMaterials = sourceEmitter.GpuMaterials;
        if (gpuMaterials != null)
        {
            for (int i = 0; i < gpuMaterials.Length; i++)
            {
                if (gpuMaterials[i] != null)
                {
                    return gpuMaterials[i];
                }
            }
        }

        Renderer renderer = sourceEmitter.GetComponentInChildren<Renderer>(true);
        return renderer != null ? renderer.sharedMaterial : null;
    }

    static Transform ResolveSourceTransform(ParticleGroupV2 sourceEmitter)
    {
        Renderer renderer = sourceEmitter.GetComponentInChildren<Renderer>(true);
        return renderer != null ? renderer.transform : sourceEmitter.transform;
    }

    static Vector3 ReadRangeMid(Material mat, int id)
    {
        if (mat == null || !mat.HasProperty(id))
        {
            return Vector3.zero;
        }

        Vector4 range = mat.GetVector(id);
        float mid = (range.x + range.y) * 0.5f;
        if (id == StartLocationRangeXUcID)
        {
            return new Vector3(mid, 0f, 0f);
        }

        if (id == StartLocationRangeYUcID)
        {
            return new Vector3(0f, mid, 0f);
        }

        return new Vector3(0f, 0f, mid);
    }

    static Vector3 RotateAroundUcZ(Vector3 v, float radians)
    {
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);
        return new Vector3(
            v.x * c - v.y * s,
            v.x * s + v.y * c,
            v.z);
    }
}
