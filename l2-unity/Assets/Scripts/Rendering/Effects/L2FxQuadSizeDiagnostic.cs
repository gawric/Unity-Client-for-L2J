using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// Reproduces calib sprite vertex-shader quad size in C# for debugging.
/// Matches: sizeM = sizeUU / 52.5 * K in object space.
/// </summary>
public static class L2FxQuadSizeDiagnostic
{
    private const float UuToMeters = 1f / 52.5f;
    private const float DefaultWorldCalibration = 1.8f;
    public const float LogIntervalSec = 0.25f;

    public struct QuadSizeSnapshot
    {
        public float startSizeMidUU;
        public float sizeMul;
        public float sizeUU;
        public float sizeMetersVertex;
        public float finalWidthM;
        public float finalHeightM;
        public float ageNorm;
        public float ageSec;
        public float lifetimeSec;
        public float initialDelaySec;
        public float scaleT;
        public float worldCalibrationK;
        public Vector3 lossyScale;
        public Vector3 quadSpan;
        public string sizeScalePath;
        public bool loopPreview;
        public int flipbookFrameA;
        public int flipbookFrameB;
        public float flipbookBlend;
    }

    public static bool ShouldTrace(string groupName, L2Particle owner, Transform groupTransform)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return false;
        }

        bool isTargetGroup =
            groupName.IndexOf("SpriteEmitter0", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            groupName.IndexOf("SpriteEmitter2", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (!isTargetGroup)
        {
            return false;
        }

        return ParticleSingleLifetimeDebug.ShouldTrace(groupName, owner, groupTransform);
    }

    public static QuadSizeSnapshot Compute(string groupName, Renderer renderer, float now, Material runtimeMat = null)
    {
        var snap = new QuadSizeSnapshot();
        if (renderer == null)
        {
            return snap;
        }

        Material mat = runtimeMat;
        if (mat == null)
        {
            Material[] mats = renderer.materials;
            mat = (mats != null && mats.Length > 0) ? mats[0] : null;
        }

        if (mat == null || !mat.HasProperty("_SizeRange"))
        {
            return snap;
        }

        Vector4 sizeRange = mat.GetVector("_SizeRange");
        snap.startSizeMidUU = (sizeRange.x + sizeRange.y) * 0.5f;
        snap.worldCalibrationK = mat.HasProperty("_L2FxWorldCalibration")
            ? Mathf.Max(mat.GetFloat("_L2FxWorldCalibration"), 1e-4f)
            : DefaultWorldCalibration;

        Transform quadTransform = renderer.transform;
        snap.lossyScale = quadTransform.lossyScale;
        snap.quadSpan = Vector3.one;
        MeshFilter mf = renderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            snap.quadSpan = mf.sharedMesh.bounds.size;
        }

        bool manualTestAge = mat.HasProperty("_TestSizeScaleAge");
        if (manualTestAge)
        {
            snap.sizeScalePath = "SE0_EvaluateDynamicSizeScale";
            snap.ageNorm = mat.GetFloat("_TestSizeScaleAge");
            snap.sizeMul = EvaluateDynamicSizeScaleSe0(mat, snap.ageNorm, out snap.scaleT);
        }
        else
        {
            snap.sizeScalePath = "L2Fx_SpriteSizeScale";
            snap.initialDelaySec = ResolveInitialDelay(mat);
            snap.lifetimeSec = ResolveLifetime(mat);
            snap.ageSec = ResolveAgeSeconds(now, mat, snap.initialDelaySec);
            snap.ageNorm = ResolveParticleAgeNorm(now, mat, snap.ageSec, snap.lifetimeSec);
            snap.sizeMul = SampleSpriteSizeScaleFromUniforms(mat, snap.ageNorm, out snap.scaleT);
        }

        if (mat.HasProperty("_TestDisableSizeScale") && mat.GetFloat("_TestDisableSizeScale") > 0.5f)
        {
            snap.sizeMul = 1f;
            snap.sizeScalePath = "TEST_SizeScaleDisabled";
        }

        snap.sizeUU = snap.startSizeMidUU * snap.sizeMul;
        float sizeInMeters = snap.sizeUU * UuToMeters;
        snap.sizeMetersVertex = sizeInMeters * snap.worldCalibrationK;
        float worldDiameterM = sizeInMeters * snap.worldCalibrationK;
        snap.finalWidthM = snap.quadSpan.x * worldDiameterM * Mathf.Abs(snap.lossyScale.x);
        snap.finalHeightM = snap.quadSpan.y * worldDiameterM * Mathf.Abs(snap.lossyScale.y);
        snap.loopPreview = mat.HasProperty("_LoopSizeScalePreview") &&
            mat.GetFloat("_LoopSizeScalePreview") > 0.5f;
        ResolveFlipbookFrames(mat, snap.ageNorm, out snap.flipbookFrameA, out snap.flipbookFrameB, out snap.flipbookBlend);
        return snap;
    }

    public static void Log(string groupName, Renderer renderer, float now, Material runtimeMat = null)
    {
        Material mat = runtimeMat;
        if (mat == null && renderer != null)
        {
            Material[] mats = renderer.materials;
            mat = (mats != null && mats.Length > 0) ? mats[0] : null;
        }

        if (mat == null)
        {
            return;
        }

        QuadSizeSnapshot s = Compute(groupName, renderer, now, mat);
        if (s.startSizeMidUU <= 0f)
        {
            return;
        }

        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : 0f;
        float seed = mat.HasProperty("_Seed") ? mat.GetFloat("_Seed") : 0f;
        float repeats = mat.HasProperty("_SizeScaleRepeats") ? mat.GetFloat("_SizeScaleRepeats") : 0f;

        Debug.Log(
            $"[L2FX_QUAD_SIZE] group='{groupName}' mat='{mat.name}' now={now:F3}s frame={Time.frameCount}\n" +
            $"  path={s.sizeScalePath}  ageNorm={s.ageNorm:F4}  scaleT=frac((1+repeats)*age)={s.scaleT:F4}  repeats={repeats:F2}\n" +
            $"  runtime: _StartTime={startTime:F3} _Seed={seed:F3} delay={s.initialDelaySec:F4}s lifetime={s.lifetimeSec:F4}s ageSec={s.ageSec:F4}s\n" +
            $"  StartSize midUU={s.startSizeMidUU:F4}  sizeMul={s.sizeMul:F4}  sizeUU={s.sizeUU:F4}\n" +
            $"  vertex OS: sizeM=sizeUU/52.5*K = {s.sizeMetersVertex:F6}m  K={s.worldCalibrationK:F3}\n" +
            $"  world: quadSpan=({s.quadSpan.x:F3},{s.quadSpan.y:F3}) lossyScale=({s.lossyScale.x:F4},{s.lossyScale.y:F4})\n" +
            $"  FINAL diameter(m): width={s.finalWidthM:F6}  height={s.finalHeightM:F6}\n" +
            $"  previewLoop={s.loopPreview} flipbook=frame{s.flipbookFrameA}->frame{s.flipbookFrameB} blend={s.flipbookBlend:F4}\n" +
            $"  formula: finalWidth = midUU * sizeMul / 52.5 * K * quadSpan.x * abs(lossyScale.x)");
    }

    private static void ResolveFlipbookFrames(
        Material mat,
        float normalizedAge,
        out int frameA,
        out int frameB,
        out float blend)
    {
        int start = mat.HasProperty("_SubdivisionStart") ? Mathf.RoundToInt(mat.GetFloat("_SubdivisionStart")) : 0;
        int end = mat.HasProperty("_SubdivisionEnd") ? Mathf.RoundToInt(mat.GetFloat("_SubdivisionEnd")) : start;
        int span = Mathf.Max(end - start, 1);
        float frame = start + Mathf.Clamp01(normalizedAge) * span;
        frameA = Mathf.Clamp(Mathf.FloorToInt(frame), start, end);
        frameB = Mathf.Clamp(frameA + 1, start, end);
        blend = Mathf.Clamp01(frame - Mathf.Floor(frame));
    }

    private static float ResolveInitialDelay(Material mat)
    {
        if (!mat.HasProperty("_InitialDelayRange"))
        {
            return 0f;
        }

        Vector4 delayRange = mat.GetVector("_InitialDelayRange");
        float seed = mat.HasProperty("_Seed") ? mat.GetFloat("_Seed") : 0f;
        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : 0f;
        return Mathf.Max(0f, RandomRange(new Vector2(delayRange.x, delayRange.y), seed, startTime, 3f));
    }

    private static float ResolveLifetime(Material mat)
    {
        if (!mat.HasProperty("_LifetimeRange"))
        {
            return 2f;
        }

        Vector4 lifetimeRange = mat.GetVector("_LifetimeRange");
        float seed = mat.HasProperty("_Seed") ? mat.GetFloat("_Seed") : 0f;
        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : 0f;
        return Mathf.Max(RandomRange(new Vector2(lifetimeRange.x, lifetimeRange.y), seed, startTime, 7f), 1e-4f);
    }

    private static float ResolveAgeSeconds(float now, Material mat, float delay)
    {
        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : 0f;
        if (startTime <= 0f)
        {
            return now;
        }

        return Mathf.Max(0f, now - startTime - delay);
    }

    private static float ResolveParticleAgeNorm(float now, Material mat, float ageSec, float lifetimeSec)
    {
        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : 0f;
        float age = ageSec;
        if (startTime <= 0f)
        {
            age = now;
        }

        bool loopPreview = mat.HasProperty("_LoopSizeScalePreview") && mat.GetFloat("_LoopSizeScalePreview") > 0.5f;
        if (loopPreview)
        {
            return Frac(age / lifetimeSec);
        }

        return Mathf.Clamp01(age / lifetimeSec);
    }

    private static float SampleSpriteSizeScaleFromUniforms(Material mat, float normalizedAge, out float scaleT)
    {
        const float sizeScaleParam = 1f;
        float repeats = mat.HasProperty("_SizeScaleRepeats") ? mat.GetFloat("_SizeScaleRepeats") : 0f;
        const int sizeScaleCount = 5;
        const bool implicitKeyZero = false;
        const bool useRegularSizeScale = false;

        scaleT = Frac((sizeScaleParam + repeats) * normalizedAge);

        BuildKeysFromUniforms(
            sizeScaleCount,
            implicitKeyZero,
            ReadKey(mat, 0),
            ReadKey(mat, 1),
            ReadKey(mat, 2),
            ReadKey(mat, 3),
            ReadKey(mat, 4),
            out float[] times,
            out float[] values);

        return SampleSizeScale(
            normalizedAge,
            sizeScaleParam,
            repeats,
            sizeScaleCount,
            times,
            values,
            useRegularSizeScale);
    }

    private static Vector2 ReadKey(Material mat, int index)
    {
        string prop = "_SizeKey" + index;
        if (!mat.HasProperty(prop))
        {
            return Vector2.zero;
        }

        Vector4 key = mat.GetVector(prop);
        return new Vector2(key.x, key.y);
    }

    private static float EvaluateDynamicSizeScaleSe0(Material mat, float progress, out float scaleT)
    {
        float repeats = mat.HasProperty("_SizeScaleRepeats") ? mat.GetFloat("_SizeScaleRepeats") : 0f;
        scaleT = Frac(progress * repeats);

        Vector2[] keys = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            keys[i] = ReadKey(mat, i);
        }

        if (keys[0].x > 0f && scaleT < keys[0].x)
        {
            return Mathf.Lerp(1f, keys[0].y, scaleT / Mathf.Max(keys[0].x, 1e-6f));
        }

        int idx = 0;
        while (idx < 4 && scaleT > keys[idx + 1].x)
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

        float u = (scaleT - t0) / (t1 - t0);
        return Mathf.Lerp(s0, s1, Mathf.Clamp01(u));
    }

    private static void BuildKeysFromUniforms(
        int sizeScaleCount,
        bool implicitKeyZero,
        Vector2 key0,
        Vector2 key1,
        Vector2 key2,
        Vector2 key3,
        Vector2 key4,
        out float[] times,
        out float[] values)
    {
        times = new float[8];
        values = new float[8];
        for (int i = 0; i < 8; i++)
        {
            times[i] = 999f;
            values[i] = 1f;
        }

        Vector2[] keys = { key0, key1, key2, key3, key4 };
        if (implicitKeyZero)
        {
            times[0] = 0f;
            values[0] = 0f;
            for (int i = 1; i < sizeScaleCount && i < keys.Length; i++)
            {
                times[i] = keys[i].x;
                values[i] = keys[i].y;
            }
        }
        else
        {
            for (int i = 0; i < sizeScaleCount && i < keys.Length; i++)
            {
                times[i] = keys[i].x;
                values[i] = keys[i].y;
            }
        }
    }

    private static float SampleSizeScale(
        float normalizedAge,
        float sizeScaleParam,
        float sizeScaleRepeats,
        int sizeScaleCount,
        float[] times,
        float[] values,
        bool useRegularSizeScale)
    {
        if (sizeScaleCount == 0)
        {
            return 1f;
        }

        float sp = Frac((sizeScaleParam + sizeScaleRepeats) * normalizedAge);

        int idx = 0;
        while (idx < sizeScaleCount && times[idx] < sp)
        {
            idx++;
        }

        float prevS;
        float prevT;
        float nextS;
        float nextT;

        if (idx == 0)
        {
            prevS = 1f;
            prevT = 0f;
            nextS = values[0];
            nextT = times[0];
        }
        else
        {
            prevS = values[idx - 1];
            prevT = times[idx - 1];
            nextS = idx < sizeScaleCount ? values[idx] : prevS;
            nextT = idx < sizeScaleCount ? times[idx] : prevT + 1e-4f;
        }

        if (Mathf.Abs(nextT - prevT) < 1e-4f)
        {
            return prevS;
        }

        float ts = (sp - prevT) / (nextT - prevT);
        return useRegularSizeScale ? Mathf.Lerp(prevS, nextS, ts) : Mathf.Lerp(prevS, nextS, ts);
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
