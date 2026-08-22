#ifndef L2_FX_RIBBON_GET_POINT_INCLUDED
#define L2_FX_RIBBON_GET_POINT_INCLUDED

// URibbonEmitter::GetNewRibbonPoint — live-verified Interlude RibbonSet (2026-07-28).
//
// RibbonSet defaults (CoordSys==1, AxisFrom=PAXIS_BoneNormal):
//   a3 = Pos - N * S * R
//   a2 = Pos + N * S * (1 - R)
//   |a2 - a3| == S
// where:
//   Pos = sample origin (owner-local bone position for CoordSys 1)
//   N   = unit bone normal / width axis
//   S   = ScaleRatio  (emitter +0x4F0)
//   R   = EdgeRatio   (emitter +0x50C)
//
// Outputs a2/a3 are owner-local edge positions; a4 = S (written ScaleRatio).
// FRibbonPoint layout (stride 52): +0 a2, +12 a3, +48 a4.
//
// Unity blade path: a2/a3 = tip/hilt world ends when |tip-hilt| replaces S.
// Prefer MeshFilter local bounds transformed through the mesh transform
// (C# L2FxRibbonGetPoint.TryGetMeshBladeEnds) — Sword_Tip/Base markers often
// sit on the weapon root with mesh-local coords and under-span the blade.
//
// Frame gaps: Unity only has one animated pose per frame. When tip/base/mid
// travel exceeds max segment length, C# LerpEdgesArc inserts intermediate
// sheets (CountInterpSheetsForEdges) — Slerp around weapon pivot so large
// swings keep a curved trail instead of linear chords.
//
// Tip/hilt polarity: StabilizeEdgePolarity keeps a2/a3 from flipping when
// Sword_Tip/Base naming disagrees with mesh-bounds max/min convention.
//
// Do not replace this with effect-local hacks — keep formulas here.

struct L2FxRibbonEdges
{
    float3 a2;
    float3 a3;
    float a4;
};

// CoordSys == 1 (RibbonSet / owner-local BoneNormal path).
L2FxRibbonEdges L2FxRibbon_GetPointCoordSys1(float3 pos, float3 normalUnit, float scaleRatio, float edgeRatio)
{
    L2FxRibbonEdges o;
    float3 n = normalUnit;
    float nLenSq = dot(n, n);
    if (nLenSq > 1e-12)
    {
        n *= rsqrt(nLenSq);
    }
    else
    {
        n = float3(1.0, 0.0, 0.0);
    }

    float s = scaleRatio;
    float r = edgeRatio;
    o.a3 = pos - n * (s * r);
    o.a2 = pos + n * (s * (1.0 - r));
    o.a4 = s;
    return o;
}

// Midpoint between edges (equals Pos when R is consistent).
float3 L2FxRibbon_EdgeMid(float3 a2, float3 a3)
{
    return (a2 + a3) * 0.5;
}

// Across-ribbon UV: 0 at a3, 1 at a2. Along-trail UV supplied by CPU strip builder.
float2 L2FxRibbon_StripUv(float across01, float along01)
{
    return float2(across01, along01);
}

#endif
