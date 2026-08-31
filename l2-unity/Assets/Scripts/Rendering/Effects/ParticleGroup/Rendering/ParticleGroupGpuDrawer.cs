using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws identical ParticleGroup slots in one instanced call.
/// Shader must tag <c>L2FxGpuInstancing=On</c> and include L2FxInstancing.hlsl.
/// Motion stays in Decompile_Common; this only batches the draw.
/// </summary>
public sealed class ParticleGroupGpuDrawer
{
    public const string ShaderTag = "L2FxGpuInstancing";
    public const string ShaderTagOn = "On";

    private static readonly int SlotsBufferId = Shader.PropertyToID("_L2FxParticleSlots");
    private static readonly int SlotStride = Marshal.SizeOf<L2FxParticleInstance>();

    private ComputeBuffer _slotsBuffer;
    private MaterialPropertyBlock _properties;

    public static bool TryBind(
        Renderer[] particles,
        out Mesh mesh,
        out Material[] materials,
        out int layer,
        out int rendererPriority)
    {
        mesh = null;
        materials = null;
        layer = 0;
        rendererPriority = 0;
        if (particles == null || particles.Length == 0)
            return false;

        Renderer first = particles[0];
        if (first == null || first is SkinnedMeshRenderer)
            return false;

        MeshFilter filter = first.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return false;

        Material[] shared = first.sharedMaterials;
        if (shared == null || shared.Length == 0)
            return false;

        for (int m = 0; m < shared.Length; m++)
        {
            if (!SupportsGpuInstancing(shared[m]))
                return false;
        }

        mesh = filter.sharedMesh;
        for (int i = 1; i < particles.Length; i++)
        {
            Renderer renderer = particles[i];
            if (renderer == null || renderer is SkinnedMeshRenderer)
                return false;

            MeshFilter other = renderer.GetComponent<MeshFilter>();
            if (other == null || other.sharedMesh != mesh)
                return false;
            Material[] otherMats = renderer.sharedMaterials;
            if (otherMats == null || otherMats.Length != shared.Length)
                return false;
            for (int m = 0; m < shared.Length; m++)
            {
                if (otherMats[m] != shared[m])
                    return false;
            }
        }

        materials = shared;
        layer = first.gameObject.layer;
        // Unity 6.0 RenderParams has no sortingLayerID/sortingOrder.
        // MeshRenderer.sortingOrder still applies to GO draws; GPU uses rendererPriority.
        rendererPriority = first.sortingOrder != 0 ? first.sortingOrder : first.rendererPriority;
        for (int m = 0; m < materials.Length; m++)
        {
            if (materials[m] != null)
                materials[m].enableInstancing = true;
        }

        return true;
    }

    public static bool SupportsGpuInstancing(Material material)
    {
        return material != null &&
               material.GetTag(ShaderTag, false, string.Empty) == ShaderTagOn;
    }

    public void Draw(
        Mesh mesh,
        Material[] materials,
        int layer,
        int rendererPriority,
        NativeArray<L2FxParticleInstance> packedSlots,
        NativeArray<Matrix4x4> matrices,
        int packed)
    {
        if (mesh == null || materials == null || packed <= 0 ||
            !packedSlots.IsCreated || !matrices.IsCreated)
            return;

        EnsureBuffer(packed);
        _slotsBuffer.SetData(packedSlots, 0, 0, packed);
        _properties ??= new MaterialPropertyBlock();
        _properties.Clear();
        _properties.SetBuffer(SlotsBufferId, _slotsBuffer);

        Bounds worldBounds = TransformBounds(mesh.bounds, matrices[0]);
        for (int i = 1; i < packed; i++)
            worldBounds.Encapsulate(TransformBounds(mesh.bounds, matrices[i]));
        worldBounds.Expand(8f);

        for (int submesh = 0; submesh < materials.Length; submesh++)
        {
            Material material = materials[submesh];
            if (material == null)
                continue;

            var rp = new RenderParams(material)
            {
                worldBounds = worldBounds,
                layer = layer,
                rendererPriority = rendererPriority,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                matProps = _properties
            };
            Graphics.RenderMeshInstanced(rp, mesh, submesh, matrices, packed);
        }
    }

    void EnsureBuffer(int count)
    {
        if (_slotsBuffer != null && _slotsBuffer.count >= count)
            return;

        _slotsBuffer?.Release();
        int size = Mathf.NextPowerOfTwo(Mathf.Max(count, 8));
        _slotsBuffer = new ComputeBuffer(size, SlotStride);
    }

    public void Release()
    {
        _slotsBuffer?.Release();
        _slotsBuffer = null;
        _properties = null;
    }

    static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
    {
        Vector3 center = matrix.MultiplyPoint3x4(local.center);
        Vector3 ext = local.extents;
        Vector3 axisX = matrix.MultiplyVector(new Vector3(ext.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, ext.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, ext.z));
        Vector3 worldExt = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(center, worldExt * 2f);
    }
}
