using UnityEngine;

/// <summary>
/// Instanced draw for one V2 emitter. Motion stays in Decompile_Common;
/// this only binds identical mesh+material slots and issues the batch.
/// </summary>
public sealed class ParticleDrawBatch
{
    readonly ParticleGroupGpuDrawer _drawer = new ParticleGroupGpuDrawer();
    bool _bound;

    public bool Bound => _bound;

    public bool TryBind(
        Renderer[] particles,
        bool useGpuInstancing,
        ParticleGroupSimulation simulation)
    {
        _bound = simulation != null &&
                 simulation.TryBindGpu(particles, useGpuInstancing);
        return _bound;
    }

    public void Draw(
        ParticleGroupSimulation simulation,
        Vector4 ownerWorldPos,
        Matrix4x4[] objectToWorldMatrices)
    {
        if (!_bound ||
            simulation == null ||
            !simulation.TryPack(ownerWorldPos, objectToWorldMatrices, out int packed))
        {
            return;
        }

        _drawer.Draw(
            simulation.GpuMesh,
            simulation.GpuMaterials,
            simulation.GpuLayer,
            simulation.GpuRendererPriority,
            simulation.PackedSlots,
            simulation.PackedMatrices,
            packed);
    }

    public void Release()
    {
        _bound = false;
        _drawer.Release();
    }
}
