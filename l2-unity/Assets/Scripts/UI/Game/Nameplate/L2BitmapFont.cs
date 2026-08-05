using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// UL2Font-style ASCII atlas: glyph table from CSV + one page texture.
/// Glyph layout matches L2 dump: StartU, USize, StartV, VSize (pixels).
/// </summary>
public sealed class L2BitmapFont
{
    public struct Glyph
    {
        public int U;
        public int V;
        public int W;
        public int H;
        public bool Valid;
    }

    private readonly Texture2D _atlas;
    private readonly Glyph[] _byCode = new Glyph[128];
    private readonly float _invW;
    private readonly float _invH;
    private int _lineHeight = 14;

    public Texture2D Atlas => _atlas;
    public int LineHeight => _lineHeight;
    public float AtlasWidth => _atlas != null ? _atlas.width : 1f;
    public float AtlasHeight => _atlas != null ? _atlas.height : 1f;

    public L2BitmapFont(Texture2D atlas, TextAsset csv)
    {
        _atlas = atlas;
        _invW = 1f / Mathf.Max(1, atlas != null ? atlas.width : 1024);
        _invH = 1f / Mathf.Max(1, atlas != null ? atlas.height : 128);

        if (csv == null || string.IsNullOrEmpty(csv.text))
        {
            return;
        }

        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (i == 0 && line.StartsWith("char", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // char,code,u,v,w,h,page — char may be comma → ",,44,..."
            string payload;
            if (line.Length >= 2 && line[0] == ',' && line[1] == ',')
            {
                payload = line.Substring(2);
            }
            else
            {
                int firstComma = line.IndexOf(',');
                if (firstComma < 0)
                {
                    continue;
                }

                payload = line.Substring(firstComma + 1);
            }

            string[] parts = payload.Split(',');
            if (parts.Length < 5)
            {
                continue;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int code))
            {
                continue;
            }

            if (code < 0 || code >= _byCode.Length)
            {
                continue;
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int u) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) ||
                !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h))
            {
                continue;
            }

            _byCode[code] = new Glyph { U = u, V = v, W = w, H = h, Valid = w > 0 && h > 0 };
            if (h > _lineHeight)
            {
                _lineHeight = h;
            }
        }
    }

    public static L2BitmapFont LoadFromResources(
        string atlasResourcePath = "Data/UI/Font/L2Lobby/l2_lobby_font_atlas",
        string csvResourcePath = "Data/UI/Font/L2Lobby/ul2font_ascii")
    {
        Texture2D atlas = Resources.Load<Texture2D>(atlasResourcePath);
        TextAsset csv = Resources.Load<TextAsset>(csvResourcePath);
        if (atlas == null)
        {
            Debug.LogError($"[L2BitmapFont] Atlas not found at Resources/{atlasResourcePath}");
            return null;
        }

        if (csv == null)
        {
            Debug.LogError($"[L2BitmapFont] CSV not found at Resources/{csvResourcePath}");
            return null;
        }

        return new L2BitmapFont(atlas, csv);
    }

    public bool TryGetGlyph(char ch, out Glyph glyph)
    {
        int code = ch;
        if (code < 0 || code >= _byCode.Length || !_byCode[code].Valid)
        {
            glyph = default;
            return false;
        }

        glyph = _byCode[code];
        return true;
    }

    public float MeasureWidth(string text, float pixelScale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        float w = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            if (TryGetGlyph(text[i], out Glyph g))
            {
                w += g.W * pixelScale;
            }
        }

        return w;
    }

    public float MeasureHeight(float pixelScale)
    {
        return _lineHeight * pixelScale;
    }

    /// <summary>
    /// Emit screen-space quads. Origin is top-left of the string; Y increases downward (L2/GUI).
    /// </summary>
    public void AppendString(
        string text,
        float x,
        float y,
        float pixelScale,
        Color32 color,
        List<Vector3> verts,
        List<Vector2> uvs,
        List<Color32> colors,
        List<int> indices)
    {
        if (string.IsNullOrEmpty(text) || verts == null)
        {
            return;
        }

        float penX = x;
        float invW = _invW;
        float invH = _invH;

        for (int i = 0; i < text.Length; i++)
        {
            if (!TryGetGlyph(text[i], out Glyph g))
            {
                continue;
            }

            float gw = g.W * pixelScale;
            float gh = g.H * pixelScale;
            int vi = verts.Count;

            // Quad: TL, TR, BR, BL (Y down)
            verts.Add(new Vector3(penX, y, 0f));
            verts.Add(new Vector3(penX + gw, y, 0f));
            verts.Add(new Vector3(penX + gw, y + gh, 0f));
            verts.Add(new Vector3(penX, y + gh, 0f));

            float u0 = g.U * invW;
            float u1 = (g.U + g.W) * invW;
            // Atlas V=0 is top strip; Unity UV V=0 is bottom — flip.
            float vTop = 1f - (g.V * invH);
            float vBot = 1f - ((g.V + g.H) * invH);
            uvs.Add(new Vector2(u0, vTop));
            uvs.Add(new Vector2(u1, vTop));
            uvs.Add(new Vector2(u1, vBot));
            uvs.Add(new Vector2(u0, vBot));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            indices.Add(vi);
            indices.Add(vi + 1);
            indices.Add(vi + 2);
            indices.Add(vi);
            indices.Add(vi + 2);
            indices.Add(vi + 3);

            penX += gw;
        }
    }

    /// <summary>
    /// IMGUI draw. Origin = top-left of string; Y down. Caller sets GUI.color (L2: tex * color).
    /// </summary>
    public void DrawStringGUI(string text, float x, float y, float pixelScale)
    {
        if (string.IsNullOrEmpty(text) || _atlas == null)
        {
            return;
        }

        float penX = x;
        float aw = AtlasWidth;
        float ah = AtlasHeight;

        for (int i = 0; i < text.Length; i++)
        {
            if (!TryGetGlyph(text[i], out Glyph g))
            {
                continue;
            }

            float gw = g.W * pixelScale;
            float gh = g.H * pixelScale;
            // Unity texcoords: V=0 at bottom; L2 atlas V=0 at top.
            Rect uv = new Rect(g.U / aw, 1f - (g.V + g.H) / ah, g.W / aw, g.H / ah);
            GUI.DrawTextureWithTexCoords(new Rect(penX, y, gw, gh), _atlas, uv);
            penX += gw;
        }
    }
}
