#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// C# mirror of TeleportCaSpriteUpline.shader motion (spawn + displacement + size)
/// for DocExtractor / RenderDocExtractor emitter-log comparison with Lineage2 ParticleSnapshot.log.
/// </summary>
public static class DocExtractorParticleMotionSimulator
{
    public const float UuToUnity = 0.01f;
    public const float UnityToUu = 100f;

    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int InitialDelayRangeId = Shader.PropertyToID("_InitialDelayRange");
    private static readonly int LifetimeRangeId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int SpawnUnitScaleId = Shader.PropertyToID("_SpawnUnitScale");
    private static readonly int PolarAzimuthDegId = Shader.PropertyToID("_PolarAzimuthDeg");
    private static readonly int PolarPitchDegId = Shader.PropertyToID("_PolarPitchDeg");
    private static readonly int PolarRadiusId = Shader.PropertyToID("_PolarRadius");
    private static readonly int StartLocationOffsetId = Shader.PropertyToID("_StartLocationOffset");
    private static readonly int UcPolarRadiusScaleId = Shader.PropertyToID("_UcPolarRadiusScale");
    private static readonly int UcStartLocationOffsetScaleId = Shader.PropertyToID("_UcStartLocationOffsetScale");
    private static readonly int UcStartLocationRangeScaleId = Shader.PropertyToID("_UcStartLocationRangeScale");
    private static readonly int VelocityRangeZId = Shader.PropertyToID("_VelocityRangeZ");
    private static readonly int UcVelocityScaleId = Shader.PropertyToID("_UcVelocityScale");
    private static readonly int AccelerationId = Shader.PropertyToID("_Acceleration");
    private static readonly int UcAccelerationScaleId = Shader.PropertyToID("_UcAccelerationScale");
    private static readonly int VelocityLossRangeZId = Shader.PropertyToID("_VelocityLossRangeZ");
    private static readonly int SizeRangeXId = Shader.PropertyToID("_SizeRangeX");
    private static readonly int SizeRangeYId = Shader.PropertyToID("_SizeRangeY");
    private static readonly int SizeRangeZId = Shader.PropertyToID("_SizeRangeZ");
    private static readonly int UniformSizeId = Shader.PropertyToID("_UniformSize");
    private static readonly int L2FxEffectScaleId = Shader.PropertyToID("_L2FxEffectScale");
    private static readonly int L2FxSpriteScaleId = Shader.PropertyToID("_L2FxSpriteScale");
    private static readonly int UseSizeScaleId = Shader.PropertyToID("_UseSizeScale");
    private static readonly int SizeScaleTime0Id = Shader.PropertyToID("_SizeScaleTime0");
    private static readonly int SizeScaleVal0Id = Shader.PropertyToID("_SizeScaleVal0");
    private static readonly int SizeScaleTime1Id = Shader.PropertyToID("_SizeScaleTime1");
    private static readonly int SizeScaleVal1Id = Shader.PropertyToID("_SizeScaleVal1");
    private static readonly int SpinParticlesId = Shader.PropertyToID("_SpinParticles");
    private static readonly int HasLifetimeId = Shader.PropertyToID("_HasLifetime");
    private static readonly int FadeInId = Shader.PropertyToID("_FadeIn");
    private static readonly int FadeInEndTimeId = Shader.PropertyToID("_FadeInEndTime");
    private static readonly int FadeoutId = Shader.PropertyToID("_Fadeout");
    private static readonly int FadeoutStartTimeId = Shader.PropertyToID("_FadeoutStartTime");
    private static readonly int ColorMultMinId = Shader.PropertyToID("_ColorMultMin");
    private static readonly int ColorMultMaxId = Shader.PropertyToID("_ColorMultMax");
    private static readonly int ColorMulMinId = Shader.PropertyToID("_ColorMulMin");
    private static readonly int ColorMulMaxId = Shader.PropertyToID("_ColorMulMax");
    private static readonly int ColorScaleCountId = Shader.PropertyToID("_ColorScaleCount");
    private static readonly int ColorScaleParamId = Shader.PropertyToID("_ColorScaleParam");
    private static readonly int ColorKey0Id = Shader.PropertyToID("_ColorKey0");
    private static readonly int ColorKey1TimeId = Shader.PropertyToID("_ColorKey1Time");
    private static readonly int ColorKey1Id = Shader.PropertyToID("_ColorKey1");
    private static readonly int ColorKey2TimeId = Shader.PropertyToID("_ColorKey2Time");
    private static readonly int ColorKey2Id = Shader.PropertyToID("_ColorKey2");
    private static readonly int ColorKey3TimeId = Shader.PropertyToID("_ColorKey3Time");
    private static readonly int ColorKey3Id = Shader.PropertyToID("_ColorKey3");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

    private const float SpinDisabledSentinel = -11796480f;
    private const float DegToRad = 0.01745329252f;

    public struct MotionSample
    {
        public Vector3 LocLocalUe;
        public Vector3 LocWorldUe;
        public Vector3 SizeUe;
        public Vector3 RotationEuler;
        public Vector3 StartSpin;
        public Vector3 SpinsPerSec;
        public Vector3 RevCenter;
        public Vector3 RevPerSec;
        public float ParticleTime;
        public float AgeNorm;
        public float MaxLifetime;
        public float LifeRemain;

        // Color mirror of the shader vertex path (in_Color0 == L2 runtimeColorA8 analog).
        public Vector3 ColorMultiplier; // (R, G, B), fixed at spawn from ColorMultRange
        public float ColorMultiplierA;
        public float Opacity;            // .uc Opacity (draw-stage; not in L2 runtimeColorA8)
        public float HdrPeak;
        public float LifeAlpha;          // post-fade alpha (A8), 0..1
        public Color RuntimeColorRgba;   // ColorScale*ColorMul - fade (A8 mirror), 0..1 RGBA
    }

    public static bool TryEvaluate(
        Transform groupTransform,
        Material mat,
        float now,
        float shaderStartTime,
        float seed,
        out MotionSample sample)
    {
        sample = default;
        if (groupTransform == null || mat == null || !mat.HasProperty(StartTimeId))
        {
            return false;
        }

        float pSeed = mat.HasProperty(SeedId) ? mat.GetFloat(SeedId) : seed;
        float startTime = mat.GetFloat(StartTimeId);
        if (startTime < -0.49f)
        {
            startTime = shaderStartTime;
        }

        Vector2 delayRange = mat.HasProperty(InitialDelayRangeId)
            ? new Vector2(mat.GetVector(InitialDelayRangeId).x, mat.GetVector(InitialDelayRangeId).y)
            : Vector2.zero;
        Vector2 lifetimeRange = mat.HasProperty(LifetimeRangeId)
            ? new Vector2(mat.GetVector(LifetimeRangeId).x, mat.GetVector(LifetimeRangeId).y)
            : new Vector2(1f, 1f);

        float delay = Mathf.Max(0f, RandomRange(delayRange, pSeed, startTime, 3f));
        float lifetime = Mathf.Max(1e-4f, RandomRange(lifetimeRange, pSeed, startTime, 7f));
        float age = Mathf.Max(0f, now - startTime - delay);
        float ageNorm = Mathf.Clamp01(age / lifetime);

        float unitScale = mat.HasProperty(SpawnUnitScaleId) ? mat.GetFloat(SpawnUnitScaleId) : UuToUnity;
        if (unitScale <= 0f)
        {
            unitScale = UuToUnity;
        }

        Vector3 spawnOfsUnity = ComputeSpawnOffsetUnity(mat, pSeed, startTime, unitScale);
        Vector3 displacementUnity = ComputeDisplacementUnity(mat, pSeed, startTime, unitScale, age);
        Vector3 spawnOfsTotalUnity = spawnOfsUnity + displacementUnity;

        sample.LocLocalUe = UnityObjectOffsetToUe(spawnOfsTotalUnity, unitScale);
        Vector3 worldUnity = groupTransform.TransformPoint(spawnOfsTotalUnity);
        sample.LocWorldUe = UnityWorldToUe(worldUnity);

        sample.SizeUe = ComputeLogSizeUe(mat, pSeed, startTime, ageNorm);
        sample.RotationEuler = Vector3.zero;
        sample.StartSpin = Vector3.zero;
        sample.RevCenter = Vector3.zero;
        sample.RevPerSec = Vector3.zero;
        sample.ParticleTime = age;
        sample.AgeNorm = ageNorm;
        sample.MaxLifetime = lifetime;
        sample.LifeRemain = Mathf.Max(0f, lifetime - age);

        ComputeColor(mat, pSeed, startTime, age, delay, lifetime, ageNorm, ref sample);

        bool spinOn = mat.HasProperty(SpinParticlesId) && mat.GetFloat(SpinParticlesId) > 0.5f;
        sample.SpinsPerSec = spinOn ? Vector3.zero : new Vector3(SpinDisabledSentinel, 0f, SpinDisabledSentinel);

        return true;
    }

    // Mirrors L2Fx_SpriteColorFade_FullKeys (engine runtimeColorA8 @+0xA8):
    // ColorScale(+Repeats) -> * ColorMultiplier -> subtractive FadeIn/Out.
    // Opacity is NOT folded into A8 (engine draw-stage only; Brighten ignores A for RGB).
    private static void ComputeColor(
        Material mat,
        float seed,
        float startTime,
        float age,
        float delay,
        float lifetime,
        float ageNorm,
        ref MotionSample sample)
    {
        Color multMin = ReadColorMulMin(mat);
        Color multMax = ReadColorMulMax(mat);

        // L2Fx_ApplyColorMultiplier: independent random per channel (seed, seed+1, seed+2).
        float r = RandomRange(new Vector2(multMin.r, multMax.r), seed, startTime, 197f);
        float g = RandomRange(new Vector2(multMin.g, multMax.g), seed + 1f, startTime, 199f);
        float b = RandomRange(new Vector2(multMin.b, multMax.b), seed + 2f, startTime, 211f);

        sample.ColorMultiplier = new Vector3(r, g, b);
        sample.ColorMultiplierA = 1f;
        sample.Opacity = mat.HasProperty(OpacityId) ? mat.GetFloat(OpacityId) : 1f;
        sample.HdrPeak = Mathf.Max(r, Mathf.Max(g, b));

        Color scale = SampleColorScaleFull(mat, ageNorm);
        Color spawn = new Color(scale.r * r, scale.g * g, scale.b * b, scale.a);
        Color runtime = ApplySubtractiveFade(mat, spawn, age, lifetime);
        sample.RuntimeColorRgba = runtime;
        sample.LifeAlpha = runtime.a;
    }

    /// <summary>
    /// Color-only mirror of L2Fx_SpriteColorFade_FullKeys for snapshot compare
    /// (ColorScale*ColorMul - Fade). Opacity is reported separately and is not in A8.
    /// </summary>
    public static bool TryEvaluateColor(
        Material mat,
        float seed,
        float startTime,
        float ageSeconds,
        float lifetimeSeconds,
        out Vector3 colorMultiplier,
        out float hdrPeak,
        out float opacity,
        out Color runtimeColorRgba)
    {
        colorMultiplier = Vector3.one;
        hdrPeak = 1f;
        opacity = 1f;
        runtimeColorRgba = Color.white;
        if (mat == null)
        {
            return false;
        }

        MotionSample sample = default;
        float lifetime = Mathf.Max(1e-4f, lifetimeSeconds);
        float ageNorm = Mathf.Clamp01(ageSeconds / lifetime);
        ComputeColor(mat, seed, startTime, ageSeconds, 0f, lifetime, ageNorm, ref sample);
        colorMultiplier = sample.ColorMultiplier;
        hdrPeak = sample.HdrPeak;
        opacity = sample.Opacity;
        runtimeColorRgba = sample.RuntimeColorRgba;
        return true;
    }

    private static Color ReadColorMulMin(Material mat)
    {
        if (mat.HasProperty(ColorMulMinId))
        {
            return mat.GetColor(ColorMulMinId);
        }

        if (mat.HasProperty(ColorMultMinId))
        {
            return mat.GetColor(ColorMultMinId);
        }

        return new Color(0.5f, 0.5f, 0.8f, 1f);
    }

    private static Color ReadColorMulMax(Material mat)
    {
        if (mat.HasProperty(ColorMulMaxId))
        {
            return mat.GetColor(ColorMulMaxId);
        }

        if (mat.HasProperty(ColorMultMaxId))
        {
            return mat.GetColor(ColorMultMaxId);
        }

        return new Color(0.7f, 0.7f, 1.0f, 1f);
    }

    // CPU mirror of L2Fx_SampleColorScale (L2FxEmitterSpawn.hlsl).
    private static Color SampleColorScaleFull(Material mat, float ageNorm)
    {
        int count = mat.HasProperty(ColorScaleCountId) ? Mathf.RoundToInt(mat.GetFloat(ColorScaleCountId)) : 0;
        if (count <= 0)
        {
            return Color.white;
        }

        float param = mat.HasProperty(ColorScaleParamId) ? mat.GetFloat(ColorScaleParamId) : 0f;
        float sp = Mathf.Repeat((param + 1f) * Mathf.Clamp01(ageNorm), 1f);

        Color c0 = mat.HasProperty(ColorKey0Id) ? mat.GetColor(ColorKey0Id) : Color.white;
        float t1 = mat.HasProperty(ColorKey1TimeId) ? mat.GetFloat(ColorKey1TimeId) : 1f;
        Color c1 = mat.HasProperty(ColorKey1Id) ? mat.GetColor(ColorKey1Id) : Color.white;
        float t2 = mat.HasProperty(ColorKey2TimeId) ? mat.GetFloat(ColorKey2TimeId) : 1f;
        Color c2 = mat.HasProperty(ColorKey2Id) ? mat.GetColor(ColorKey2Id) : Color.white;
        float t3 = mat.HasProperty(ColorKey3TimeId) ? mat.GetFloat(ColorKey3TimeId) : 1f;
        Color c3 = mat.HasProperty(ColorKey3Id) ? mat.GetColor(ColorKey3Id) : Color.white;

        float[] times = { 0f, t1, t2, t3 };
        Color[] colors = { c0, c1, c2, c3 };
        count = Mathf.Clamp(count, 1, 4);

        int idx = 0;
        while (idx < count && times[idx] < sp)
        {
            idx++;
        }

        Color prevCol;
        float prevT;
        Color nextCol;
        float nextT;
        if (idx == 0)
        {
            prevCol = Color.white;
            prevT = 0f;
            nextCol = colors[0];
            nextT = times[0];
        }
        else
        {
            prevCol = colors[idx - 1];
            prevT = times[idx - 1];
            nextCol = idx < count ? colors[idx] : prevCol;
            nextT = idx < count ? times[idx] : prevT + 1e-4f;
        }

        float ts = (sp - prevT) / Mathf.Max(nextT - prevT, 1e-4f);
        return Color.Lerp(prevCol, nextCol, ts);
    }

    // Mirror of L2Fx_SpriteColorFade_Apply (L2FxSpriteColorFade.hlsl): subtract the same
    // normalized amount from all RGBA channels, clamp at 0 (fade-in and fade-out).
    private static Color ApplySubtractiveFade(Material mat, Color color, float age, float lifetime)
    {
        float hasLifetime = mat.HasProperty(HasLifetimeId) ? mat.GetFloat(HasLifetimeId) : 1f;
        if (hasLifetime < 0.5f)
        {
            return color;
        }

        float lt = Mathf.Max(1e-4f, lifetime);
        if (age <= 0f || age >= lt)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        bool fadeIn = mat.HasProperty(FadeInId) && mat.GetFloat(FadeInId) >= 0.5f;
        float fadeInEnd = mat.HasProperty(FadeInEndTimeId) ? mat.GetFloat(FadeInEndTimeId) : 0.05f;
        if (fadeIn && fadeInEnd > 0f && age < fadeInEnd)
        {
            float fi = Mathf.Clamp01((fadeInEnd - age) / Mathf.Max(1e-4f, fadeInEnd));
            color = SubtractScalar(color, fi);
        }

        bool fadeOut = !mat.HasProperty(FadeoutId) || mat.GetFloat(FadeoutId) >= 0.5f;
        if (fadeOut)
        {
            float start = Mathf.Clamp(mat.HasProperty(FadeoutStartTimeId) ? mat.GetFloat(FadeoutStartTimeId) : 0.3f, 0f, lt);
            if (age > start)
            {
                float fo = Mathf.Clamp01((age - start) / Mathf.Max(1e-4f, lt - start));
                color = SubtractScalar(color, fo);
            }
        }

        return new Color(
            Mathf.Max(0f, color.r),
            Mathf.Max(0f, color.g),
            Mathf.Max(0f, color.b),
            Mathf.Max(0f, color.a));
    }

    private static Color SubtractScalar(Color c, float s)
    {
        return new Color(c.r - s, c.g - s, c.b - s, c.a - s);
    }

    public static Vector3 UnityWorldToUe(Vector3 worldUnity)
    {
        return new Vector3(
            worldUnity.x * UnityToUu,
            worldUnity.z * UnityToUu,
            worldUnity.y * UnityToUu);
    }

    public static Vector3 UnityObjectOffsetToUe(Vector3 offsetUnity, float unitScale)
    {
        float inv = unitScale > 0f ? 1f / unitScale : UnityToUu;
        return new Vector3(
            offsetUnity.x * inv,
            offsetUnity.z * inv,
            offsetUnity.y * inv);
    }

    private static Vector3 ComputeSpawnOffsetUnity(Material mat, float seed, float startTime, float unitScale)
    {
        Vector2 azimuth = mat.HasProperty(PolarAzimuthDegId)
            ? new Vector2(mat.GetVector(PolarAzimuthDegId).x, mat.GetVector(PolarAzimuthDegId).y)
            : Vector2.zero;
        Vector2 pitch = mat.HasProperty(PolarPitchDegId)
            ? new Vector2(mat.GetVector(PolarPitchDegId).x, mat.GetVector(PolarPitchDegId).y)
            : Vector2.zero;
        Vector2 radius = mat.HasProperty(PolarRadiusId)
            ? new Vector2(mat.GetVector(PolarRadiusId).x, mat.GetVector(PolarRadiusId).y)
            : Vector2.zero;

        float polarScale = mat.HasProperty(UcPolarRadiusScaleId) ? mat.GetFloat(UcPolarRadiusScaleId) : 1f;
        float offsetScale = mat.HasProperty(UcStartLocationOffsetScaleId) ? mat.GetFloat(UcStartLocationOffsetScaleId) : 1f;
        float rangeScale = mat.HasProperty(UcStartLocationRangeScaleId) ? mat.GetFloat(UcStartLocationRangeScaleId) : 1f;

        Vector3 offsetUe = mat.HasProperty(StartLocationOffsetId)
            ? mat.GetVector(StartLocationOffsetId)
            : Vector3.zero;
        offsetUe *= offsetScale;

        Vector3 posUe = SpawnRegionOffsetUe(
            azimuth,
            pitch,
            radius * polarScale,
            offsetUe,
            Vector3.zero,
            Vector3.zero,
            seed,
            startTime,
            rangeScale);

        return UeVectorToUnity(posUe) * unitScale;
    }

    private static Vector3 ComputeDisplacementUnity(
        Material mat,
        float seed,
        float startTime,
        float unitScale,
        float age)
    {
        float velScale = mat.HasProperty(UcVelocityScaleId) ? mat.GetFloat(UcVelocityScaleId) : 1f;
        Vector2 velZRange = mat.HasProperty(VelocityRangeZId)
            ? new Vector2(mat.GetVector(VelocityRangeZId).x, mat.GetVector(VelocityRangeZId).y)
            : Vector2.zero;
        velZRange *= velScale;
        float velZ = RandomRange(velZRange, seed, startTime, 107f);
        Vector3 velUe = new Vector3(0f, 0f, velZ);

        Vector3 accUe = mat.HasProperty(AccelerationId)
            ? mat.GetVector(AccelerationId)
            : Vector3.zero;
        float accScale = mat.HasProperty(UcAccelerationScaleId) ? mat.GetFloat(UcAccelerationScaleId) : 1f;
        accUe *= accScale;

        Vector2 lossZRange = mat.HasProperty(VelocityLossRangeZId)
            ? new Vector2(mat.GetVector(VelocityLossRangeZId).x, mat.GetVector(VelocityLossRangeZId).y)
            : Vector2.zero;
        float lossZ = RandomRange(lossZRange, seed, startTime, 109f);
        Vector3 lossUe = new Vector3(0f, 0f, lossZ);

        Vector3 vel = UeVectorToUnity(velUe) * unitScale;
        Vector3 acc = UeVectorToUnity(accUe) * unitScale;
        // VelocityLoss is a rate (1/s), NOT a distance -> no unitScale.
        Vector3 lossRate = UeVectorToUnity(lossUe);
        return DisplacementVelocityLossExp(vel, acc, lossRate, age);
    }

    private static Vector3 ComputeLogSizeUe(Material mat, float seed, float startTime, float ageNorm)
    {
        Vector2 rangeX = mat.HasProperty(SizeRangeXId)
            ? new Vector2(mat.GetVector(SizeRangeXId).x, mat.GetVector(SizeRangeXId).y)
            : new Vector2(1f, 1f);
        Vector2 rangeY = mat.HasProperty(SizeRangeYId)
            ? new Vector2(mat.GetVector(SizeRangeYId).x, mat.GetVector(SizeRangeYId).y)
            : new Vector2(1f, 1f);
        Vector2 rangeZ = mat.HasProperty(SizeRangeZId)
            ? new Vector2(mat.GetVector(SizeRangeZId).x, mat.GetVector(SizeRangeZId).y)
            : new Vector2(1f, 1f);
        bool uniform = mat.HasProperty(UniformSizeId) && mat.GetFloat(UniformSizeId) > 0.5f;

        Vector3 sizeUe = StartSizeUe(
            new Vector3(rangeX.x, rangeY.x, rangeZ.x),
            new Vector3(rangeX.y, rangeY.y, rangeZ.y),
            uniform,
            seed,
            startTime);

        float effectScale = mat.HasProperty(L2FxEffectScaleId) ? mat.GetFloat(L2FxEffectScaleId) : 1f;
        float spriteScale = mat.HasProperty(L2FxSpriteScaleId) ? mat.GetFloat(L2FxSpriteScaleId) : 1f;
        if (effectScale <= 0f)
        {
            effectScale = 1f;
        }

        if (spriteScale <= 0f)
        {
            spriteScale = 1f;
        }

        sizeUe *= effectScale * spriteScale;

        float rel = SampleUplineSizeScaleScalar(mat, ageNorm);
        return new Vector3(sizeUe.x * rel, sizeUe.y * rel, sizeUe.z * rel);
    }

    private static float SampleUplineSizeScaleScalar(Material mat, float ageNorm)
    {
        if (!mat.HasProperty(UseSizeScaleId) || mat.GetFloat(UseSizeScaleId) < 0.5f)
        {
            return 1f;
        }

        float t0 = mat.HasProperty(SizeScaleTime0Id) ? mat.GetFloat(SizeScaleTime0Id) : 0f;
        float v0 = mat.HasProperty(SizeScaleVal0Id) ? mat.GetFloat(SizeScaleVal0Id) : 1f;
        float t1 = mat.HasProperty(SizeScaleTime1Id) ? mat.GetFloat(SizeScaleTime1Id) : 1f;
        float v1 = mat.HasProperty(SizeScaleVal1Id) ? mat.GetFloat(SizeScaleVal1Id) : 1f;

        float sp = Frac(ageNorm);
        if (sp <= t1)
        {
            float denom = Mathf.Max(0.0001f, t1 - t0);
            return Mathf.Lerp(v0, v1, Mathf.Clamp01((sp - t0) / denom));
        }

        return v1;
    }

    private static Vector3 SpawnRegionOffsetUe(
        Vector2 azimuthDegMinMax,
        Vector2 polarFromPositiveZDegMinMax,
        Vector2 radiusMinMax,
        Vector3 startLocationOffsetUe,
        Vector3 startLocationRangeMinUe,
        Vector3 startLocationRangeMaxUe,
        float seed,
        float startTime,
        float rangeScale)
    {
        Vector3 posUe = SpawnOffsetPolarDegrees(
            azimuthDegMinMax,
            polarFromPositiveZDegMinMax,
            radiusMinMax,
            seed,
            startTime);

        posUe.x += RandomRange(
            new Vector2(startLocationRangeMinUe.x * rangeScale, startLocationRangeMaxUe.x * rangeScale),
            seed,
            startTime,
            83f);
        posUe.y += RandomRange(
            new Vector2(startLocationRangeMinUe.y * rangeScale, startLocationRangeMaxUe.y * rangeScale),
            seed,
            startTime,
            89f);
        posUe.z += RandomRange(
            new Vector2(startLocationRangeMinUe.z * rangeScale, startLocationRangeMaxUe.z * rangeScale),
            seed,
            startTime,
            97f);

        posUe += startLocationOffsetUe;
        return posUe;
    }

    private static Vector3 SpawnOffsetPolarDegrees(
        Vector2 azimuthDegMinMax,
        Vector2 polarFromPositiveZDegMinMax,
        Vector2 radiusMinMax,
        float seed,
        float startTime)
    {
        float thetaDeg = RandomRange(azimuthDegMinMax, seed, startTime, 71f);
        float phiDeg = RandomRange(polarFromPositiveZDegMinMax, seed, startTime, 73f);
        float radius = RandomRange(radiusMinMax, seed, startTime, 79f);
        return PolarCartesianUe(thetaDeg, phiDeg, radius);
    }

    private static Vector3 PolarCartesianUe(float thetaDeg, float phiDeg, float radius)
    {
        float theta = thetaDeg * DegToRad;
        float phi = phiDeg * DegToRad;
        float sinPhi = Mathf.Sin(phi);
        return new Vector3(
            radius * sinPhi * Mathf.Cos(theta),
            radius * sinPhi * Mathf.Sin(theta),
            radius * Mathf.Cos(phi));
    }

    private static Vector3 StartSizeUe(Vector3 sizeMinUe, Vector3 sizeMaxUe, bool uniformSize, float seed, float startTime)
    {
        if (uniformSize)
        {
            float s = RandomRange(new Vector2(sizeMinUe.x, sizeMaxUe.x), seed, startTime, 151f);
            return new Vector3(s, s, s);
        }

        return new Vector3(
            RandomRange(new Vector2(sizeMinUe.x, sizeMaxUe.x), seed, startTime, 151f),
            RandomRange(new Vector2(sizeMinUe.y, sizeMaxUe.y), seed, startTime, 157f),
            RandomRange(new Vector2(sizeMinUe.z, sizeMaxUe.z), seed, startTime, 163f));
    }

    private static Vector3 DisplacementLinearVelocityLoss(
        Vector3 velocity,
        Vector3 acceleration,
        Vector3 velocityLossPerSec,
        float ageSeconds)
    {
        float t = Mathf.Max(0f, ageSeconds);
        return velocity * t + 0.5f * acceleration * t * t - 0.5f * velocityLossPerSec * t * t;
    }

    // Exact UE velocity-loss integration, per axis: dv/dt = a - k*v (k = VelocityLoss, 1/s).
    // Acceleration is also damped, so speed converges to a/k (terminal) with no overshoot.
    //   x(t) = (a/k) t + (v0 - a/k) * (1 - exp(-k t)) / k ; k->0 => v0*t + 0.5*a*t^2
    // Mirrors L2Fx_DisplacementVelocityLossExp in L2FxMeshParticleMotion.hlsl.
    private static Vector3 DisplacementVelocityLossExp(
        Vector3 velocity,
        Vector3 acceleration,
        Vector3 lossPerSec,
        float ageSeconds)
    {
        float t = Mathf.Max(0f, ageSeconds);
        Vector3 res = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            float k = lossPerSec[i];
            if (k > 1e-4f)
            {
                float vTerm = acceleration[i] / k;
                res[i] = vTerm * t + (velocity[i] - vTerm) * (1f - Mathf.Exp(-k * t)) / k;
            }
            else
            {
                res[i] = velocity[i] * t + 0.5f * acceleration[i] * t * t;
            }
        }
        return res;
    }

    private static Vector3 UeVectorToUnity(Vector3 vUe)
    {
        return new Vector3(vUe.x, vUe.z, vUe.y);
    }

    private static float RandomRange(Vector2 minMax, float seed, float startTime, float salt)
    {
        float t = Hash11((seed * 17f) + (startTime * 31f) + salt);
        return Mathf.Lerp(minMax.x, minMax.y, t);
    }

    private static float Hash11(float n)
    {
        return Frac(Mathf.Sin(n) * 43758.5453123f);
    }

    private static float Frac(float x)
    {
        return x - Mathf.Floor(x);
    }
}
#endif
