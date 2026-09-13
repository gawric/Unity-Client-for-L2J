using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Queues GPU-instanced L2 FX draws so they can flush inside the compositor pass
/// instead of LateUpdate (which would miss the UNORM target).
/// </summary>
public static class L2FxGpuDrawQueue
{
    public struct DrawItem
    {
        public Mesh Mesh;
        public Material Material;
        public int Submesh;
        public int Layer;
        public int RendererPriority;
        public Bounds WorldBounds;
        public ComputeBuffer SlotsBuffer;
        public Matrix4x4[] Matrices;
        public int InstanceCount;
        public MaterialPropertyBlock Properties;
    }

    static readonly List<DrawItem> s_Items = new List<DrawItem>(64);
    static bool s_Capture;
    static int s_Frame;

    public static bool IsCapturing => s_Capture;

    public static void BeginFrameCapture()
    {
        int frame = Time.frameCount;
        if (s_Frame != frame)
        {
            s_Items.Clear();
            s_Frame = frame;
        }

        s_Capture = true;
    }

    public static void EndFrameCapture()
    {
        s_Capture = false;
    }

    public static void Enqueue(DrawItem item)
    {
        if (item.Mesh == null || item.Material == null || item.InstanceCount <= 0)
            return;
        s_Items.Add(item);
    }

    public static void FlushImmediateFallback()
    {
        // Compositor disabled: draw now like the old LateUpdate path.
        for (int i = 0; i < s_Items.Count; i++)
            DrawItemNow(s_Items[i]);
        s_Items.Clear();
        s_Capture = false;
    }

    public static void Flush(CommandBuffer cmd)
    {
        if (cmd == null || s_Items.Count == 0)
        {
            s_Items.Clear();
            return;
        }

        s_Items.Sort((a, b) => a.RendererPriority.CompareTo(b.RendererPriority));

        for (int i = 0; i < s_Items.Count; i++)
        {
            DrawItem item = s_Items[i];
            if (item.Mesh == null || item.Material == null || item.InstanceCount <= 0)
                continue;

            // Must use CommandBuffer so draws hit the UNORM target set by the pass.
            cmd.DrawMeshInstanced(
                item.Mesh,
                item.Submesh,
                item.Material,
                0,
                item.Matrices,
                item.InstanceCount,
                item.Properties);
        }

        s_Items.Clear();
    }

    static void DrawItemNow(DrawItem item)
    {
        var rp = new RenderParams(item.Material)
        {
            worldBounds = item.WorldBounds,
            layer = item.Layer,
            rendererPriority = item.RendererPriority,
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            matProps = item.Properties
        };
        Graphics.RenderMeshInstanced(rp, item.Mesh, item.Submesh, item.Matrices, item.InstanceCount);
    }

    public static void Clear()
    {
        s_Items.Clear();
        s_Capture = false;
    }
}
