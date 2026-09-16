using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Nameplates submitted at beginCamera would land in the UNORM encode and get
/// covered by haze. Queue them and flush after compositor decode onto the camera.
/// </summary>
public static class L2NameplateOverlayQueue
{
    struct Item
    {
        public Material Material;
        public GraphicsBuffer GlyphBuffer;
        public GraphicsBuffer IndexBuffer;
        public int IndexCount;
        public Camera Camera;
    }

    static readonly List<Item> s_Items = new List<Item>(8);
    static readonly Bounds DrawBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

    public static void Enqueue(
        Material material,
        GraphicsBuffer glyphBuffer,
        GraphicsBuffer indexBuffer,
        int indexCount,
        Camera camera)
    {
        if (material == null || glyphBuffer == null || indexBuffer == null || indexCount <= 0)
            return;

        s_Items.Add(new Item
        {
            Material = material,
            GlyphBuffer = glyphBuffer,
            IndexBuffer = indexBuffer,
            IndexCount = indexCount,
            Camera = camera
        });
    }

    public static void Flush(CommandBuffer cmd)
    {
        if (cmd == null || s_Items.Count == 0)
        {
            s_Items.Clear();
            return;
        }

        for (int i = 0; i < s_Items.Count; i++)
        {
            Item item = s_Items[i];
            if (item.Material == null || item.IndexBuffer == null || item.IndexCount <= 0)
                continue;

            item.Material.SetBuffer("_GlyphBuffer", item.GlyphBuffer);
            cmd.DrawProcedural(
                item.IndexBuffer,
                Matrix4x4.identity,
                item.Material,
                0,
                MeshTopology.Triangles,
                item.IndexCount,
                1);
        }

        s_Items.Clear();
    }

    public static void FlushImmediateFallback()
    {
        for (int i = 0; i < s_Items.Count; i++)
            DrawNow(s_Items[i]);
        s_Items.Clear();
    }

    public static void Clear()
    {
        s_Items.Clear();
    }

    static void DrawNow(Item item)
    {
        if (item.Material == null || item.IndexBuffer == null || item.IndexCount <= 0)
            return;

        item.Material.SetBuffer("_GlyphBuffer", item.GlyphBuffer);
        var rp = new RenderParams(item.Material)
        {
            worldBounds = DrawBounds,
            camera = item.Camera,
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            layer = 0
        };
        Graphics.RenderPrimitivesIndexed(
            rp,
            MeshTopology.Triangles,
            item.IndexBuffer,
            item.IndexCount,
            0,
            1);
    }
}
