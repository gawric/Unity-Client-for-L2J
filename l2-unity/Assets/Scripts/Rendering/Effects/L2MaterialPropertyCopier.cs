using System;
using UnityEngine;

/// <summary>
/// GPU bridge: copies shared material params into runtime instances and writes dynamic shader uniforms.
/// </summary>
public static class L2MaterialPropertyCopier
{
    public const string OwnerWorldPosProperty = "_OwnerWorldPos";
    public const string UseExternalTargetPositionProperty = "_UseExternalTargetPosition";
    public const string UseOwnerFromShaderTargetProperty = "_UseOwnerFromShaderTarget";
    public const string L2FxTargetWorldPosProperty = "_L2FxTargetWorldPos";

    public static readonly int HoldId = Shader.PropertyToID("_Hold");
    public static readonly int HoldSizeReferenceId = Shader.PropertyToID("_HoldSizeReference");
    public static readonly int LifetimeRangeId = Shader.PropertyToID("_LifetimeRange");
    public static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    public static readonly int SeedId = Shader.PropertyToID("_Seed");
    public static readonly int HasLifetimeId = Shader.PropertyToID("_HasLifetime");
    public static readonly int InitialDelayRangeId = Shader.PropertyToID("_InitialDelayRange");
    public static readonly int LoopSizeScalePreviewId = Shader.PropertyToID("_LoopSizeScalePreview");
    public static readonly int FadeInId = Shader.PropertyToID("_FadeIn");
    public static readonly int FadeInEndTimeId = Shader.PropertyToID("_FadeInEndTime");
    public static readonly int FadeoutId = Shader.PropertyToID("_Fadeout");
    public static readonly int FadeoutStartTimeId = Shader.PropertyToID("_FadeoutStartTime");
    public static readonly int FadeOutPowerId = Shader.PropertyToID("_FadeOutPower");
    public static readonly int DebugAtlasPreviewId = Shader.PropertyToID("_DebugAtlasPreview");
    public static readonly int RgbBoostId = Shader.PropertyToID("_RgbBoost");
    public static readonly int AlphaBoostId = Shader.PropertyToID("_AlphaBoost");
    public static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    public static readonly int AtlasUvRemapId = Shader.PropertyToID("_AtlasUvRemap");
    public static readonly int AtlasUvMinMaxId = Shader.PropertyToID("_AtlasUvMinMax");
    public static readonly int IgnoreMainTexAlphaId = Shader.PropertyToID("_IgnoreMainTexAlpha");
    public static readonly int EmitterAlphaId = Shader.PropertyToID("_EmitterAlpha");
    public static readonly int AlphaEdgeFeatherId = Shader.PropertyToID("_AlphaEdgeFeather");
    public static readonly int SplitRibbonByLumId = Shader.PropertyToID("_SplitRibbonByLum");
    public static readonly int SoftLumMinId = Shader.PropertyToID("_SoftLumMin");
    public static readonly int SoftLumMaxId = Shader.PropertyToID("_SoftLumMax");
    public static readonly int LineLumMinId = Shader.PropertyToID("_LineLumMin");
    public static readonly int LineLumMaxId = Shader.PropertyToID("_LineLumMax");
    public static readonly int SoftOpacityMulId = Shader.PropertyToID("_SoftOpacityMul");
    public static readonly int LineOpacityMulId = Shader.PropertyToID("_LineOpacityMul");
    public static readonly int SoftRgbBoostId = Shader.PropertyToID("_SoftRgbBoost");
    public static readonly int LineRgbBoostId = Shader.PropertyToID("_LineRgbBoost");
    public static readonly int SplitByUvLayerId = Shader.PropertyToID("_SplitByUvLayer");
    public static readonly int UvLayerCenterId = Shader.PropertyToID("_UvLayerCenter");
    public static readonly int UvLayerDistMinId = Shader.PropertyToID("_UvLayerDistMin");
    public static readonly int UvLayerDistMaxId = Shader.PropertyToID("_UvLayerDistMax");
    public static readonly int OuterSoftOpacityMulId = Shader.PropertyToID("_OuterSoftOpacityMul");
    public static readonly int OuterLineOpacityMulId = Shader.PropertyToID("_OuterLineOpacityMul");
    public static readonly int OuterSoftRgbBoostId = Shader.PropertyToID("_OuterSoftRgbBoost");
    public static readonly int OuterLineRgbBoostId = Shader.PropertyToID("_OuterLineRgbBoost");
    public static readonly int ColorScaleRepeatsId = Shader.PropertyToID("_ColorScaleRepeats");
    public static readonly int ColorScaleCountId = Shader.PropertyToID("_ColorScaleCount");
    public static readonly int ColorScaleTime1Id = Shader.PropertyToID("_ColorScaleTime1");
    public static readonly int ColorScaleTime2Id = Shader.PropertyToID("_ColorScaleTime2");
    public static readonly int ColorScale0Id = Shader.PropertyToID("_ColorScale0");
    public static readonly int ColorScale1Id = Shader.PropertyToID("_ColorScale1");
    public static readonly int ColorScale2Id = Shader.PropertyToID("_ColorScale2");
    public static readonly int BAlphaBlendId = Shader.PropertyToID("_bAlphaBlend");
    public static readonly int BillboardToCameraId = Shader.PropertyToID("_BillboardToCamera");
    public static readonly int BillboardWorldUpId = Shader.PropertyToID("_BillboardWorldUp");
    public static readonly int BillboardEulerOffsetId = Shader.PropertyToID("_BillboardEulerOffset");
    public static readonly int UseExternalTargetPositionId = Shader.PropertyToID(UseExternalTargetPositionProperty);
    public static readonly int UseOwnerFromShaderTargetId = Shader.PropertyToID(UseOwnerFromShaderTargetProperty);
    public static readonly int L2FxTargetWorldPosId = Shader.PropertyToID(L2FxTargetWorldPosProperty);
    public static readonly int StartSpinRandStateBitsId = Shader.PropertyToID("_StartSpinRandStateBits");
    public static readonly int MeshSpawnRandStateBitsId = Shader.PropertyToID("_MeshSpawnRandStateBits");
    public static readonly int SpawnModeId = Shader.PropertyToID("_SpawnMode");
    public static readonly int SpinParticlesId = Shader.PropertyToID("_SpinParticles");
    public static readonly int StartSpinYawRangeUcId = Shader.PropertyToID("_StartSpinYawRangeUc");
    public static readonly int StartSpinPitchRangeUcId = Shader.PropertyToID("_StartSpinPitchRangeUc");
    public static readonly int StartSpinRollRangeUcId = Shader.PropertyToID("_StartSpinRollRangeUc");
    public static readonly int StartLocationZRangeUUId = Shader.PropertyToID("_StartLocationZRangeUU");
    public static readonly int StartVelocityZRangeUUId = Shader.PropertyToID("_StartVelocityZRangeUU");
    public static readonly int StartSizeZRangeId = Shader.PropertyToID("_StartSizeZRange");
    public static readonly int StartSizeXYId = Shader.PropertyToID("_StartSizeXY");
    public static readonly int ColorMultiplierId = Shader.PropertyToID("_ColorMultiplier");
    public static readonly int SpriteSpinRandStateBitsId = Shader.PropertyToID("_SpriteSpinRandStateBits");
    public static readonly int SpriteSpinStartRangeUcId = Shader.PropertyToID("_SpriteSpinStartRangeUc");
    public static readonly int SpriteSpinSpsRangeUcId = Shader.PropertyToID("_SpriteSpinSpsRangeUc");
    public static readonly int SpriteSpinCcwOrCwId = Shader.PropertyToID("_SpriteSpinCcwOrCw");
    public static readonly int SpriteMotionRandStateBitsId = Shader.PropertyToID("_SpriteMotionRandStateBits");
    public static readonly int SpriteSpinModeId = Shader.PropertyToID("_SpinMode");
    public static readonly int StartVelocityRangeXUcId = Shader.PropertyToID("_StartVelocityRangeXUc");
    public static readonly int StartVelocityRangeYUcId = Shader.PropertyToID("_StartVelocityRangeYUc");
    public static readonly int StartVelocityRangeZUcId = Shader.PropertyToID("_StartVelocityRangeZUc");
    public static readonly int StartLocationOffsetUeId = Shader.PropertyToID("_StartLocationOffsetUe");
    public static readonly int StartLocationRangeXUcId = Shader.PropertyToID("_StartLocationRangeXUc");
    public static readonly int StartLocationRangeYUcId = Shader.PropertyToID("_StartLocationRangeYUc");
    public static readonly int StartLocationRangeZUcId = Shader.PropertyToID("_StartLocationRangeZUc");
    public static readonly int StartSizeRangeXUcId = Shader.PropertyToID("_StartSizeRangeXUc");
    public static readonly int StartSizeRangeYUcId = Shader.PropertyToID("_StartSizeRangeYUc");
    public static readonly int StartSizeRangeZUcId = Shader.PropertyToID("_StartSizeRangeZUc");
    public static readonly int SpsYawRangeUcId = Shader.PropertyToID("_SpsYawRangeUc");
    public static readonly int SpsPitchRangeUcId = Shader.PropertyToID("_SpsPitchRangeUc");
    public static readonly int SpsRollRangeUcId = Shader.PropertyToID("_SpsRollRangeUc");
    public static readonly int SpinCCWorCWId = Shader.PropertyToID("_SpinCCWorCW");
    public static readonly int L2MotionReplayEnabledId = Shader.PropertyToID("_L2MotionReplayEnabled");
    public static readonly int SpawnDeltaTimeId = Shader.PropertyToID("_SpawnDeltaTime");

    private const uint AppRandMultiplier = 214013u;
    private const uint AppRandIncrement = 2531011u;
    // SpawnParticleSnapshot.log: m_u004_b / SpriteEmitter0, emitter 15475F00, slot 0.
    private const uint HealingPotionSe0ReplaySpawnState = 0x6FEC3FC2u;
    private const float HealingPotionSe0ReplaySpawnDeltaTime = 0.0111764f;
    // m_u004_b / MeshEmitter3: captured L2 SpawnParticle trace shows 31
    // appRand draws from slot 0's state before Roll to slot 1's state before Roll.
    // This is emitter-specific, not a general StartSpin rule.
    public const int MeshEmitter3SlotToSlotDrawCount = 31;
    // MeshEmitter SpawnParticle Loc/Vel/Size stream (L2FxMeshSpawnParticle).
    // LIVE VERIFIED: it_healing_potion_ta Name="Wave" (LocZ/VelZ path).
    // LIVE VERIFIED: shot_N_atk / e_u505 MeshEmitter225 "Spirit" (full XYZ),
    //   SpawnSoulShotSpiritCapture 2026-07-22 draws=28 scopes=12.
    // Base state is BEFORE StartVelocity. StartSpin = +22.
    public const int MeshSpawnSlotToSlotDrawCount = 31;
    public const int MeshSpawnDrawsBeforeStartSpin = 22;

    /// <summary>
    /// Opt-in: material exposes MeshSpawn TLS (_MeshSpawnRandStateBits).
    /// LIVE: Wave (LocZ/VelZ) + Spirit MeshEmitter225 (full XYZ).
    /// </summary>
    public static bool IsMeshSpawnParticleMaterial(Material mat)
    {
        if (mat == null || !mat.HasProperty(MeshSpawnRandStateBitsId))
            return false;

        // Unified MeshEmitter exposes the full property superset. The mode,
        // rather than property existence, opts a material into SpawnParticle.
        return !mat.HasProperty(SpawnModeId) || mat.GetFloat(SpawnModeId) > 0.5f;
    }

    public static bool IsMeshStartSpinMaterial(Material mat)
    {
        if (mat == null || !mat.HasProperty(StartSpinRandStateBitsId))
            return false;

        return !mat.HasProperty(SpinParticlesId) || mat.GetFloat(SpinParticlesId) > 0.5f;
    }

    public static bool IsSpriteSpawnMaterial(Material mat)
    {
        if (mat == null || !mat.HasProperty(SpriteMotionRandStateBitsId))
            return false;

        return !mat.HasProperty(SpawnModeId) || mat.GetFloat(SpawnModeId) > 0.5f;
    }

    public static bool IsSpriteSpinMaterial(Material mat)
    {
        if (mat == null || !mat.HasProperty(SpriteSpinRandStateBitsId))
            return false;

        return !mat.HasProperty(SpriteSpinModeId) || mat.GetFloat(SpriteSpinModeId) > 0.5f;
    }

    /// <summary>
    /// shot_N_atk MeshEmitter225 Spirit: MeshSpawn + anisotropic Size XYZ + OffsetUe X=-5.
    /// </summary>
    public static bool IsShotNAtkMeshEmitter225SpiritMaterial(Material mat)
    {
        if (mat == null ||
            !IsMeshSpawnParticleMaterial(mat) ||
            !mat.HasProperty(StartLocationOffsetUeId))
        {
            return false;
        }

        // Distinguish from ShockWave (Offset=0, short FadeOut).
        Vector4 offset = mat.GetVector(StartLocationOffsetUeId);
        return offset.x < -1f;
    }

    /// <summary>
    /// shot_N_atk MeshEmitter226 ShockWave: MeshSpawn + short life flash FadeOut~0.0375.
    /// </summary>
    public static bool IsShotNAtkMeshEmitter226ShockWaveMaterial(Material mat)
    {
        if (mat == null ||
            !IsMeshSpawnParticleMaterial(mat) ||
            !mat.HasProperty(StartLocationOffsetUeId))
        {
            return false;
        }

        if (IsShotNAtkMeshEmitter225SpiritMaterial(mat))
        {
            return false;
        }

        // UC FadeOutStartTime=0.0375; Spirit uses ~0.41.
        int fadeOutId = Shader.PropertyToID("_FadeOutStartTime");
        if (!mat.HasProperty(fadeOutId))
        {
            return false;
        }

        return mat.GetFloat(fadeOutId) < 0.1f;
    }

    /// <summary>
    /// MeshEmitter SpawnParticle TLS stream. <paramref name="baseState"/> is
    /// immediately before slot 0 StartVelocityRange GetRand.
    /// LIVE: Wave + Spirit (same 31-draw slot stride).
    /// </summary>
    public static void CopyMeshSpawnAppRandFromBaseState(
        Material runtimeMat,
        Material sharedMat,
        uint baseState,
        int slotIndex)
    {
        if (!IsMeshSpawnParticleMaterial(sharedMat) || runtimeMat == null)
        {
            return;
        }

        CopyVectorIfPresent(runtimeMat, sharedMat, StartLocationZRangeUUId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityZRangeUUId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSizeZRangeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, StartSizeXYId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartLocationOffsetUeId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartLocationRangeXUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartLocationRangeYUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartLocationRangeZUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityRangeXUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityRangeYUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityRangeZUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSizeRangeXUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSizeRangeYUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSizeRangeZUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSpinYawRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSpinPitchRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSpinRollRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpsYawRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpsPitchRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpsRollRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpinCCWorCWId);
        CopyVectorIfPresent(runtimeMat, sharedMat, ColorMultiplierId);
        CopyVectorIfPresent(runtimeMat, sharedMat, LifetimeRangeId);
        CopyVectorIfPresent(runtimeMat, sharedMat, InitialDelayRangeId);

        uint velocityState = AdvanceAppRandState(baseState, slotIndex * MeshSpawnSlotToSlotDrawCount);
        SetMeshSpawnRandState(runtimeMat, velocityState);
        SetStartSpinRandState(
            runtimeMat,
            AdvanceAppRandState(velocityState, MeshSpawnDrawsBeforeStartSpin));
    }

    public static void SetMeshSpawnRandState(Material mat, uint state)
    {
        if (mat != null && mat.HasProperty(MeshSpawnRandStateBitsId))
        {
            mat.SetFloat(
                MeshSpawnRandStateBitsId,
                BitConverter.Int32BitsToSingle(unchecked((int)state)));
        }
    }

    public static uint ReadMeshSpawnRandState(Material mat)
    {
        if (mat != null && mat.HasProperty(MeshSpawnRandStateBitsId))
        {
            float bits = mat.GetFloat(MeshSpawnRandStateBitsId);
            if (bits != 0f)
            {
                return unchecked((uint)BitConverter.SingleToInt32Bits(bits));
            }
        }

        return 0u;
    }

    /// <summary>
    /// CPU mirror of L2Fx_MeshSpawnParticle_SampleLocVelSizeZ for snapshot logs.
    /// </summary>
    public static void SampleMeshSpawnLocVelSizeZ(
        Material mat,
        uint stateBeforeVelocity,
        out float locationZ,
        out float velocityZ,
        out float sizeZ)
    {
        uint state = stateBeforeVelocity;
        Vector4 velRange = mat != null && mat.HasProperty(StartVelocityZRangeUUId)
            ? mat.GetVector(StartVelocityZRangeUUId)
            : new Vector4(-3.6f, 3.6f, 0f, 0f);
        Vector4 locRange = mat != null && mat.HasProperty(StartLocationZRangeUUId)
            ? mat.GetVector(StartLocationZRangeUUId)
            : new Vector4(-3.6f, 3.6f, 0f, 0f);
        Vector4 sizeZRange = mat != null && mat.HasProperty(StartSizeZRangeId)
            ? mat.GetVector(StartSizeZRangeId)
            : new Vector4(-0.0408f, 0.0408f, 0f, 0f);
        float sizeXY = mat != null && mat.HasProperty(StartSizeXYId)
            ? mat.GetFloat(StartSizeXYId)
            : 0.132f;
        Vector4 colorMul = mat != null && mat.HasProperty(ColorMultiplierId)
            ? mat.GetVector(ColorMultiplierId)
            : new Vector4(1f, 0.774f, 0.6f, 0f);
        Vector4 lifetimeRange = mat != null && mat.HasProperty(LifetimeRangeId)
            ? mat.GetVector(LifetimeRangeId)
            : new Vector4(1f, 1f, 0f, 0f);
        Vector4 delayRange = mat != null && mat.HasProperty(InitialDelayRangeId)
            ? mat.GetVector(InitialDelayRangeId)
            : Vector4.zero;

        velocityZ = AppRandFRangeVectorZ(0f, 0f, 0f, 0f, velRange.x, velRange.y, ref state);
        locationZ = AppRandFRangeVectorZ(0f, 0f, 0f, 0f, locRange.x, locRange.y, ref state);

        AppRandFRange(0f, 1f, ref state);
        for (int i = 0; i < 6; i++)
        {
            AppRandFrand(ref state);
        }

        AppRandFRangeVectorZ(
            colorMul.x, colorMul.x,
            colorMul.y, colorMul.y,
            colorMul.z, colorMul.z,
            ref state);
        AppRandFRange(lifetimeRange.x, lifetimeRange.y, ref state);
        AppRandFRange(delayRange.x, delayRange.y, ref state);
        AppRandFRange(1f, 1f, ref state);

        sizeZ = AppRandFRangeVectorZ(
            sizeXY, sizeXY,
            sizeXY, sizeXY,
            sizeZRange.x, sizeZRange.y,
            ref state);
    }

    /// <summary>
    /// CPU mirror of L2Fx_MeshSpawnParticle_SampleLocVelSize (Spirit / anisotropic mesh).
    /// Leaves state at StartSpin entry (draw 22).
    /// </summary>
    public static void SampleMeshSpawnLocVelSize(
        Material mat,
        uint stateBeforeVelocity,
        out Vector3 velocityUe,
        out Vector3 locationUe,
        out Vector3 colorMulRgb,
        out float lifetimeSeconds,
        out float initialDelaySeconds,
        out Vector3 sizeUe)
    {
        uint state = stateBeforeVelocity;
        Vector2 velX = ReadMinMax(mat, StartVelocityRangeXUcId, 10f, 10f);
        Vector2 velY = ReadMinMax(mat, StartVelocityRangeYUcId, -20f, 20f);
        Vector2 velZ = ReadMinMax(mat, StartVelocityRangeZUcId, -20f, 20f);
        Vector2 locX = ReadMinMax(mat, StartLocationRangeXUcId, 0f, 0f);
        Vector2 locY = ReadMinMax(mat, StartLocationRangeYUcId, 0f, 0f);
        Vector2 locZ = ReadMinMax(mat, StartLocationRangeZUcId, 0f, 0f);
        Vector4 colorMul = mat != null && mat.HasProperty(ColorMultiplierId)
            ? mat.GetVector(ColorMultiplierId)
            : new Vector4(0.6f, 0.6f, 0.6f, 0f);
        Vector2 life = ReadMinMax(mat, LifetimeRangeId, 1f, 1.5f);
        Vector2 delay = ReadMinMax(mat, InitialDelayRangeId, 0f, 0f);
        Vector2 sizeX = ReadMinMax(mat, StartSizeRangeXUcId, 0.015f, 0.015f);
        Vector2 sizeY = ReadMinMax(mat, StartSizeRangeYUcId, 0.1f, 0.1f);
        Vector2 sizeZ = ReadMinMax(mat, StartSizeRangeZUcId, 0.1f, 0.1f);

        velocityUe = AppRandFRangeVector(velX, velY, velZ, ref state);
        locationUe = AppRandFRangeVector(locX, locY, locZ, ref state);

        AppRandFRange(0f, 1f, ref state);
        for (int i = 0; i < 6; i++)
        {
            AppRandFrand(ref state);
        }

        colorMulRgb = AppRandFRangeVector(
            new Vector2(colorMul.x, colorMul.x),
            new Vector2(colorMul.y, colorMul.y),
            new Vector2(colorMul.z, colorMul.z),
            ref state);
        lifetimeSeconds = AppRandFRange(life.x, life.y, ref state);
        initialDelaySeconds = AppRandFRange(delay.x, delay.y, ref state);
        AppRandFRange(1f, 1f, ref state);
        sizeUe = AppRandFRangeVector(sizeX, sizeY, sizeZ, ref state);
    }

    private static Vector2 ReadMinMax(Material mat, int propertyId, float defaultMin, float defaultMax)
    {
        if (mat != null && mat.HasProperty(propertyId))
        {
            Vector4 v = mat.GetVector(propertyId);
            return new Vector2(v.x, v.y);
        }

        return new Vector2(defaultMin, defaultMax);
    }

    private static uint AppRandStep(ref uint state)
    {
        state = unchecked(state * AppRandMultiplier + AppRandIncrement);
        return (state >> 16) & 0x7fffu;
    }

    private static float AppRandFrand(ref uint state)
    {
        return AppRandStep(ref state) / 32767f;
    }

    private static float AppRandFRange(float min, float max, ref uint state)
    {
        return AppRandFrand(ref state) * (min - max) + max;
    }

    private static float AppRandFRangeVectorZ(
        float xMin, float xMax,
        float yMin, float yMax,
        float zMin, float zMax,
        ref uint state)
    {
        float z = AppRandFRange(zMin, zMax, ref state);
        AppRandFRange(yMin, yMax, ref state);
        AppRandFRange(xMin, xMax, ref state);
        return z;
    }

    // FRangeVector::GetRand draw order Z→Y→X; return (X,Y,Z)=(yaw,pitch,roll).
    private static Vector3 AppRandFRangeVector(
        Vector2 xRange,
        Vector2 yRange,
        Vector2 zRange,
        ref uint state)
    {
        float z = AppRandFRange(zRange.x, zRange.y, ref state);
        float y = AppRandFRange(yRange.x, yRange.y, ref state);
        float x = AppRandFRange(xRange.x, xRange.y, ref state);
        return new Vector3(x, y, z);
    }

    public static void CopyLifetimeFadeAndFxFromShared(Material runtimeMat, Material sharedMat)
    {
        if (runtimeMat == null || sharedMat == null)
        {
            return;
        }

        CopyFloatIfPresent(runtimeMat, sharedMat, HasLifetimeId);
        // CompositePrefabEffect may call EffectShaderLifetimeHelper.Apply(false) on the whole instance;
        // per-slot spawn must restore authored lifetime fade (upline SpriteEmitter2, etc.).
        RestoreShaderLifetimeFromSharedFadeAuthored(runtimeMat, sharedMat);
        CopyVectorIfPresent(runtimeMat, sharedMat, LifetimeRangeId);
        CopyVectorIfPresent(runtimeMat, sharedMat, InitialDelayRangeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LoopSizeScalePreviewId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeInId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeInEndTimeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeoutId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeoutStartTimeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeOutPowerId);
        if (Application.isPlaying && runtimeMat.HasProperty(DebugAtlasPreviewId))
        {
            runtimeMat.SetFloat(DebugAtlasPreviewId, 0f);
        }
        else
        {
            CopyFloatIfPresent(runtimeMat, sharedMat, DebugAtlasPreviewId);
        }
        CopyFloatIfPresent(runtimeMat, sharedMat, RgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, AlphaBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OpacityId);
        CopyFloatIfPresent(runtimeMat, sharedMat, AtlasUvRemapId);
        CopyVectorIfPresent(runtimeMat, sharedMat, AtlasUvMinMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, IgnoreMainTexAlphaId);
        CopyFloatIfPresent(runtimeMat, sharedMat, EmitterAlphaId);
        CopyFloatIfPresent(runtimeMat, sharedMat, AlphaEdgeFeatherId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SplitRibbonByLumId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftLumMinId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftLumMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineLumMinId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineLumMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SplitByUvLayerId);
        CopyVectorIfPresent(runtimeMat, sharedMat, UvLayerCenterId);
        CopyFloatIfPresent(runtimeMat, sharedMat, UvLayerDistMinId);
        CopyFloatIfPresent(runtimeMat, sharedMat, UvLayerDistMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterSoftOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterLineOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterSoftRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterLineRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleRepeatsId);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleCountId);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleTime1Id);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleTime2Id);
        CopyColorIfPresent(runtimeMat, sharedMat, ColorScale0Id);
        CopyColorIfPresent(runtimeMat, sharedMat, ColorScale1Id);
        CopyColorIfPresent(runtimeMat, sharedMat, ColorScale2Id);
        CopyFloatIfPresent(runtimeMat, sharedMat, BAlphaBlendId);
        CopyFloatIfPresent(runtimeMat, sharedMat, BillboardToCameraId);
        CopyVectorIfPresent(runtimeMat, sharedMat, BillboardWorldUpId);
        CopyVectorIfPresent(runtimeMat, sharedMat, BillboardEulerOffsetId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpriteSpinStartRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpriteSpinSpsRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, SpriteSpinCcwOrCwId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityRangeXUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityRangeYUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartVelocityRangeZUcId);
    }

    /// <summary>
    /// Copies one live appRand sequence into a m_u004_b / MeshEmitter3 slot.
    /// The supplied base state is generated once per effect play; the stored state
    /// is immediately before that slot's Roll/Z StartSpin draw.
    /// </summary>
    public static void CopyMeshAppRandStartSpinFromBaseState(
        Material runtimeMat,
        Material sharedMat,
        uint baseState,
        int slotIndex)
    {
        if (runtimeMat == null || sharedMat == null)
        {
            return;
        }

        bool hasMeshSpawn = IsMeshSpawnParticleMaterial(sharedMat);
        bool hasStartSpin = IsMeshStartSpinMaterial(sharedMat);
        if (!hasMeshSpawn && !hasStartSpin)
            return;

        if (hasMeshSpawn)
        {
            // Mesh spawn base is before StartVelocity; MeshEmitter3 base is before StartSpin.
            CopyMeshSpawnAppRandFromBaseState(runtimeMat, sharedMat, baseState, slotIndex);
            return;
        }

        CopyVectorIfPresent(runtimeMat, sharedMat, StartSpinYawRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSpinPitchRangeUcId);
        CopyVectorIfPresent(runtimeMat, sharedMat, StartSpinRollRangeUcId);

        SetStartSpinRandState(
            runtimeMat,
            ComputeMeshEmitter3StartSpinState(baseState, slotIndex));
    }

    public static uint ComputeMeshEmitter3StartSpinState(uint sharedBaseState, int slotIndex)
    {
        return AdvanceAppRandState(sharedBaseState, slotIndex * MeshEmitter3SlotToSlotDrawCount);
    }

    /// <summary>
    /// Mesh SpawnParticle: sharedBase is before StartVelocity. StartSpin is +22 draws
    /// within the slot (slot*31 + 22 from sharedBase). LIVE: Wave + Spirit.
    /// </summary>
    public static uint ComputeMeshSpawnStartSpinState(uint sharedBaseState, int slotIndex)
    {
        uint velocityState = AdvanceAppRandState(
            sharedBaseState,
            slotIndex * MeshSpawnSlotToSlotDrawCount);
        return AdvanceAppRandState(velocityState, MeshSpawnDrawsBeforeStartSpin);
    }

    public static uint ReadStartSpinRandState(Material mat)
    {
        if (mat == null)
        {
            return 0u;
        }

        if (mat.HasProperty(StartSpinRandStateBitsId))
        {
            float bits = mat.GetFloat(StartSpinRandStateBitsId);
            if (bits != 0f)
            {
                return unchecked((uint)BitConverter.SingleToInt32Bits(bits));
            }
        }

        return 0u;
    }

    public static void SetStartSpinRandState(Material mat, uint state)
    {
        if (mat != null && mat.HasProperty(StartSpinRandStateBitsId))
        {
            mat.SetFloat(
                StartSpinRandStateBitsId,
                BitConverter.Int32BitsToSingle(unchecked((int)state)));
        }
    }

    /// <summary>
    /// Writes the state immediately before SpriteEmitter StartSpin's
    /// FRangeVector::GetRand call. The shader consumes the verified nine
    /// appFrand draws from this state.
    /// </summary>
    public static void SetSpriteSpinRandState(Material mat, uint state)
    {
        if (mat != null && mat.HasProperty(SpriteSpinRandStateBitsId))
        {
            mat.SetFloat(
                SpriteSpinRandStateBitsId,
                BitConverter.Int32BitsToSingle(unchecked((int)state)));
        }
    }

    /// <summary>
    /// Writes the state immediately before SpriteEmitter StartVelocityRange's
    /// FRangeVector::GetRand call. The shader consumes its Z/Y/X draws.
    /// </summary>
    public static void SetSpriteMotionRandState(Material mat, uint state)
    {
        if (mat != null && mat.HasProperty(SpriteMotionRandStateBitsId))
        {
            mat.SetFloat(
                SpriteMotionRandStateBitsId,
                BitConverter.Int32BitsToSingle(unchecked((int)state)));
        }
    }

    /// <summary>
    /// Opt-in diagnostic replay for the captured m_u004_b SpriteEmitter0 slot 0.
    /// It is intentionally limited to slot 0 and does not alter normal gameplay
    /// while _L2MotionReplayEnabled remains zero on the shared material.
    /// </summary>
    public static bool ApplyHealingPotionSe0MotionReplay(Material mat, int slotIndex)
    {
        if (mat == null || slotIndex != 0 ||
            !mat.HasProperty(L2MotionReplayEnabledId) ||
            mat.GetFloat(L2MotionReplayEnabledId) <= 0.5f)
        {
            return false;
        }

        SetSpriteMotionRandState(mat, HealingPotionSe0ReplaySpawnState);
        SetSpriteSpinRandState(mat, AdvanceAppRandState(HealingPotionSe0ReplaySpawnState, 22));
        if (mat.HasProperty(SpawnDeltaTimeId))
        {
            mat.SetFloat(SpawnDeltaTimeId, HealingPotionSe0ReplaySpawnDeltaTime);
        }

        return true;
    }

    public static uint ReadSpriteSpinRandState(Material mat)
    {
        if (mat != null && mat.HasProperty(SpriteSpinRandStateBitsId))
        {
            float bits = mat.GetFloat(SpriteSpinRandStateBitsId);
            if (bits != 0f)
            {
                return unchecked((uint)BitConverter.SingleToInt32Bits(bits));
            }
        }

        return 0u;
    }

    public static float UIntBitsToFloat(uint state)
    {
        return BitConverter.Int32BitsToSingle(unchecked((int)state));
    }

    public static void ResolveGpuInstanceRandBits(
        Material sharedMat,
        uint meshEmitter3AppRandBaseState,
        uint spriteEmitterAppRandBaseState,
        int slotIndex,
        out float meshSpawnRandBits,
        out float startSpinRandBits,
        out float spriteMotionRandBits,
        out float spriteSpinRandBits)
    {
        L2AppRand.ResolveGpuInstanceRandBits(
            IsMeshSpawnParticleMaterial(sharedMat),
            IsMeshStartSpinMaterial(sharedMat),
            meshEmitter3AppRandBaseState,
            spriteEmitterAppRandBaseState,
            slotIndex,
            out meshSpawnRandBits,
            out startSpinRandBits,
            out spriteMotionRandBits,
            out spriteSpinRandBits);
    }

    /// <summary>
    /// Produces a finite bit-pattern that survives Material.SetFloat/asuint.
    /// It is a state seed, not a float numeric value.
    /// </summary>
    public static uint CreateFiniteAppRandState()
    {
        uint high = (uint)UnityEngine.Random.Range(0, 32768);
        uint low = (uint)UnityEngine.Random.Range(0, 65536);
        uint state = ((high << 16) | low) & 0x7F7FFFFFu;
        return state == 0u ? 1u : state;
    }

    public static uint AdvanceAppRandState(uint state, int drawCount)
    {
        return L2AppRand.Advance(state, drawCount);
    }

    public static float ReadLifetimeMax(Material[] sharedMaterials, float fallback)
    {
        if (sharedMaterials == null)
        {
            return fallback;
        }

        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            Material mat = sharedMaterials[i];
            if (mat != null && mat.HasProperty(LifetimeRangeId))
            {
                return mat.GetVector(LifetimeRangeId).y;
            }
        }

        return fallback;
    }

    public static void SetFloatOnMaterials(Material[] materials, int propertyId, float value)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && mat.HasProperty(propertyId))
            {
                mat.SetFloat(propertyId, value);
            }
        }
    }

    public static void SetVectorOnMaterials(Material[] materials, int propertyId, Vector4 value)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && mat.HasProperty(propertyId))
            {
                mat.SetVector(propertyId, value);
            }
        }
    }

    public static float ReadFloatFromFirstMaterial(Material[] materials, int propertyId, float fallback)
    {
        if (materials == null)
        {
            return fallback;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && mat.HasProperty(propertyId))
            {
                return mat.GetFloat(propertyId);
            }
        }

        return fallback;
    }

    private static void RestoreShaderLifetimeFromSharedFadeAuthored(Material runtimeMat, Material sharedMat)
    {
        if (runtimeMat == null || sharedMat == null || !runtimeMat.HasProperty(HasLifetimeId))
        {
            return;
        }

        float sharedHasLifetime = sharedMat.HasProperty(HasLifetimeId) ? sharedMat.GetFloat(HasLifetimeId) : 0f;
        float fadeOut = sharedMat.HasProperty(FadeoutId) ? sharedMat.GetFloat(FadeoutId) : 0f;
        if (sharedHasLifetime > 0.5f || fadeOut > 0.5f)
        {
            runtimeMat.SetFloat(HasLifetimeId, 1f);
        }
    }

    private static void CopyFloatIfPresent(Material runtimeMat, Material sharedMat, int propertyId)
    {
        if (runtimeMat.HasProperty(propertyId) && sharedMat.HasProperty(propertyId))
        {
            runtimeMat.SetFloat(propertyId, sharedMat.GetFloat(propertyId));
        }
    }

    private static void CopyVectorIfPresent(Material runtimeMat, Material sharedMat, int propertyId)
    {
        if (runtimeMat.HasProperty(propertyId) && sharedMat.HasProperty(propertyId))
        {
            runtimeMat.SetVector(propertyId, sharedMat.GetVector(propertyId));
        }
    }

    private static void CopyColorIfPresent(Material runtimeMat, Material sharedMat, int propertyId)
    {
        if (runtimeMat.HasProperty(propertyId) && sharedMat.HasProperty(propertyId))
        {
            runtimeMat.SetColor(propertyId, sharedMat.GetColor(propertyId));
        }
    }
}
