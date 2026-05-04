#ifndef L2_FX_FLIPBOOK_INCLUDED
#define L2_FX_FLIPBOOK_INCLUDED

#include "L2FxParticleAnim.hlsl"

// Unreal / Lineage 2 sprite sheet: TextureUSubdivisions x TextureVSubdivisions cells,
// SubdivisionStart .. SubdivisionEnd (inclusive indices into the U*V grid),
// optional BlendBetweenSubdivisions (lerp adjacent frames in time).
//
// Gawric SubDivision.shadersubgraph parity: random frame in [frameStart, frameEnd] from Seed
// (Shader Graph Random Range + Floor), then atlas UV = origin + saturate(customUv * cellSize).

int L2Fx_FlipbookCellCount(int uSubdivisions, int vSubdivisions)
{
    uSubdivisions = max(uSubdivisions, 1);
    vSubdivisions = max(vSubdivisions, 1);
    return uSubdivisions * vSubdivisions;
}

// uv01 = per-quad 0..1; cellIndex follows SubDivision.shadersubgraph:
// x = floor(index / rows), y = rows - 1 - (index % rows).
float2 L2Fx_FlipbookAtlasUV(float2 uv01, int cellIndex, int uSubdivisions, int vSubdivisions)
{
    int uSub = max(uSubdivisions, 1);
    int vSub = max(vSubdivisions, 1);
    int tiles = uSub * vSub;
    cellIndex = clamp(cellIndex, 0, tiles - 1);
    float du = 1.0 / (float)uSub;
    float dv = 1.0 / (float)vSub;
    int u = cellIndex / vSub;
    int v = (vSub - 1) - (cellIndex % vSub);
    float2 cellSize = float2(du, dv);
    float2 origin = float2((float)u * du, (float)v * dv);
    return origin + saturate(uv01 * cellSize);
}

// Discrete frame in [subStart, subEnd] vs normalized particle age 0..1 (matches UE span = end - start).
int L2Fx_FlipbookFrameIndex(float normalizedAge, int subStart, int subEnd)
{
    int span = max(subEnd - subStart, 1);
    float t = saturate(normalizedAge);
    int f = subStart + (int)floor(t * (float)span);
    return clamp(f, subStart, subEnd);
}

// Continuous frame index for BlendBetweenSubdivisions (lerp UV between floor and floor+1).
float L2Fx_FlipbookFrameFloat(float normalizedAge, int subStart, int subEnd)
{
    int span = max(subEnd - subStart, 1);
    float t = saturate(normalizedAge);
    return (float)subStart + t * (float)span;
}

// Two frame indices + lerp weight in [0,1) for sampling two atlas cells.
void L2Fx_FlipbookBlendFrames(
    float normalizedAge,
    int subStart,
    int subEnd,
    out int frameA,
    out int frameB,
    out float blend)
{
    float f = L2Fx_FlipbookFrameFloat(normalizedAge, subStart, subEnd);
    frameA = (int)floor(f);
    frameB = frameA + 1;
    frameA = clamp(frameA, subStart, subEnd);
    frameB = clamp(frameB, subStart, subEnd);
    blend = saturate(f - (float)frameA);
}

// Full atlas UV pair for BlendBetweenSubdivisions + subdiv grid (Unreal TextureU/VSubdivisions).
void L2Fx_FlipbookAtlasUVBlend(
    float2 uv01,
    float normalizedAge,
    int uSubdivisions,
    int vSubdivisions,
    int subStart,
    int subEnd,
    out float2 uvA,
    out float2 uvB,
    out float blend)
{
    int fa, fb;
    L2Fx_FlipbookBlendFrames(normalizedAge, subStart, subEnd, fa, fb, blend);
    uvA = L2Fx_FlipbookAtlasUV(uv01, fa, uSubdivisions, vSubdivisions);
    uvB = L2Fx_FlipbookAtlasUV(uv01, fb, uSubdivisions, vSubdivisions);
}

// --- Gawric SubDivision-style (random cell, not time-based) ---

// Matches intent of SubDivision: RandomRange(Seed, FrameStart, FrameEnd) then Floor -> int cell index.
int L2Fx_FlipbookSubDivisionRandomFrame(float seed, float startTime, int frameStart, int frameEnd, float salt)
{
    float mn = (float)min(frameStart, frameEnd);
    float mx = (float)max(frameStart, frameEnd);
    float r = L2Fx_RandomRange(float2(mn, mx), seed, startTime, salt);
    int idx = (int)floor(r);
    return clamp(idx, min(frameStart, frameEnd), max(frameStart, frameEnd));
}

// Atlas UV for one quad: same SubDivision mapping as L2Fx_FlipbookAtlasUV.
float2 L2Fx_FlipbookSubDivisionAtlasUV(
    float2 customUv01,
    int cellIndex,
    int columns,
    int rows)
{
    return L2Fx_FlipbookAtlasUV(customUv01, cellIndex, columns, rows);
}

// One-call equivalent to SubDivision outputs used for sampling: random index + UV.
float2 L2Fx_FlipbookSubDivisionUV_Random(
    float2 customUv01,
    float seed,
    float startTime,
    int columns,
    int rows,
    int frameStart,
    int frameEnd,
    float salt)
{
    int idx = L2Fx_FlipbookSubDivisionRandomFrame(seed, startTime, frameStart, frameEnd, salt);
    return L2Fx_FlipbookSubDivisionAtlasUV(customUv01, idx, columns, rows);
}

#endif
