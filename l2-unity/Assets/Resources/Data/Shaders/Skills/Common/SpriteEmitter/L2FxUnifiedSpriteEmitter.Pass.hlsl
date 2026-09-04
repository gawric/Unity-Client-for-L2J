#ifndef L2_FX_UNIFIED_SPRITE_EMITTER_PASS_INCLUDED
#define L2_FX_UNIFIED_SPRITE_EMITTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../L2FxEmitterSpawn.hlsl"
#include "../L2FxCoreGeometryTest.hlsl"
#include "../Decompile_Common/L2FxAppRand.hlsl"
#include "../Decompile_Common/L2FxSpriteSpawnParticle.hlsl"
#include "../Decompile_Common/L2FxSpritePolar.hlsl"
#include "../Decompile_Common/L2FxStartLocationRange.hlsl"
#include "../Decompile_Common/L2FxPTVD_OwnerAndStartPosition.hlsl"
#include "../Decompile_Common/L2FxPTVD_StartPositionAndOwner.hlsl"
#include "../Decompile_Common/L2FxSpriteMotion.hlsl"
#include "../Decompile_Common/L2FxSpriteSpin.hlsl"
#include "../Decompile_Common/L2FxPTDU_Up.hlsl"
#include "../Decompile_Common/L2FxPTDU_Normal.hlsl"
#include "../Decompile_Common/L2FxSpriteSizeScale.hlsl"
#include "../Decompile_Common/L2FxSpriteColorFade.hlsl"
#include "../Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
#include "../Decompile_Common/L2FxPTDS_DrawStyle.hlsl"
#include "../Decompile_Common/L2FxD3d9FixedFunction.hlsl"
#include "../Decompile_Common/Essence/L2FxHE_VectorScale.hlsl"
#include "../Decompile_Common/Essence/L2FxHE_CoordinateSystem.hlsl"
#include "../Decompile_Common/Essence/L2FxHE_Revolution.hlsl"
#include "../Decompile_Common/Essence/L2FxHE_PTDU_Forward.hlsl"
#include "../Decompile_Common/Essence/L2FxHE_LocationShape.hlsl"
#include "../L2FxSpriteEmitterVertex.hlsl"
#include "../L2FxFlipbook.hlsl"
#include "../L2FxMeshFragment.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

// Spawn: 0=None, 1=Box, 2=Polar, 3=Full TLS stream.
// Full TLS shape: 0=Shape0/box, 1=Polar.
// HE shape: 0=box, 1=sphere, 2=polar. Atlas stays L2FxFlipbook.
// Motion: 0=None, 1=Ballistic, 2=Drag.
// Orientation: 0=Camera billboard, 1=PTDU_Up, 2=PTDU_Normal, 3=PTDU_Forward.
// PTVD: 0=None, 1=StartPositionAndOwner, 2=OwnerAndStartPosition.
// Size: 0=Uniform, 1=XYZ. Spin: 0=None, 1=appRand.
// Flipbook: 0=Static, 1=Timed, 2=Random, 3=BlendBetween.
CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float _StartTime;
    float _Seed;
    float _SpriteMotionRandStateBits;
    float _SpriteSpinRandStateBits;
    float4 _OwnerWorldPos;
    float4 _LifetimeRange;
    float4 _InitialDelayRange;
    float _UseManualAge;
    float _ManualAge;

    float _SpawnMode;
    float _FullTlsShape;
    float _HeLocationShape;
    float _MotionMode;
    float _OrientationMode;
    float4 _SurfaceNormals;
    float _PtvdMode;
    float _SizeMode;
    float _SpinMode;
    float _FlipbookMode;

    float _L2FxWorldCalibration;
    float4 _StartLocationOffsetUc;
    float4 _StartLocationRangeUU;
    float4 _StartLocationRangeXUc;
    float4 _StartLocationRangeYUc;
    float4 _StartLocationRangeZUc;
    float4 _PolarThetaRangeUc;
    float4 _PolarPhiRangeUc;
    float4 _PolarRadiusRangeUc;
    float4 _SphereRadiusRangeUc;
    float _UseRevolution;
    float4 _RevolutionCenterOffsetRangeXUc;
    float4 _RevolutionCenterOffsetRangeYUc;
    float4 _RevolutionCenterOffsetRangeZUc;
    float4 _RevolutionsPerSecondRangeXUc;
    float4 _RevolutionsPerSecondRangeYUc;
    float4 _RevolutionsPerSecondRangeZUc;
    float _UseRevolutionScale;
    float _RevolutionScaleRepeats;
    float _RevolutionScaleCount;
    float4 _RevolutionScaleKey0;
    float4 _RevolutionScaleKey1;
    float4 _RevolutionScaleKey2;
    float4 _RevolutionScaleKey3;
    float4 _RevolutionScaleKey4;
    float4 _RevolutionScaleKey5;
    float4 _RevolutionScaleKey6;
    float4 _StartVelocityRangeXUc;
    float4 _StartVelocityRangeYUc;
    float4 _StartVelocityRangeZUc;
    float4 _StartVelocityRadialRangeUc;
    float4 _AccelerationUc;
    float4 _VelocityLossRangeUc;
    float _CoordinateSystem;
    float _IndependentSprayAccel;
    float4 _MaxAbsVelocityUc;
    float _UseVelocityScale;
    float _VelocityScaleRepeats;
    float _VelocityScaleCount;
    float4 _VelocityScaleKey0;
    float4 _VelocityScaleKey1;
    float4 _VelocityScaleKey2;
    float4 _VelocityScaleKey3;
    float4 _VelocityScaleKey4;
    float4 _VelocityScaleKey5;
    float4 _VelocityScaleKey6;
    float _SpawnDeltaTime;

    float4 _SizeRange;
    float4 _SizeRangeXUc;
    float4 _SizeRangeYUc;
    float4 _SizeRangeZUc;
    float _UseSizeScale;
    float _SizeScaleRepeats;
    float _SizeScaleCount;
    float4 _SizeKey0;
    float4 _SizeKey1;
    float4 _SizeKey2;
    float4 _SizeKey3;
    float4 _SizeKey4;

    float4 _SpriteSpinStartRangeUc;
    float4 _SpriteSpinSpsRangeUc;
    float4 _SpriteSpinCcwOrCw;

    float _ColorScaleCount;
    float _ColorScaleParam;
    float4 _ColorKey0;
    float _ColorKey1Time;
    float4 _ColorKey1;
    float _ColorKey2Time;
    float4 _ColorKey2;
    float _ColorKey3Time;
    float4 _ColorKey3;
    float4 _ColorMulMin;
    float4 _ColorMulMax;
    float _ColorFadeAlphaBlend;
    float _FadeIn;
    float _FadeInEndTime;
    float _Fadeout;
    float _FadeoutStartTime;
    float _Opacity;
    float _RgbBoost;
    float _L2SpriteColorGammaToLinear;

    float _TextureUSubdivisions;
    float _TextureVSubdivisions;
    float _SubdivisionStart;
    float _SubdivisionEnd;
    float _StaticSubdivision;

    float _IgnoreMainTexAlpha;
    float _AlphaFromLuma;
    float _LumaAlphaFloor;
    float _UseSoftLumaAlpha;
    float _LumaAlphaPower;
    float _AlphaClipThreshold;
    float _DebugSpriteOut;
CBUFFER_END

#include "../L2FxInstancing.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uvA : TEXCOORD0;
    float2 uvB : TEXCOORD1;
    nointerpolation float blend : TEXCOORD2;
    nointerpolation float ageSeconds : TEXCOORD3;
    nointerpolation float lifetime : TEXCOORD4;
    nointerpolation float4 colorMul : TEXCOORD5;
    nointerpolation float4 debugData : TEXCOORD6;
};

struct L2FxUnifiedSpriteSpawn
{
    float3 positionUe;
    float3 velocityUe;
    float3 sizeUu;
    float3 colorMul;
    float lifetime;
    float delay;
};

void L2FxUnifiedSprite_ResolveSimpleSpawn(
    uint state,
    float mode,
    out L2FxUnifiedSpriteSpawn spawn)
{
    spawn.velocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        _StartVelocityRangeXUc.xy,
        _StartVelocityRangeYUc.xy,
        _StartVelocityRangeZUc.xy,
        state);
    spawn.positionUe = _StartLocationOffsetUc.xyz;
    if (mode > 1.5)
    {
        spawn.positionUe += L2Fx_SpritePolar_GetRandUe(
            _PolarThetaRangeUc.xy,
            _PolarPhiRangeUc.xy,
            _PolarRadiusRangeUc.xy,
            state);
    }
    else
    {
        spawn.positionUe = L2Fx_StartLocationRange_ApplyUe(
            spawn.positionUe,
            _StartLocationRangeXUc.xy,
            _StartLocationRangeYUc.xy,
            _StartLocationRangeZUc.xy,
            state);
    }
    spawn.lifetime = max(L2Fx_FRange_GetRand(_LifetimeRange.xy, state), 1e-4);
    float uniformSize = L2Fx_FRange_GetRand(_SizeRange.xy, state);
    spawn.sizeUu = float3(uniformSize, uniformSize, uniformSize);
    spawn.colorMul = _ColorMulMin.xyz;
    spawn.delay = L2Fx_RandomInitialDelay(
        _InitialDelayRange.xy, _SpriteMotionRandStateBits, _StartTime, 3.0);
}

void L2FxUnifiedSprite_ResolveFullTls(
    uint state,
    out L2FxUnifiedSpriteSpawn spawn)
{
    if (_FullTlsShape < 0.5)
    {
        float radial;
        L2Fx_SpriteSpawnParticle_SampleShape0ThroughSize(
            _StartVelocityRangeXUc.xy,
            _StartVelocityRangeYUc.xy,
            _StartVelocityRangeZUc.xy,
            _StartLocationRangeXUc.xy,
            _StartLocationRangeYUc.xy,
            _StartLocationRangeZUc.xy,
            float2(_ColorMulMin.x, _ColorMulMax.x),
            float2(_ColorMulMin.y, _ColorMulMax.y),
            float2(_ColorMulMin.z, _ColorMulMax.z),
            _LifetimeRange.xy,
            _InitialDelayRange.xy,
            _StartVelocityRadialRangeUc.xy,
            _SizeMode > 0.5 ? _SizeRangeXUc.xy : _SizeRange.xy,
            _SizeMode > 0.5 ? _SizeRangeYUc.xy : _SizeRange.xy,
            _SizeMode > 0.5 ? _SizeRangeZUc.xy : _SizeRange.xy,
            state,
            spawn.velocityUe,
            spawn.positionUe,
            spawn.colorMul,
            spawn.lifetime,
            spawn.delay,
            radial,
            spawn.sizeUu);
        spawn.positionUe += _StartLocationOffsetUc.xyz;
        spawn.lifetime = max(spawn.lifetime, 1e-4);
        return;
    }

    spawn.velocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        _StartVelocityRangeXUc.xy,
        _StartVelocityRangeYUc.xy,
        _StartVelocityRangeZUc.xy,
        state);
    spawn.positionUe = _StartLocationOffsetUc.xyz
        + L2Fx_SpritePolar_GetRandUe(
            _PolarThetaRangeUc.xy,
            _PolarPhiRangeUc.xy,
            _PolarRadiusRangeUc.xy,
            state);
    [unroll] for (int rngSkip = 0; rngSkip < 7; ++rngSkip)
        L2Fx_AppRand(state);
    spawn.colorMul = L2Fx_FRangeVector_GetRandYawPitchRoll(
        float2(_ColorMulMin.x, _ColorMulMax.x),
        float2(_ColorMulMin.y, _ColorMulMax.y),
        float2(_ColorMulMin.z, _ColorMulMax.z),
        state);
    spawn.lifetime = max(L2Fx_FRange_GetRand(_LifetimeRange.xy, state), 1e-4);
    spawn.delay = L2Fx_FRange_GetRand(_InitialDelayRange.xy, state);
    L2Fx_FRange_GetRand(_StartVelocityRadialRangeUc.xy, state);
    spawn.sizeUu = L2Fx_FRangeVector_GetRandYawPitchRoll(
        _SizeMode > 0.5 ? _SizeRangeXUc.xy : _SizeRange.xy,
        _SizeMode > 0.5 ? _SizeRangeYUc.xy : _SizeRange.xy,
        _SizeMode > 0.5 ? _SizeRangeZUc.xy : _SizeRange.xy,
        state);
}

void L2FxUnifiedSprite_ApplySphereIfNeeded(inout L2FxUnifiedSpriteSpawn spawn)
{
    if (_HeLocationShape < 0.5 || _HeLocationShape > 1.5)
    {
        return;
    }

    float radius = L2Fx_RandomRange(_SphereRadiusRangeUc.xy, _Seed, _StartTime, 211.0);
    float u = L2Fx_RandomRange(float2(0.0, 1.0), _Seed, _StartTime, 212.0);
    float v = L2Fx_RandomRange(float2(0.0, 1.0), _Seed, _StartTime, 213.0);
    float theta = 6.28318530718 * u;
    float z = 1.0 - 2.0 * v;
    float ring = sqrt(max(0.0, 1.0 - z * z));
    spawn.positionUe = _StartLocationOffsetUc.xyz
        + float3(ring * cos(theta), ring * sin(theta), z) * radius;
}

L2FxUnifiedSpriteSpawn L2FxUnifiedSprite_ResolveSpawn()
{
    L2FxUnifiedSpriteSpawn spawn;
    uint state = asuint(_SpriteMotionRandStateBits);
    if (_SpawnMode > 2.5)
    {
        L2FxUnifiedSprite_ResolveFullTls(state, spawn);
        L2FxUnifiedSprite_ApplySphereIfNeeded(spawn);
        return spawn;
    }
    if (_SpawnMode > 0.5)
    {
        L2FxUnifiedSprite_ResolveSimpleSpawn(state, _SpawnMode, spawn);
        L2FxUnifiedSprite_ApplySphereIfNeeded(spawn);
        return spawn;
    }

    spawn.positionUe = _StartLocationOffsetUc.xyz;
    spawn.velocityUe = float3(0.0, 0.0, 0.0);
    float uniformSize = L2Fx_RandomRange(
        _SizeRange.xy, _Seed, _StartTime, 11.0);
    spawn.sizeUu = float3(uniformSize, uniformSize, uniformSize);
    spawn.colorMul = _ColorMulMin.xyz;
    spawn.lifetime = max(L2Fx_RandomLifetime(
        _LifetimeRange.xy, _Seed, _StartTime, 7.0), 1e-4);
    spawn.delay = L2Fx_RandomInitialDelay(
        _InitialDelayRange.xy, _Seed, _StartTime, 3.0);
    L2FxUnifiedSprite_ApplySphereIfNeeded(spawn);
    return spawn;
}

void L2FxUnifiedSprite_SampleRevolution(out float3 centerUe, out float3 rps)
{
    uint revState = asuint(_SpriteMotionRandStateBits) ^ 0xA5A5u;
    centerUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        _RevolutionCenterOffsetRangeXUc.xy,
        _RevolutionCenterOffsetRangeYUc.xy,
        _RevolutionCenterOffsetRangeZUc.xy,
        revState);
    rps = L2Fx_FRangeVector_GetRandYawPitchRoll(
        _RevolutionsPerSecondRangeXUc.xy,
        _RevolutionsPerSecondRangeYUc.xy,
        _RevolutionsPerSecondRangeZUc.xy,
        revState);
}

float3 L2FxUnifiedSprite_ApplyRevolution(
    float3 locationUe,
    float ageSeconds,
    float lifetimeSeconds)
{
    float3 centerUe;
    float3 rps;
    L2FxUnifiedSprite_SampleRevolution(centerUe, rps);
    float times[7];
    float3 values[7];
    L2FxHE_VectorScale_BuildKeys7(
        _RevolutionScaleKey0,
        _RevolutionScaleKey1,
        _RevolutionScaleKey2,
        _RevolutionScaleKey3,
        _RevolutionScaleKey4,
        _RevolutionScaleKey5,
        _RevolutionScaleKey6,
        times,
        values);
    float3 integratedMultiplierSeconds =
        L2FxHE_VectorScale_IntegrateMultiplierMidpoint32(
            ageSeconds,
            lifetimeSeconds,
            _UseRevolutionScale,
            _RevolutionScaleRepeats,
            (uint)clamp(_RevolutionScaleCount, 0.0, 7.0),
            times,
            values);
    return L2FxHE_Revolution_ApplyIntegratedMultiplierUe(
        _UseRevolution,
        locationUe,
        centerUe,
        rps,
        integratedMultiplierSeconds);
}

float L2FxUnifiedSprite_SizeScale(float ageNorm)
{
    return L2Fx_SpriteSizeScale_ScalarFromUniforms(
        ageNorm,
        _UseSizeScale,
        _SizeScaleRepeats,
        (uint)clamp(_SizeScaleCount, 1.0, 5.0),
        false,
        false,
        _SizeKey0.x, _SizeKey0.y,
        _SizeKey1.x, _SizeKey1.y,
        _SizeKey2.x, _SizeKey2.y,
        _SizeKey3.x, _SizeKey3.y,
        _SizeKey4.x, _SizeKey4.y);
}

float3 L2FxUnifiedSprite_ApplyPtvd(float3 velocityUe, float3 positionUe)
{
    if (_PtvdMode > 1.5)
        return L2FxPTVD_OwnerAndStartPosition(
            velocityUe, positionUe, float3(0.0, 0.0, 0.0));
    if (_PtvdMode > 0.5)
        return L2FxPTVD_StartPositionAndOwner(
            velocityUe, positionUe, float3(0.0, 0.0, 0.0));
    return velocityUe;
}

float3 L2FxUnifiedSprite_Displacement(
    float3 velocityUe,
    float age,
    float lifetime)
{
    float3 accelerationUe;
    float3 velocityLossUe;
    L2FxHE_IndependentSprayAccel_Resolve(
        _CoordinateSystem,
        _IndependentSprayAccel,
        _AccelerationUc.xyz,
        _VelocityLossRangeUc.xyz,
        accelerationUe,
        velocityLossUe);
    bool maxAbsClamps = L2FxHE_MaxAbsWouldClamp(
        velocityUe,
        accelerationUe,
        velocityLossUe,
        _MaxAbsVelocityUc.xyz,
        age,
        _MotionMode);
    if (_UseVelocityScale > 0.5 || maxAbsClamps)
    {
        float times[7];
        float3 values[7];
        L2FxHE_VectorScale_BuildKeys7(
            _VelocityScaleKey0,
            _VelocityScaleKey1,
            _VelocityScaleKey2,
            _VelocityScaleKey3,
            _VelocityScaleKey4,
            _VelocityScaleKey5,
            _VelocityScaleKey6,
            times,
            values);
        return L2FxHE_VectorScale_IntegrateVelocityMidpoint16(
            velocityUe,
            accelerationUe,
            velocityLossUe,
            _MaxAbsVelocityUc.xyz,
            age,
            lifetime,
            _MotionMode,
            _UseVelocityScale,
            _VelocityScaleRepeats,
            (uint)clamp(_VelocityScaleCount, 0.0, 7.0),
            times,
            values);
    }

    if (_MotionMode > 1.5)
        return L2Fx_SpriteMotion_DisplacementUeWithDrag(
            velocityUe, accelerationUe,
            velocityLossUe, age, 1.0);
    if (_MotionMode > 0.5)
        return L2Fx_SpriteMotion_DisplacementUe(
            velocityUe, accelerationUe, age, 1.0);
    return float3(0.0, 0.0, 0.0);
}

void L2FxUnifiedSprite_ResolveFlipbook(
    float2 uv,
    float ageNorm,
    out float2 uvA,
    out float2 uvB,
    out float blend)
{
    int uSub = max(1, (int)_TextureUSubdivisions);
    int vSub = max(1, (int)_TextureVSubdivisions);
    int s0 = (int)_SubdivisionStart;
    int s1 = (int)_SubdivisionEnd;
    float2 baseUv = TRANSFORM_TEX(uv, _MainTex);
    uvA = baseUv;
    uvB = uvA;
    blend = 0.0;

    if (_FlipbookMode > 2.5)
    {
        L2Fx_FlipbookAtlasUVBlend(
            baseUv, ageNorm, uSub, vSub, s0, s1, uvA, uvB, blend);
    }
    else if (_FlipbookMode > 1.5)
    {
        int frame = L2Fx_FlipbookSubDivisionRandomFrame(
            _SpriteMotionRandStateBits, _StartTime, s0, s1, 19.0);
        uvA = L2Fx_FlipbookAtlasUV(baseUv, frame, uSub, vSub);
        uvB = uvA;
    }
    else if (_FlipbookMode > 0.5)
    {
        int frame = L2Fx_FlipbookFrameIndex(ageNorm, s0, s1);
        uvA = L2Fx_FlipbookAtlasUV(baseUv, frame, uSub, vSub);
        uvB = uvA;
    }
    else if (uSub * vSub > 1)
    {
        int frame = clamp((int)_StaticSubdivision, 0, uSub * vSub - 1);
        uvA = L2Fx_FlipbookAtlasUV(baseUv, frame, uSub, vSub);
        uvB = uvA;
    }
}

Varyings vert(Attributes IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    Varyings OUT;
    L2FxUnifiedSpriteSpawn spawn = L2FxUnifiedSprite_ResolveSpawn();
    spawn.positionUe = L2Fx_ApplySpawnLocationAddUe(spawn.positionUe);

    float ageSeconds = _UseManualAge > 0.5
        ? _ManualAge
        : max(0.0, _Time.y - _StartTime - spawn.delay);
    float ageNorm = saturate(ageSeconds / spawn.lifetime);
    float sizeMul = L2FxUnifiedSprite_SizeScale(ageNorm);
    float3 velocityUe = L2FxUnifiedSprite_ApplyPtvd(
        spawn.velocityUe + _AccelerationUc.xyz * _SpawnDeltaTime,
        spawn.positionUe);
    float3 displacementUe = L2FxUnifiedSprite_Displacement(
        velocityUe, ageSeconds, spawn.lifetime);
    float prevAge = max(0.0, ageSeconds - max(_SpawnDeltaTime, 1e-4));
    float3 previousDisplacementUe = L2FxUnifiedSprite_Displacement(
        velocityUe, prevAge, spawn.lifetime);
    float3 currentUe = L2FxUnifiedSprite_ApplyRevolution(
        spawn.positionUe + displacementUe, ageSeconds, spawn.lifetime);
    float3 previousUe = L2FxUnifiedSprite_ApplyRevolution(
        spawn.positionUe + previousDisplacementUe, prevAge, spawn.lifetime);

    float startSpin = 0.0;
    float spinsPerSecond = 0.0;
    if (_SpinMode > 0.5)
    {
        L2Fx_SpriteSpin_SpawnSlotFloatsFromAppRandState(
            _SpriteSpinStartRangeUc.xy,
            _SpriteSpinSpsRangeUc.xy,
            _SpriteSpinCcwOrCw.xyz,
            asuint(_SpriteSpinRandStateBits),
            startSpin,
            spinsPerSecond);
    }
    float spinRadians = L2Fx_SpriteSpin_EvaluateRadians(
        startSpin, spinsPerSecond, ageSeconds);

    float worldK = _L2FxWorldCalibration;
    float sizeXM = L2Fx_GetFinalVertexSizeMeters(
        spawn.sizeUu.x * sizeMul, worldK);
    float sizeYM = L2Fx_GetFinalVertexSizeMeters(
        (_SizeMode > 0.5 ? spawn.sizeUu.y : spawn.sizeUu.x) * sizeMul,
        worldK);
    float3 currentOS = L2Fx_UcPositionToUnityMeters(currentUe, worldK);
    float3 previousOS = L2Fx_UcPositionToUnityMeters(previousUe, worldK);
    currentOS = L2Fx_ApplySpawnWorldPositionOs(currentOS);
    previousOS = L2Fx_ApplySpawnWorldPositionOs(previousOS);

    if (_OrientationMode > 2.5)
    {
        float3 cornerOS = L2FxHE_PTDU_Forward_PositionUnityFromQuadOs(
            currentOS, previousOS, sizeXM, sizeYM, IN.positionOS.xy);
        OUT.positionHCS = TransformObjectToHClip(cornerOS);
    }
    else if (_OrientationMode > 1.5)
    {
        float3 positionWS = L2FxPTDU_Normal_PositionWS(
            TransformObjectToWorld(currentOS),
            IN.positionOS.xy,
            sizeXM,
            sizeYM,
            _SurfaceNormals.xyz);
        OUT.positionHCS = TransformWorldToHClip(positionWS);
    }
    else if (_OrientationMode > 0.5)
    {
        float3 cameraOS = TransformWorldToObject(GetCameraPositionWS());
        float3 cornerOS = L2FxPTDU_Up_PositionUnityFromQuadOs(
            currentOS, previousOS, cameraOS,
            sizeXM, sizeYM, IN.positionOS.xy);
        OUT.positionHCS = TransformObjectToHClip(cornerOS);
    }
    else
    {
        float2 rotatedQuad = L2Fx_SpriteSpin_RotateBillboardOffset(
            IN.positionOS.xy * sizeXM, spinRadians);
        float3 positionWS = L2Fx_CameraBillboardPositionWS(
            TransformObjectToWorld(currentOS),
            float3(rotatedQuad, IN.positionOS.z * sizeXM),
            0.0, 0.0);
        OUT.positionHCS = TransformWorldToHClip(positionWS);
    }

    L2FxUnifiedSprite_ResolveFlipbook(
        IN.uv, ageNorm, OUT.uvA, OUT.uvB, OUT.blend);
    OUT.ageSeconds = ageSeconds;
    OUT.lifetime = spawn.lifetime;
    OUT.colorMul = float4(spawn.colorMul, 1.0);
    OUT.debugData = float4(currentOS, ageSeconds);
    return OUT;
}

half4 frag(Varyings IN) : SV_Target
{
    half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvA);
    half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvB);

    float4 colorFade = L2Fx_SpriteColorFade_FullKeys(
        (uint)_ColorScaleCount,
        _ColorScaleParam,
        _ColorFadeAlphaBlend,
        _ColorKey0,
        _ColorKey1Time, _ColorKey1,
        _ColorKey2Time, _ColorKey2,
        _ColorKey3Time, _ColorKey3,
        IN.colorMul.xyz,
        IN.colorMul.xyz,
        IN.ageSeconds,
        IN.lifetime,
        1.0,
        _FadeIn,
        _FadeInEndTime,
        _Fadeout,
        _FadeoutStartTime,
        _Opacity,
        1.0,
        _SpriteMotionRandStateBits,
        _StartTime);
    colorFade = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
        colorFade, _L2SpriteColorGammaToLinear);

    half4 color;
    if (_FlipbookMode > 2.5)
    {
        // D3D9 BlendBetweenSubdivisions: t0/t1 are the same atlas, UV A/B.
        // in_Color0.a is the frame blend (IN.blend), not particle opacity.
        half4 mixed = lerp(texA, texB, (half)IN.blend);
        float textureAlpha = L2Fx_MeshFrag_SampleTextureAlphaSoft(
            mixed,
            _AlphaFromLuma,
            _LumaAlphaFloor,
            _LumaAlphaPower,
            _UseSoftLumaAlpha,
            _IgnoreMainTexAlpha);
        if (_AlphaClipThreshold >= 0.0)
            clip(textureAlpha - _AlphaClipThreshold);
        color = L2Fx_D3d9_BlendDiffuseAlphaTwoTex(
            texA, texB, half4((half3)colorFade.rgb, (half)IN.blend));
        // Combiner a = texture. FadeIn/Out only — not Opacity 0.1, or steam
        // becomes invisible. New puffs ramp coverage instead of drawing black.
        color.a = L2Fx_D3d9_BlendBetweenParticleCoverage(
            (half)textureAlpha,
            colorFade.a,
            _Opacity,
            _ColorFadeAlphaBlend);
        color.rgb *= (half)_RgbBoost;
    }
    else
    {
        half4 tex = lerp(texA, texB, (half)IN.blend);
        float textureAlpha = L2Fx_MeshFrag_SampleTextureAlphaSoft(
            tex,
            _AlphaFromLuma,
            _LumaAlphaFloor,
            _LumaAlphaPower,
            _UseSoftLumaAlpha,
            _IgnoreMainTexAlpha);
        if (_AlphaClipThreshold >= 0.0)
            clip(textureAlpha - _AlphaClipThreshold);
        color = tex * (half4)colorFade;
        color.a = (half)(textureAlpha * colorFade.a);
        color.rgb *= (half)_RgbBoost;
    }

    // One+One ignores framebuffer alpha. Dark atlas RGB (and bilinear bleed from
    // the neighbor cell) still adds as a square. MightTaAuraRing bakes luma into rgb.
    if (_AlphaFromLuma > 0.5 && _ColorFadeAlphaBlend < 0.5)
    {
        color.rgb *= (half)saturate(color.a);
    }

    color.rgb += (half3)(IN.debugData.xyz * 1e-10);
    if (_DebugSpriteOut > 0.5)
    {
        half3 lumaW = half3(0.299h, 0.587h, 0.114h);
        half luma = dot(color.rgb, lumaW);
        if (_DebugSpriteOut < 1.5)
            color = half4(color.a, color.a, color.a, 1.0h);
        else if (_DebugSpriteOut < 2.5)
            color = half4(luma, luma, luma, 1.0h);
        else if (_DebugSpriteOut < 3.5)
            color = half4(color.rgb, 1.0h);
        else
        {
            // Red = would punch a hole: high coverage alpha, dark RGB.
            half hole = step(0.5h, color.a) * (1.0h - step(0.25h, luma));
            color = half4(hole, luma, color.a, 1.0h);
        }
    }

    return color;
}

#endif
