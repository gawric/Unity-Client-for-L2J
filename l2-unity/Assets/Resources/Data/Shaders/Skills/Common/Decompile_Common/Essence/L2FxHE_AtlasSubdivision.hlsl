#ifndef L2_FX_HE_ATLAS_SUBDIVISION_INCLUDED
#define L2_FX_HE_ATLAS_SUBDIVISION_INCLUDED

// HE SpawnParticle loc_208EB70C atlas cell:
//   TextureU @+0x350  TextureV @+0x354
//   subdivFlags @+0x358  bit1 UseRandomSubdivision
//   SubdivisionStart @+0x368  End @+0x36C
//   store cell at slot+0xCC  (-1 if not random)
//
// Atlas UV / timed BlendBetween in ../../L2FxFlipbook.hlsl (do not duplicate).
// Random cell HE SpawnParticle:
//   if UseRandomSubdivision:
//     if End != 0: cell = trunc(appFrand() * (End - Start) + Start)  // End exclusive
//     else:        cell = trunc(appFrand() * U * V)
//   else:
//     slot+0xCC = -1  (timed / BlendBetween over life)
//
// 1147:
//   d_mon_fire2_ca Aura     4x4 Blend End=16
//   d_mon_fire2_ca Sprite   1x1 Random End=3 (clamps to one cell)
//   d_mon_fire2_ca smorke   2x2 Random End=3
//   u_mon_fire1_fl Core     4x4 Random Start=8 End=10
//   u_mon_fire1_fl Flame    2x2 Random End=2
//   u_mon_fire1_fl CoreRound 4x4 Blend End=16
//   d_mon_fire_ta  center   Blend (default 1x1)
//   d_mon_fire_ta  Cb       2x4 Blend End=3

#include "../L2FxAppRand.hlsl"
#include "../../L2FxFlipbook.hlsl"

static const uint L2FX_HE_SUBDIV_BLEND_BIT = 1u;
static const uint L2FX_HE_SUBDIV_RANDOM_BIT = 2u;

static const int L2FX_HE_ATLAS_MODE_STATIC = 0;
static const int L2FX_HE_ATLAS_MODE_TIMED = 1;
static const int L2FX_HE_ATLAS_MODE_RANDOM = 2;
static const int L2FX_HE_ATLAS_MODE_BLEND = 3;

int L2FxHE_Atlas_ModeFromFlags(uint subdivFlags, int subStart, int subEnd)
{
    if ((subdivFlags & L2FX_HE_SUBDIV_BLEND_BIT) != 0u)
    {
        return L2FX_HE_ATLAS_MODE_BLEND;
    }
    if ((subdivFlags & L2FX_HE_SUBDIV_RANDOM_BIT) != 0u)
    {
        return L2FX_HE_ATLAS_MODE_RANDOM;
    }
    if (subEnd > subStart)
    {
        return L2FX_HE_ATLAS_MODE_TIMED;
    }
    return L2FX_HE_ATLAS_MODE_STATIC;
}

// SpawnParticle: trunc(appFrand() * (End - Start) + Start). End is exclusive.
int L2FxHE_Atlas_RandomFrame(int subStart, int subEnd, inout uint appRandState)
{
    int span = subEnd - subStart;
    if (span <= 0)
    {
        return subStart;
    }

    float t = L2Fx_AppFrand(appRandState);
    return (int)trunc(t * (float)span + (float)subStart);
}

int L2FxHE_Atlas_RandomFrameOrGrid(
    int subStart,
    int subEnd,
    int uSubdivisions,
    int vSubdivisions,
    inout uint appRandState)
{
    if (subEnd == 0)
    {
        int tiles = L2Fx_FlipbookCellCount(uSubdivisions, vSubdivisions);
        float t = L2Fx_AppFrand(appRandState);
        return (int)trunc(t * (float)tiles);
    }

    return L2FxHE_Atlas_RandomFrame(subStart, subEnd, appRandState);
}

int L2FxHE_Atlas_ClampCell(int cellIndex, int uSubdivisions, int vSubdivisions)
{
    int tiles = L2Fx_FlipbookCellCount(uSubdivisions, vSubdivisions);
    return clamp(cellIndex, 0, tiles - 1);
}

float2 L2FxHE_Atlas_UV_Static(
    float2 uv01,
    int cellIndex,
    int uSubdivisions,
    int vSubdivisions)
{
    return L2Fx_FlipbookAtlasUV(
        uv01,
        L2FxHE_Atlas_ClampCell(cellIndex, uSubdivisions, vSubdivisions),
        uSubdivisions,
        vSubdivisions);
}

float2 L2FxHE_Atlas_UV_Timed(
    float2 uv01,
    float normalizedAge,
    int uSubdivisions,
    int vSubdivisions,
    int subStart,
    int subEnd)
{
    int cell = L2Fx_FlipbookFrameIndex(normalizedAge, subStart, subEnd);
    return L2FxHE_Atlas_UV_Static(uv01, cell, uSubdivisions, vSubdivisions);
}

float2 L2FxHE_Atlas_UV_Random(
    float2 uv01,
    int uSubdivisions,
    int vSubdivisions,
    int subStart,
    int subEnd,
    inout uint appRandState)
{
    int cell = L2FxHE_Atlas_RandomFrameOrGrid(
        subStart,
        subEnd,
        uSubdivisions,
        vSubdivisions,
        appRandState);
    return L2FxHE_Atlas_UV_Static(uv01, cell, uSubdivisions, vSubdivisions);
}

void L2FxHE_Atlas_UV_Blend(
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
    L2Fx_FlipbookAtlasUVBlend(
        uv01,
        normalizedAge,
        uSubdivisions,
        vSubdivisions,
        subStart,
        subEnd,
        uvA,
        uvB,
        blend);
}

#endif
