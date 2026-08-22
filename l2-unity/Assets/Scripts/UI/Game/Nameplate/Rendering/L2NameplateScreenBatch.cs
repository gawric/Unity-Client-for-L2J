using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shared L2 canvas glyph batch: screen pixels → StructuredBuffer → one RenderPrimitivesIndexed.
/// Used by <see cref="NameplatesManager"/> and <see cref="LobbyNameplatesManager"/>.
/// </summary>
public sealed class L2NameplateScreenBatch : IDisposable
{
    private const string ShaderResourcePath = "Data/Shaders/UI/L2BitmapFontScreenSpace";
    private const string ShaderName = "L2/UI/BitmapFontScreenSpace";
    private const int GlyphStride = 32;

    private readonly List<Vector3> _pixelVerts = new List<Vector3>(256);
    private readonly List<Vector2> _uvs = new List<Vector2>(256);
    private readonly List<Color32> _colors = new List<Color32>(256);
    private readonly List<int> _indices = new List<int>(384);
    private readonly List<GlyphVertex> _glyphs = new List<GlyphVertex>(256);
    private readonly Bounds _drawBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

    private Material _material;
    private GraphicsBuffer _glyphBuffer;
    private GraphicsBuffer _indexBuffer;
    private GlyphVertex[] _glyphScratch;
    private int[] _indexScratch;
    private int _glyphCapacity;
    private int _indexCapacity;
    private int _drawIndexCount;
    private bool _disposed;

    [StructLayout(LayoutKind.Sequential)]
    private struct GlyphVertex
    {
        public Vector2 ScreenPos;
        public float Depth;
        public float Pad0;
        public Vector2 UV;
        public uint Color;
        public uint Pad1;
    }

    public bool IsReady => _material != null;

    public void EnsureMaterial(Texture atlas)
    {
        if (_material != null)
        {
            if (atlas != null)
            {
                _material.mainTexture = atlas;
                _material.SetTexture("_MainTex", atlas);
            }

            return;
        }

        Shader shader = Resources.Load<Shader>(ShaderResourcePath);
        if (shader == null)
        {
            shader = Shader.Find(ShaderName);
        }

        if (shader == null)
        {
            Debug.LogError($"[L2NameplateScreenBatch] Shader '{ShaderName}' not found.");
            return;
        }

        _material = new Material(shader)
        {
            name = "L2ScreenBitmapFont (Shared)",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = 5000
        };
        if (atlas != null)
        {
            _material.mainTexture = atlas;
            _material.SetTexture("_MainTex", atlas);
        }
    }

    public void BeginFrame()
    {
        _pixelVerts.Clear();
        _uvs.Clear();
        _colors.Clear();
        _indices.Clear();
        _glyphs.Clear();
        _drawIndexCount = 0;
    }

    /// <summary>
    /// L2 canvas line: Floor pixel origin + optional frac discard, AppendString → screen verts (Y up).
    /// </summary>
    public void AppendLine(
        L2BitmapFont font,
        string text,
        float x,
        float yTop,
        float scale,
        Color color,
        float depth,
        float screenH,
        bool discardSubpixelFrac)
    {
        if (font == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        float ox = Mathf.Floor(x);
        float oy = Mathf.Floor(yTop);
        float fracX = x - ox;
        float fracY = yTop - oy;
        if (discardSubpixelFrac)
        {
            fracX = 0f;
            fracY = 0f;
        }

        int vertStart = _pixelVerts.Count;
        font.AppendString(text, ox, oy, scale, color, _pixelVerts, _uvs, _colors, _indices);

        uint packed = PackColor(color);
        for (int v = vertStart; v < _pixelVerts.Count; v++)
        {
            Vector3 p = _pixelVerts[v];
            float sx = p.x + fracX;
            float sy = (screenH - p.y) - fracY;
            _glyphs.Add(new GlyphVertex
            {
                ScreenPos = new Vector2(sx, sy),
                Depth = depth,
                Pad0 = 0f,
                UV = _uvs[v],
                Color = packed,
                Pad1 = 0u
            });
        }
    }

    /// <summary>
    /// L2 DrawTile / DrawTargetTex quad. <paramref name="x1"/>..<paramref name="y2"/> are
    /// canvas top-down pixels (same space as <see cref="AppendLine"/> yTop).
    /// UV defaults to full texture; pass atlas pixel rect for cropped HeadDisplay sprites.
    /// </summary>
    public void AppendQuad(
        float x1,
        float y1,
        float x2,
        float y2,
        float depth,
        float screenH,
        Color color,
        bool snapPixels = true,
        float u0 = 0f,
        float v0 = 0f,
        float u1 = 1f,
        float v1 = 1f)
    {
        if (snapPixels)
        {
            x1 = Mathf.Floor(x1);
            y1 = Mathf.Floor(y1);
            x2 = Mathf.Floor(x2);
            y2 = Mathf.Floor(y2);
        }

        if (x2 <= x1 || y2 <= y1)
        {
            return;
        }

        uint packed = PackColor(color);
        int baseIndex = _glyphs.Count;

        // TL, TR, BR, BL in canvas top-down. Unity UV V=0 at texture bottom.
        // v1 = top of source rect, v0 = bottom (Unity space).
        AddQuadVert(x1, y1, u0, v1, depth, screenH, packed);
        AddQuadVert(x2, y1, u1, v1, depth, screenH, packed);
        AddQuadVert(x2, y2, u1, v0, depth, screenH, packed);
        AddQuadVert(x1, y2, u0, v0, depth, screenH, packed);

        _indices.Add(baseIndex);
        _indices.Add(baseIndex + 1);
        _indices.Add(baseIndex + 2);
        _indices.Add(baseIndex);
        _indices.Add(baseIndex + 2);
        _indices.Add(baseIndex + 3);
    }

    private void AddQuadVert(
        float canvasX,
        float canvasY,
        float u,
        float v,
        float depth,
        float screenH,
        uint packed)
    {
        _glyphs.Add(new GlyphVertex
        {
            ScreenPos = new Vector2(canvasX, screenH - canvasY),
            Depth = depth,
            Pad0 = 0f,
            UV = new Vector2(u, v),
            Color = packed,
            Pad1 = 0u
        });
    }

    public bool HasGeometry => _glyphs.Count > 0 && _indices.Count > 0;

    /// <summary>Upload buffers and submit one indexed procedural draw for <paramref name="cam"/>.</summary>
    public bool UploadAndDraw(Camera cam)
    {
        if (_disposed || _material == null || cam == null || !HasGeometry)
        {
            return false;
        }

        EnsureBuffers(_glyphs.Count, _indices.Count);

        for (int i = 0; i < _glyphs.Count; i++)
        {
            _glyphScratch[i] = _glyphs[i];
        }

        for (int i = 0; i < _indices.Count; i++)
        {
            _indexScratch[i] = _indices[i];
        }

        _glyphBuffer.SetData(_glyphScratch, 0, 0, _glyphs.Count);
        _indexBuffer.SetData(_indexScratch, 0, 0, _indices.Count);
        _drawIndexCount = _indices.Count;

        _material.SetBuffer("_GlyphBuffer", _glyphBuffer);
        var rp = new RenderParams(_material)
        {
            worldBounds = _drawBounds,
            camera = cam,
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            layer = 0
        };
        Graphics.RenderPrimitivesIndexed(
            rp,
            MeshTopology.Triangles,
            _indexBuffer,
            _drawIndexCount,
            0,
            1);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseBuffers();

        if (_material != null)
        {
            UnityEngine.Object.Destroy(_material);
            _material = null;
        }
    }

    private void EnsureBuffers(int glyphCount, int indexCount)
    {
        if (glyphCount <= 0 || indexCount <= 0)
        {
            return;
        }

        if (_glyphBuffer == null || _glyphCapacity < glyphCount)
        {
            _glyphBuffer?.Release();
            _glyphCapacity = Mathf.NextPowerOfTwo(Mathf.Max(256, glyphCount));
            _glyphBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                _glyphCapacity,
                GlyphStride);
            _glyphScratch = new GlyphVertex[_glyphCapacity];
        }

        if (_indexBuffer == null || _indexCapacity < indexCount)
        {
            _indexBuffer?.Release();
            _indexCapacity = Mathf.NextPowerOfTwo(Mathf.Max(384, indexCount));
            _indexBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Index,
                _indexCapacity,
                sizeof(int));
            _indexScratch = new int[_indexCapacity];
        }
    }

    private void ReleaseBuffers()
    {
        if (_glyphBuffer != null)
        {
            _glyphBuffer.Release();
            _glyphBuffer = null;
        }

        if (_indexBuffer != null)
        {
            _indexBuffer.Release();
            _indexBuffer = null;
        }

        _glyphCapacity = 0;
        _indexCapacity = 0;
        _glyphScratch = null;
        _indexScratch = null;
    }

    private static uint PackColor(Color32 c)
    {
        return (uint)(c.r | (c.g << 8) | (c.b << 16) | (c.a << 24));
    }
}
