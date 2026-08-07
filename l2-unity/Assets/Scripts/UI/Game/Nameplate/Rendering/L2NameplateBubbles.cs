using UnityEngine;

/// <summary>
/// L2 DrawTargetName bubble layout (DrawTargetTex left/right of name).
/// Screen size from engine.dll (19×16). Source PNGs are 32×32 with opaque ink only in
/// top-left ~18×19 — UV must crop or transparent pad looks like a gap to the name.
/// </summary>
public static class L2NameplateBubbles
{
    public const float BubbleWidth = 19f;
    public const float BubbleHeight = 16f;
    public const float BubbleYOffset = 1f; // nameY - 1

    // HeadDisplay_DF_Target_* opaque bbox (all three textures).
    public const float AtlasSize = 32f;
    public const float ContentU = 18f;
    public const float ContentV = 19f;

    public const string NormalResourcePath = "Data/UI/Target/HeadDisplay_DF_Target_Normal";
    public const string TargetResourcePath = "Data/UI/Target/HeadDisplay_DF_Target_Target";
    public const string AttackResourcePath = "Data/UI/Target/HeadDisplay_DF_Target_Attack";

    /// <summary>
    /// Append left+right HeadDisplay spheres for one nameplate line.
    /// Canvas top-down pixels; <paramref name="nameLeft"/> / <paramref name="nameW"/> match name glyphs.
    /// </summary>
    public static void AppendPair(
        L2NameplateScreenBatch batch,
        float nameLeft,
        float nameW,
        float yNameTop,
        float depth,
        float screenH,
        float extraPad,
        Color color)
    {
        if (batch == null)
        {
            return;
        }

        float pad = Mathf.Max(0f, extraPad);
        float y0 = yNameTop - BubbleYOffset;
        float y1 = y0 + BubbleHeight;

        // PNG top-left content → Unity UV (V flipped).
        float u0 = 0f;
        float u1 = ContentU / AtlasSize;
        float v1 = 1f;
        float v0 = 1f - (ContentV / AtlasSize);

        float leftX1 = nameLeft - BubbleWidth - pad;
        float leftX2 = nameLeft - pad;
        batch.AppendQuad(leftX1, y0, leftX2, y1, depth, screenH, color, true, u0, v0, u1, v1);

        float rightX1 = nameLeft + nameW;
        float rightX2 = rightX1 + BubbleWidth;
        batch.AppendQuad(rightX1, y0, rightX2, y1, depth, screenH, color, true, u0, v0, u1, v1);
    }

    public static Texture2D LoadTexture(string resourcePath)
    {
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
        }

        return tex;
    }
}
