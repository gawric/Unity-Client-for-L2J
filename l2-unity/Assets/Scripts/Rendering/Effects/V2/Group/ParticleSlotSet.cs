using UnityEngine;

/// <summary>
/// GameObject slot view for one V2 emitter: clones, hide, GO-path shader start.
/// </summary>
public sealed class ParticleSlotSet
{
    readonly ParticleGroupRendererService _renderers;

    public ParticleSlotSet(EffectPart host)
    {
        _renderers = new ParticleGroupRendererService(host);
    }

    public Renderer[] Particles => _renderers.Particles;
    public int Count => _renderers.Count;

    public void SetParticles(Renderer[] particles) => _renderers.SetParticles(particles);

    public void CollectIfEmpty() => _renderers.CollectIfEmpty();

    public void EnsureClones(bool cloneToMaxCount, int maxCount, int cloneLimit, ref bool clonesCreated) =>
        _renderers.EnsureClones(cloneToMaxCount, maxCount, cloneLimit, ref clonesCreated);

    public void ExpandShaderDrivenBounds() => _renderers.ExpandShaderDrivenBounds();

    public void HideAll() => _renderers.HideAll();

    public void DisableForGpuDraw() => _renderers.DisableForGpuDraw();

    public void EnableForGameObjectDraw() => _renderers.EnableForGameObjectDraw();

    public bool ApplyExpire(
        ParticleGroupSimulation simulation,
        bool[] managedActive,
        float[] managedTimes) =>
        _renderers.ApplyExpire(simulation, managedActive, managedTimes);

    public void ActivateGoSlot(
        int slot,
        float shaderStartTime,
        float seed,
        uint meshRandBase,
        uint spriteRandBase) =>
        _renderers.ActivateGoSlot(slot, shaderStartTime, seed, meshRandBase, spriteRandBase);

    public float ReadShaderSlotDuration(float fallbackDuration) =>
        _renderers.ReadShaderSlotDuration(fallbackDuration);

    public float ReadLifetimeCenter(float fallbackDuration) =>
        _renderers.ReadLifetimeCenter(fallbackDuration);

    public Vector4 ResolveGpuOwnerWorldPos(Material[] gpuMaterials) =>
        _renderers.ResolveGpuOwnerWorldPos(gpuMaterials);

    public Matrix4x4[] ResolveObjectToWorldMatrices() =>
        _renderers.ResolveObjectToWorldMatrices();
}
