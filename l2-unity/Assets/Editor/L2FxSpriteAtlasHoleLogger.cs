#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Logs RGB/A stats for L2 sprite atlas cells to explain AlphaBlend "black holes":
/// dark RGB + high alpha punches dst * (1-a).
/// Menu: Tools/L2 Effects/Log Sprite Atlas Hole Stats
/// </summary>
public static class L2FxSpriteAtlasHoleLogger
{
    const float DarkLuma = 0.25f;
    const float OpaqueAlpha = 0.5f;

    [MenuItem("Tools/L2 Effects/Log Sprite Atlas Hole Stats")]
    static void LogFromSelection()
    {
        Material mat = Selection.activeObject as Material;
        if (mat == null)
        {
            Renderer renderer = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInChildren<Renderer>(true)
                : null;
            mat = renderer != null ? renderer.sharedMaterial : null;
        }

        Texture tex = mat != null && mat.HasProperty("_MainTex")
            ? mat.GetTexture("_MainTex")
            : null;
        if (tex == null)
        {
            tex = Selection.activeObject as Texture;
        }

        if (tex == null)
        {
            Debug.LogError(
                "[L2FxAtlasHole] Select a steam material, renderer, or fx_m_t0035 texture.");
            return;
        }

        int uSub = mat != null && mat.HasProperty("_TextureUSubdivisions")
            ? Mathf.Max(1, Mathf.RoundToInt(mat.GetFloat("_TextureUSubdivisions")))
            : 8;
        int vSub = mat != null && mat.HasProperty("_TextureVSubdivisions")
            ? Mathf.Max(1, Mathf.RoundToInt(mat.GetFloat("_TextureVSubdivisions")))
            : 8;
        int start = mat != null && mat.HasProperty("_SubdivisionStart")
            ? Mathf.RoundToInt(mat.GetFloat("_SubdivisionStart"))
            : 0;
        int end = mat != null && mat.HasProperty("_SubdivisionEnd")
            ? Mathf.RoundToInt(mat.GetFloat("_SubdivisionEnd"))
            : uSub * vSub - 1;
        if (mat == null)
        {
            Debug.LogWarning(
                "[L2FxAtlasHole] Texture selected without a material: using 8x8 and all frames. " +
                "Select SpriteEmitter5213/5214 to inspect the actual steam range 4..15.");
        }

        if (end < start)
        {
            int swap = start;
            start = end;
            end = swap;
        }

        LogAtlas(tex, mat, uSub, vSub, start, end);
    }

    static void LogAtlas(Texture tex, Material mat, int uSub, int vSub, int start, int end)
    {
        Texture2D tex2d = tex as Texture2D;
        if (tex2d == null)
        {
            Debug.LogError("[L2FxAtlasHole] Not a Texture2D: " + tex.name);
            return;
        }

        string path = AssetDatabase.GetAssetPath(tex2d);
        var importer = UnityEditor.AssetImporter.GetAtPath(path) as TextureImporter;
        var sb = new StringBuilder(2048);
        sb.AppendLine("[L2FxAtlasHole] texture=" + tex2d.name);
        sb.AppendLine("  path=" + path);
        sb.AppendLine("  size=" + tex2d.width + "x" + tex2d.height +
                      " format=" + tex2d.format + " readable=" + tex2d.isReadable);
        if (importer != null)
        {
            sb.AppendLine("  importer sRGB=" + importer.sRGBTexture +
                          " alphaSource=" + importer.alphaSource +
                          " alphaIsTransparency=" + importer.alphaIsTransparency +
                          " mipmaps=" + importer.mipmapEnabled +
                          " filter=" + importer.filterMode);
        }

        if (mat != null)
        {
            sb.AppendLine("  material=" + mat.name +
                          " shader=" + mat.shader.name);
            AppendFloat(sb, mat, "_SrcBlend");
            AppendFloat(sb, mat, "_DstBlend");
            AppendFloat(sb, mat, "_Opacity");
            AppendFloat(sb, mat, "_FlipbookMode");
            AppendFloat(sb, mat, "_ColorFadeAlphaBlend");
            AppendFloat(sb, mat, "_IgnoreMainTexAlpha");
            AppendFloat(sb, mat, "_AlphaFromLuma");
            AppendFloat(sb, mat, "_DebugSpriteOut");
            sb.AppendLine("  cells=" + uSub + "x" + vSub +
                          " frames=" + start + ".." + end);
        }

        if (!tex2d.isReadable)
        {
            sb.AppendLine("  ERROR: texture is not readable. Enable Read/Write and Reimport.");
            Debug.Log(sb.ToString());
            return;
        }

        Color[] pixels = tex2d.GetPixels();
        int cellW = tex2d.width / uSub;
        int cellH = tex2d.height / vSub;
        int cells = uSub * vSub;
        int holeCells = 0;
        for (int frame = start; frame <= end && frame < cells; frame++)
        {
            // Must match L2Fx_FlipbookAtlasUV exactly:
            // u = index / V, v = V - 1 - (index % V).
            int col = frame / vSub;
            int row = (vSub - 1) - (frame % vSub);
            int x0 = col * cellW;
            int y0 = row * cellH;
            CellStats stats = SampleCell(pixels, tex2d.width, x0, y0, cellW, cellH);
            if (stats.holeFrac > 0.05f)
            {
                holeCells++;
            }

            sb.AppendFormat(
                "  cell {0,2} uv=({1},{2}) n={3} rgb=({4:F3},{5:F3},{6:F3}) " +
                "luma={7:F3} aAvg={8:F3} aMax={9:F3} coveredLuma={10:F3} hole={11:P0} " +
                "(luma<{12:F2} && a>{13:F2})\n",
                frame, col, row, stats.count,
                stats.avgR, stats.avgG, stats.avgB, stats.avgLuma, stats.avgA,
                stats.maxA, stats.coveredAvgLuma, stats.holeFrac, DarkLuma, OpaqueAlpha);
        }

        sb.AppendLine("  holeCells=" + holeCells +
                      "  (hole = luma<" + DarkLuma.ToString("0.00") +
                      " AND a>" + OpaqueAlpha.ToString("0.00") + ")");
        sb.AppendLine("  If hole% is high: AlphaBlend dst*(1-a)+src*a punches black circles.");
        sb.AppendLine("  Shader debug on material _DebugSpriteOut: 1=texA 2=luma 3=rgb 4=holes(R=hole,G=luma,B=a).");
        Debug.Log(sb.ToString());

        string outPath = Path.Combine("Temp", "l2fx-atlas-hole-" + tex2d.name + ".txt");
        Directory.CreateDirectory("Temp");
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log("[L2FxAtlasHole] wrote " + Path.GetFullPath(outPath));
    }

    static void AppendFloat(StringBuilder sb, Material mat, string prop)
    {
        if (mat.HasProperty(prop))
        {
            sb.AppendLine("  " + prop + "=" + mat.GetFloat(prop));
        }
    }

    struct CellStats
    {
        public int count;
        public int holeCount;
        public int coveredCount;
        public float holeFrac;
        public float avgR, avgG, avgB, avgA, maxA, avgLuma, coveredAvgLuma;
    }

    static CellStats SampleCell(Color[] pixels, int width, int x0, int y0, int cellW, int cellH)
    {
        CellStats s = default;
        float r = 0f, g = 0f, b = 0f, a = 0f, luma = 0f, coveredLuma = 0f;
        int maxX = Mathf.Min(x0 + cellW, width);
        int maxY = Mathf.Min(y0 + cellH, pixels.Length / width);
        for (int y = y0; y < maxY; y++)
        {
            int row = y * width;
            for (int x = x0; x < maxX; x++)
            {
                Color c = pixels[row + x];
                float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                r += c.r;
                g += c.g;
                b += c.b;
                a += c.a;
                s.maxA = Mathf.Max(s.maxA, c.a);
                luma += lum;
                s.count++;
                if (c.a > 0.05f)
                {
                    coveredLuma += lum;
                    s.coveredCount++;
                }

                if (lum < DarkLuma && c.a > OpaqueAlpha)
                {
                    s.holeCount++;
                }
            }
        }

        if (s.count <= 0)
        {
            return s;
        }

        float inv = 1f / s.count;
        s.avgR = r * inv;
        s.avgG = g * inv;
        s.avgB = b * inv;
        s.avgA = a * inv;
        s.avgLuma = luma * inv;
        s.coveredAvgLuma = s.coveredCount > 0
            ? coveredLuma / s.coveredCount
            : 0f;
        s.holeFrac = s.holeCount * inv;
        return s;
    }
}
#endif
