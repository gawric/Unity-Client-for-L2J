#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

/// <summary>
/// CPU mirror of HealingPotionTaCalibSpriteEmitter0.shader spawn + PTVD + motion.
/// Used by Unity_ParticleSnapshot.log to compare trajectories with L2 SpawnParticleSnapshot.log.
/// </summary>
public static class DocExtractorSpriteEmitter0MotionSimulator
{
    public const float UuToMeters = 1f / 52.5f;
    public const int SlotToSlotDrawCount = 28;

    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    private static readonly int SpriteMotionRandStateBitsId = Shader.PropertyToID("_SpriteMotionRandStateBits");
    private static readonly int StartLocationOffsetUuId = Shader.PropertyToID("_StartLocationOffsetUU");
    private static readonly int StartLocationOffsetUcId = Shader.PropertyToID("_StartLocationOffsetUc");
    private static readonly int StartLocationRangeXUcId = Shader.PropertyToID("_StartLocationRangeXUc");
    private static readonly int StartLocationRangeYUcId = Shader.PropertyToID("_StartLocationRangeYUc");
    private static readonly int StartLocationRangeZUcId = Shader.PropertyToID("_StartLocationRangeZUc");
    private static readonly int StartVelocityRadialRangeUcId = Shader.PropertyToID("_StartVelocityRadialRangeUc");
    private static readonly int VelocityLossRangeUcId = Shader.PropertyToID("_VelocityLossRangeUc");
    private static readonly int PolarThetaRangeUcId = Shader.PropertyToID("_PolarThetaRangeUc");
    private static readonly int PolarPhiRangeUcId = Shader.PropertyToID("_PolarPhiRangeUc");
    private static readonly int PolarRadiusRangeUcId = Shader.PropertyToID("_PolarRadiusRangeUc");
    private static readonly int StartVelocityRangeXUcId = Shader.PropertyToID("_StartVelocityRangeXUc");
    private static readonly int StartVelocityRangeYUcId = Shader.PropertyToID("_StartVelocityRangeYUc");
    private static readonly int StartVelocityRangeZUcId = Shader.PropertyToID("_StartVelocityRangeZUc");
    private static readonly int AccelerationUcId = Shader.PropertyToID("_AccelerationUc");
    private static readonly int SpawnDeltaTimeId = Shader.PropertyToID("_SpawnDeltaTime");
    private static readonly int LifetimeRangeId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int InitialDelayRangeId = Shader.PropertyToID("_InitialDelayRange");
    private static readonly int SizeRangeId = Shader.PropertyToID("_SizeRange");
    private static readonly int SizeRangeXUcId = Shader.PropertyToID("_SizeRangeXUc");
    private static readonly int ColorMulMinId = Shader.PropertyToID("_ColorMulMin");
    private static readonly int ColorMulMaxId = Shader.PropertyToID("_ColorMulMax");
    private static readonly int SizeScaleRepeatsId = Shader.PropertyToID("_SizeScaleRepeats");
    private static readonly int SizeKey0Id = Shader.PropertyToID("_SizeKey0");
    private static readonly int SizeKey1Id = Shader.PropertyToID("_SizeKey1");
    private static readonly int SizeKey2Id = Shader.PropertyToID("_SizeKey2");
    private static readonly int SizeKey3Id = Shader.PropertyToID("_SizeKey3");
    private static readonly int SizeKey4Id = Shader.PropertyToID("_SizeKey4");
    private static readonly int WorldCalibrationId = Shader.PropertyToID("_L2FxWorldCalibration");

    private const float DegToRad = 0.01745329252f;

    public struct SpawnSnapshot
    {
        public uint AppRandStateBeforeSpawn;
        public Vector3 RawVelocityUe;
        public Vector3 PolarOffsetUe;
        public Vector3 SpawnPositionUe;
        public Vector3 VelocityBeforePtvdUe;
        public Vector3 VelocityAfterPtvdUe;
        public Vector3 PtvdDirectionUe;
        public Vector3 ColorMulRgb;
        public float LifetimeSeconds;
        public float SpawnSizeUU;
    }

    public struct MotionSample
    {
        public SpawnSnapshot Spawn;
        public Vector3 LocLocalUe;
        public Vector3 LocWorldUe;
        public Vector3 SizeUe;
        public float ParticleTime;
        public float AgeNorm;
        public float MaxLifetime;
        public float LifeRemain;
        public Vector3 VelocityNowUe;
        public Vector3 DisplacementUe;
    }

    public static bool IsSpriteEmitter0Material(Material mat)
    {
        // SE0 calib has StartLocationOffsetUU; kirakira SE7 shares _SpriteMotionRandStateBits
        // but must not take this path (wrong labels, no ColorScale A8 log).
        return mat != null &&
               mat.HasProperty(SpriteMotionRandStateBitsId) &&
               mat.HasProperty(StartLocationOffsetUuId);
    }

    public static bool IsKirakiraSpriteEmitter7Material(Material mat)
    {
        // SE324/SE325 also have ColorMul + motion rand; exclude via OffsetUc / LocRange / VelLoss.
        return mat != null &&
               mat.HasProperty(SpriteMotionRandStateBitsId) &&
               mat.HasProperty(ColorMulMinId) &&
               !mat.HasProperty(StartLocationOffsetUuId) &&
               !IsShotNAtkSpriteEmitter324Material(mat) &&
               !IsShotNAtkSpriteEmitter325Material(mat);
    }

    /// <summary>
    /// shot_N_atk_v1_ta / SpriteEmitter324 "Particle": OffsetUc + Polar + PTVD2 + VelLoss.
    /// Distinct from teleport upline (uses _SizeRangeXUc / _SizeRangeYUc, not _SizeRange).
    /// </summary>
    public static bool IsShotNAtkSpriteEmitter324Material(Material mat)
    {
        return mat != null &&
               mat.HasProperty(SpriteMotionRandStateBitsId) &&
               mat.HasProperty(StartLocationOffsetUcId) &&
               mat.HasProperty(VelocityLossRangeUcId) &&
               mat.HasProperty(SizeRangeId) &&
               mat.HasProperty(PolarRadiusRangeUcId) &&
               !mat.HasProperty(SizeRangeXUcId);
    }

    /// <summary>
    /// shot_N_atk_v1_ta / SpriteEmitter325 "smog": shape0 LocRange stream + VelLoss, no Polar/PTVD.
    /// </summary>
    public static bool IsShotNAtkSpriteEmitter325Material(Material mat)
    {
        return mat != null &&
               mat.HasProperty(SpriteMotionRandStateBitsId) &&
               mat.HasProperty(VelocityLossRangeUcId) &&
               mat.HasProperty(SizeRangeId) &&
               mat.HasProperty(StartLocationRangeXUcId) &&
               !mat.HasProperty(PolarRadiusRangeUcId) &&
               !mat.HasProperty(StartLocationOffsetUcId) &&
               !mat.HasProperty(SizeRangeXUcId);
    }

    public static bool TryEvaluate(
        Transform groupTransform,
        Material mat,
        float now,
        float shaderStartTime,
        out MotionSample sample)
    {
        sample = default;
        bool isSe324 = IsShotNAtkSpriteEmitter324Material(mat);
        bool isSe325 = IsShotNAtkSpriteEmitter325Material(mat);
        if (groupTransform == null ||
            (!IsSpriteEmitter0Material(mat) &&
             !IsKirakiraSpriteEmitter7Material(mat) &&
             !isSe324 &&
             !isSe325))
        {
            return false;
        }

        float startTime = mat.HasProperty(StartTimeId) ? mat.GetFloat(StartTimeId) : shaderStartTime;
        if (startTime < -0.49f)
        {
            startTime = shaderStartTime;
        }

        uint state = ReadAppRandStateBits(mat);
        if (state == 0u)
        {
            return false;
        }

        SpawnSnapshot spawn = isSe325
            ? EvaluateSpawnParticle325(mat, state)
            : isSe324
                ? EvaluateSpawnParticle324(mat, state)
                : EvaluateSpawn(mat, state);
        float ageSeconds = Mathf.Max(0f, now - startTime);
        float ageNorm = Mathf.Clamp01(ageSeconds / Mathf.Max(1e-4f, spawn.LifetimeSeconds));
        float repeats = mat.HasProperty(SizeScaleRepeatsId) ? mat.GetFloat(SizeScaleRepeatsId) : 0f;
        Vector4 key0 = ReadSizeKey(mat, SizeKey0Id, new Vector4(0f, 1f, 0f, 0f));
        Vector4 key1 = ReadSizeKey(mat, SizeKey1Id, key0);
        Vector4 key2 = ReadSizeKey(mat, SizeKey2Id, key1);
        Vector4 key3 = ReadSizeKey(mat, SizeKey3Id, new Vector4(1f, key2.y, 0f, 0f));
        Vector4 key4 = ReadSizeKey(mat, SizeKey4Id, key3);
        float sizeMul = EvaluateDynamicSizeScale(ageNorm, repeats, key0, key1, key2, key3, key4);
        float sizeUU = spawn.SpawnSizeUU * sizeMul;

        Vector3 accelerationUe = mat.HasProperty(AccelerationUcId)
            ? ToVector3(mat.GetVector(AccelerationUcId))
            : Vector3.zero;
        sample.Spawn = spawn;
        if (isSe324 || isSe325)
        {
            Vector3 lossUe = ToVector3(mat.GetVector(VelocityLossRangeUcId));
            sample.DisplacementUe = DisplacementUeWithDrag(
                spawn.VelocityAfterPtvdUe, accelerationUe, lossUe, ageSeconds);
            sample.VelocityNowUe = VelocityNowWithDrag(
                spawn.VelocityAfterPtvdUe, accelerationUe, lossUe, ageSeconds);
        }
        else
        {
            sample.DisplacementUe = DisplacementUe(spawn.VelocityAfterPtvdUe, accelerationUe, ageSeconds);
            sample.VelocityNowUe = spawn.VelocityAfterPtvdUe + accelerationUe * ageSeconds;
        }

        sample.LocLocalUe = spawn.SpawnPositionUe + sample.DisplacementUe;

        float worldK = mat.HasProperty(WorldCalibrationId) ? mat.GetFloat(WorldCalibrationId) : 1.8f;
        Vector3 offsetUnity = UcPositionToUnityMetersInternal(sample.LocLocalUe, worldK);
        Vector3 worldUnity = groupTransform.TransformPoint(offsetUnity);
        sample.LocWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(worldUnity);

        sample.SizeUe = new Vector3(sizeUU, sizeUU, sizeUU);
        sample.ParticleTime = ageSeconds;
        sample.AgeNorm = ageNorm;
        sample.MaxLifetime = spawn.LifetimeSeconds;
        sample.LifeRemain = Mathf.Max(0f, spawn.LifetimeSeconds - ageSeconds);
        return true;
    }

    public static SpawnSnapshot EvaluateSpawn(Material mat, uint stateBeforeSpawn)
    {
        uint state = stateBeforeSpawn;
        Vector2 velX = ToMinMax(mat.GetVector(StartVelocityRangeXUcId));
        Vector2 velY = ToMinMax(mat.GetVector(StartVelocityRangeYUcId));
        Vector2 velZ = ToMinMax(mat.GetVector(StartVelocityRangeZUcId));
        Vector3 rawVelocityUe = FRangeVectorGetRandYawPitchRoll(velX, velY, velZ, ref state);

        Vector3 polarUe = SpritePolarGetRandUe(
            ToMinMax(mat.GetVector(PolarThetaRangeUcId)),
            ToMinMax(mat.GetVector(PolarPhiRangeUcId)),
            ToMinMax(mat.GetVector(PolarRadiusRangeUcId)),
            ref state);

        for (int i = 0; i < 10; i++)
        {
            AppRand(ref state);
        }

        Vector2 lifetimeRange = ToMinMax(mat.GetVector(LifetimeRangeId));
        float lifetimeSeconds = FRangeGetRand(lifetimeRange, ref state);
        AppRand(ref state);
        AppRand(ref state);

        Vector2 sizeRange = ToMinMax(mat.GetVector(SizeRangeId));
        float spawnSizeUU = FRangeGetRand(sizeRange, ref state);
        AppRand(ref state);
        AppRand(ref state);

        Vector3 offsetUe = mat.HasProperty(StartLocationOffsetUuId)
            ? ToVector3(mat.GetVector(StartLocationOffsetUuId))
            : Vector3.zero;
        Vector3 spawnPositionUe = offsetUe + polarUe;
        Vector3 accelerationUe = mat.HasProperty(AccelerationUcId)
            ? ToVector3(mat.GetVector(AccelerationUcId))
            : Vector3.zero;
        float spawnDeltaTime = mat.HasProperty(SpawnDeltaTimeId) ? mat.GetFloat(SpawnDeltaTimeId) : 0.012f;
        Vector3 velocityBeforePtvdUe = rawVelocityUe + accelerationUe * spawnDeltaTime;

        Vector3 ptvdDirectionUe = spawnPositionUe;
        float directionLength = ptvdDirectionUe.magnitude;
        if (directionLength > 1e-5f)
        {
            ptvdDirectionUe /= directionLength;
        }

        // SE0 / mode 1: negated (PTVD_StartPositionAndOwner).
        Vector3 velocityAfterPtvdUe = directionLength > 1e-5f
            ? Vector3.Scale(-velocityBeforePtvdUe, ptvdDirectionUe)
            : Vector3.zero;

        return new SpawnSnapshot
        {
            AppRandStateBeforeSpawn = stateBeforeSpawn,
            RawVelocityUe = rawVelocityUe,
            PolarOffsetUe = polarUe,
            SpawnPositionUe = spawnPositionUe,
            VelocityBeforePtvdUe = velocityBeforePtvdUe,
            VelocityAfterPtvdUe = velocityAfterPtvdUe,
            PtvdDirectionUe = ptvdDirectionUe,
            ColorMulRgb = Vector3.one,
            LifetimeSeconds = lifetimeSeconds,
            SpawnSizeUU = spawnSizeUU
        };
    }

    /// <summary>
    /// CPU mirror of SpriteEmitter324_Particle.shader spawn:
    /// Vel → Polar → Lifetime → Size; OffsetUc+Polar; PTVD_OwnerAndStartPosition (no negate).
    /// </summary>
    public static SpawnSnapshot EvaluateSpawnParticle324(Material mat, uint stateBeforeSpawn)
    {
        uint state = stateBeforeSpawn;
        Vector2 velX = ToMinMax(mat.GetVector(StartVelocityRangeXUcId));
        Vector2 velY = ToMinMax(mat.GetVector(StartVelocityRangeYUcId));
        Vector2 velZ = ToMinMax(mat.GetVector(StartVelocityRangeZUcId));
        Vector3 rawVelocityUe = FRangeVectorGetRandYawPitchRoll(velX, velY, velZ, ref state);

        Vector3 polarUe = SpritePolarGetRandUe(
            ToMinMax(mat.GetVector(PolarThetaRangeUcId)),
            ToMinMax(mat.GetVector(PolarPhiRangeUcId)),
            ToMinMax(mat.GetVector(PolarRadiusRangeUcId)),
            ref state);

        Vector2 lifetimeRange = ToMinMax(mat.GetVector(LifetimeRangeId));
        float lifetimeSeconds = FRangeGetRand(lifetimeRange, ref state);
        Vector2 sizeRange = ToMinMax(mat.GetVector(SizeRangeId));
        float spawnSizeUU = FRangeGetRand(sizeRange, ref state);

        Vector3 offsetUe = mat.HasProperty(StartLocationOffsetUcId)
            ? ToVector3(mat.GetVector(StartLocationOffsetUcId))
            : Vector3.zero;
        Vector3 spawnPositionUe = offsetUe + polarUe;
        Vector3 accelerationUe = mat.HasProperty(AccelerationUcId)
            ? ToVector3(mat.GetVector(AccelerationUcId))
            : Vector3.zero;
        float spawnDeltaTime = mat.HasProperty(SpawnDeltaTimeId) ? mat.GetFloat(SpawnDeltaTimeId) : 0.012f;
        Vector3 velocityBeforePtvdUe = rawVelocityUe + accelerationUe * spawnDeltaTime;

        Vector3 ptvdDirectionUe = spawnPositionUe;
        float directionLength = ptvdDirectionUe.magnitude;
        if (directionLength > 1e-5f)
        {
            ptvdDirectionUe /= directionLength;
        }

        Vector3 velocityAfterPtvdUe = directionLength > 1e-5f
            ? Vector3.Scale(velocityBeforePtvdUe, ptvdDirectionUe)
            : Vector3.zero;

        return new SpawnSnapshot
        {
            AppRandStateBeforeSpawn = stateBeforeSpawn,
            RawVelocityUe = rawVelocityUe,
            PolarOffsetUe = polarUe,
            SpawnPositionUe = spawnPositionUe,
            VelocityBeforePtvdUe = velocityBeforePtvdUe,
            VelocityAfterPtvdUe = velocityAfterPtvdUe,
            PtvdDirectionUe = ptvdDirectionUe,
            ColorMulRgb = Vector3.one,
            LifetimeSeconds = lifetimeSeconds,
            SpawnSizeUU = spawnSizeUU
        };
    }

    /// <summary>
    /// CPU mirror of SpriteEmitter325_Smog.shader / L2FxSpriteSpawnParticle shape0:
    /// Vel → LocRange → +0x2FC → 2×zeroVec → ColorMul → Life → Delay → Radial → Size(vector.x).
    /// No Polar / PTVD. VelocityAfter = raw + Accel*SpawnDeltaTime.
    /// </summary>
    public static SpawnSnapshot EvaluateSpawnParticle325(Material mat, uint stateBeforeSpawn)
    {
        uint state = stateBeforeSpawn;
        Vector2 velX = ToMinMax(mat.GetVector(StartVelocityRangeXUcId));
        Vector2 velY = ToMinMax(mat.GetVector(StartVelocityRangeYUcId));
        Vector2 velZ = ToMinMax(mat.GetVector(StartVelocityRangeZUcId));
        Vector3 rawVelocityUe = FRangeVectorGetRandYawPitchRoll(velX, velY, velZ, ref state);

        Vector2 locX = mat.HasProperty(StartLocationRangeXUcId)
            ? ToMinMax(mat.GetVector(StartLocationRangeXUcId))
            : Vector2.zero;
        Vector2 locY = mat.HasProperty(StartLocationRangeYUcId)
            ? ToMinMax(mat.GetVector(StartLocationRangeYUcId))
            : Vector2.zero;
        Vector2 locZ = mat.HasProperty(StartLocationRangeZUcId)
            ? ToMinMax(mat.GetVector(StartLocationRangeZUcId))
            : Vector2.zero;
        Vector3 locationUe = FRangeVectorGetRandYawPitchRoll(locX, locY, locZ, ref state);

        // [6] +0x2FC [0,1]
        FRangeGetRand(new Vector2(0f, 1f), ref state);
        // [7..12] unused zero vectors
        for (int i = 0; i < 6; i++)
        {
            AppFrand(ref state);
        }

        Vector2 mulX = mat.HasProperty(ColorMulMinId) && mat.HasProperty(ColorMulMaxId)
            ? new Vector2(mat.GetVector(ColorMulMinId).x, mat.GetVector(ColorMulMaxId).x)
            : new Vector2(0.716f, 0.716f);
        Vector2 mulY = mat.HasProperty(ColorMulMinId) && mat.HasProperty(ColorMulMaxId)
            ? new Vector2(mat.GetVector(ColorMulMinId).y, mat.GetVector(ColorMulMaxId).y)
            : new Vector2(0.83f, 0.83f);
        Vector2 mulZ = mat.HasProperty(ColorMulMinId) && mat.HasProperty(ColorMulMaxId)
            ? new Vector2(mat.GetVector(ColorMulMinId).z, mat.GetVector(ColorMulMaxId).z)
            : new Vector2(0.784f, 0.784f);
        Vector3 colorMulRgb = FRangeVectorGetRandYawPitchRoll(mulX, mulY, mulZ, ref state);

        float lifetimeSeconds = FRangeGetRand(ToMinMax(mat.GetVector(LifetimeRangeId)), ref state);
        Vector2 delayRange = mat.HasProperty(InitialDelayRangeId)
            ? ToMinMax(mat.GetVector(InitialDelayRangeId))
            : Vector2.zero;
        FRangeGetRand(delayRange, ref state);
        Vector2 radialRange = mat.HasProperty(StartVelocityRadialRangeUcId)
            ? ToMinMax(mat.GetVector(StartVelocityRadialRangeUcId))
            : new Vector2(1f, 1f);
        FRangeGetRand(radialRange, ref state);

        Vector2 sizeRange = ToMinMax(mat.GetVector(SizeRangeId));
        Vector3 sizeUu = FRangeVectorGetRandYawPitchRoll(sizeRange, sizeRange, sizeRange, ref state);

        Vector3 accelerationUe = mat.HasProperty(AccelerationUcId)
            ? ToVector3(mat.GetVector(AccelerationUcId))
            : Vector3.zero;
        float spawnDeltaTime = mat.HasProperty(SpawnDeltaTimeId) ? mat.GetFloat(SpawnDeltaTimeId) : 0.012f;
        Vector3 velocity0Ue = rawVelocityUe + accelerationUe * spawnDeltaTime;

        return new SpawnSnapshot
        {
            AppRandStateBeforeSpawn = stateBeforeSpawn,
            RawVelocityUe = rawVelocityUe,
            PolarOffsetUe = Vector3.zero,
            SpawnPositionUe = locationUe,
            VelocityBeforePtvdUe = velocity0Ue,
            VelocityAfterPtvdUe = velocity0Ue,
            PtvdDirectionUe = Vector3.zero,
            ColorMulRgb = colorMulRgb,
            LifetimeSeconds = lifetimeSeconds,
            SpawnSizeUU = sizeUu.x
        };
    }

    public static uint ReadAppRandStateBits(Material mat)
    {
        if (mat == null || !mat.HasProperty(SpriteMotionRandStateBitsId))
        {
            return 0u;
        }

        float bits = mat.GetFloat(SpriteMotionRandStateBitsId);
        if (bits == 0f)
        {
            return 0u;
        }

        return unchecked((uint)BitConverter.SingleToInt32Bits(bits));
    }

    private static Vector3 DisplacementUe(Vector3 velocity0Ue, Vector3 accelerationUe, float ageSeconds)
    {
        float t = Mathf.Max(0f, ageSeconds);
        return velocity0Ue * t + 0.5f * accelerationUe * t * t;
    }

    // Mirrors L2Fx_SpriteMotion_DisplacementUeWithDrag / DisplacementComponentWithDrag.
    private static Vector3 DisplacementUeWithDrag(
        Vector3 velocity0Ue,
        Vector3 accelerationUe,
        Vector3 lossPerSecUe,
        float ageSeconds)
    {
        float t = Mathf.Max(0f, ageSeconds);
        Vector3 res = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            float k = lossPerSecUe[i];
            if (k > 1e-6f)
            {
                float vInf = accelerationUe[i] / k;
                res[i] = vInf * t + (velocity0Ue[i] - vInf) * (1f - Mathf.Exp(-k * t)) / k;
            }
            else
            {
                res[i] = velocity0Ue[i] * t + 0.5f * accelerationUe[i] * t * t;
            }
        }

        return res;
    }

    private static Vector3 VelocityNowWithDrag(
        Vector3 velocity0Ue,
        Vector3 accelerationUe,
        Vector3 lossPerSecUe,
        float ageSeconds)
    {
        float t = Mathf.Max(0f, ageSeconds);
        Vector3 res = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            float k = lossPerSecUe[i];
            if (k > 1e-6f)
            {
                float vInf = accelerationUe[i] / k;
                res[i] = vInf + (velocity0Ue[i] - vInf) * Mathf.Exp(-k * t);
            }
            else
            {
                res[i] = velocity0Ue[i] + accelerationUe[i] * t;
            }
        }

        return res;
    }

    public static Vector3 UcPositionToUnityMeters(Vector3 uePositionUu, float worldCalibK)
    {
        // Match L2Fx_UcPositionToUnityMeters: world UU/52.5, size K does not scale motion.
        _ = worldCalibK;
        return new Vector3(uePositionUu.x, uePositionUu.z, uePositionUu.y) * UuToMeters;
    }

    private static Vector3 UcPositionToUnityMetersInternal(Vector3 uePositionUu, float worldCalibK)
    {
        return UcPositionToUnityMeters(uePositionUu, worldCalibK);
    }

    private static Vector4 ReadSizeKey(Material mat, int propertyId, Vector4 fallback)
    {
        return mat != null && mat.HasProperty(propertyId) ? mat.GetVector(propertyId) : fallback;
    }

    private static float EvaluateDynamicSizeScale(
        float progress,
        float repeats,
        Vector4 key0,
        Vector4 key1,
        Vector4 key2,
        Vector4 key3,
        Vector4 key4)
    {
        float phase = repeats > 0f
            ? Mathf.Repeat(progress * repeats, 1f)
            : Mathf.Clamp01(progress);

        Vector4[] keys = { key0, key1, key2, key3, key4 };
        if (keys[0].x > 0f && phase < keys[0].x)
        {
            return Mathf.Lerp(1f, keys[0].y, phase / Mathf.Max(keys[0].x, 1e-6f));
        }

        int idx = 0;
        while (idx < 4 && phase > keys[idx + 1].x)
        {
            idx++;
        }

        float t0 = keys[idx].x;
        float s0 = keys[idx].y;
        float t1 = keys[idx + 1].x;
        float s1 = keys[idx + 1].y;
        if (Mathf.Abs(t1 - t0) < 1e-6f)
        {
            return s0;
        }

        float u = (phase - t0) / (t1 - t0);
        return Mathf.Lerp(s0, s1, Mathf.Clamp01(u));
    }

    private static Vector3 SpritePolarGetRandUe(
        Vector2 thetaDegreesMinMax,
        Vector2 phiDegreesMinMax,
        Vector2 radiusUuMinMax,
        ref uint state)
    {
        float radiusUu = FRangeGetRand(radiusUuMinMax, ref state);
        float phiDegrees = FRangeGetRand(phiDegreesMinMax, ref state);
        float thetaDegrees = FRangeGetRand(thetaDegreesMinMax, ref state);
        return PolarCartesianUe(thetaDegrees, phiDegrees, radiusUu);
    }

    private static Vector3 PolarCartesianUe(float thetaDegrees, float phiDegrees, float radius)
    {
        float theta = thetaDegrees * DegToRad;
        float phi = phiDegrees * DegToRad;
        float sinPhi = Mathf.Sin(phi);
        return new Vector3(
            radius * sinPhi * Mathf.Cos(theta),
            radius * sinPhi * Mathf.Sin(theta),
            radius * Mathf.Cos(phi));
    }

    private static Vector3 FRangeVectorGetRandYawPitchRoll(
        Vector2 yawRange,
        Vector2 pitchRange,
        Vector2 rollRange,
        ref uint state)
    {
        float roll = FRangeGetRand(rollRange, ref state);
        float pitch = FRangeGetRand(pitchRange, ref state);
        float yaw = FRangeGetRand(yawRange, ref state);
        return new Vector3(yaw, pitch, roll);
    }

    private static float FRangeGetRand(Vector2 minMax, ref uint state)
    {
        return AppFrand(ref state) * (minMax.x - minMax.y) + minMax.y;
    }

    private static uint AppRand(ref uint state)
    {
        state = unchecked(state * 214013u + 2531011u);
        return (state >> 16) & 0x7fffu;
    }

    private static float AppFrand(ref uint state)
    {
        return AppRand(ref state) / 32767f;
    }

    private static Vector2 ToMinMax(Vector4 v)
    {
        return new Vector2(v.x, v.y);
    }

    private static Vector3 ToVector3(Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }
}
#endif
