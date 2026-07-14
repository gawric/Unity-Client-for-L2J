#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes Unity_ParticleSnapshot.log in Lineage2 ParticleSnapshot.log format
/// for RenderDocExtractor emitter_log_analyze.py (shared parser with L2).
/// </summary>
public static class DocExtractorParticleSnapshotLogger
{
    public const string DefaultLogPath =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\Unity_ParticleSnapshot.log";

    public const float SampleIntervalSec = 0.1f;
    private const int SpriteEmitterLogIndex = 0;
    private const int UplineUcLayerIndex = 3;
    private const string UplineEffectName = "LineageEffect.e_u031_a";
    private const uint AppRandMultiplier = 214013u;
    private const uint AppRandIncrement = 2531011u;
    private const float AppFrandDivisor = 32767f;
    private const float SpinUcToUru = 65535f;

    public static bool Enabled = true;

    private static readonly object WriteLock = new object();
    private static readonly Dictionary<int, GroupSession> Sessions = new Dictionary<int, GroupSession>();

    private sealed class GroupSession
    {
        public bool Open;
        public int TickCounter;
        public float LastSampleTime = -999f;
        public bool MeshEmitter3PairVerifyLogged;
        public readonly Dictionary<int, Vector3> PrevLocLocalUe = new Dictionary<int, Vector3>();
        public readonly Dictionary<int, Vector3> PrevLocWorldUe = new Dictionary<int, Vector3>();
        public readonly Dictionary<int, MeshStartSpinSnapshot> MeshEmitter3StartSpinBySlot =
            new Dictionary<int, MeshStartSpinSnapshot>();
        public readonly Dictionary<int, SpriteSpinSnapshot> PrevSpriteSpinBySlot =
            new Dictionary<int, SpriteSpinSnapshot>();
    }

    private readonly struct SpriteSpinSnapshot
    {
        public readonly float SampleTime;
        public readonly float AngleUru;

        public SpriteSpinSnapshot(float sampleTime, float angleUru)
        {
            SampleTime = sampleTime;
            AngleUru = angleUru;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeLoad()
    {
        // Log file is created on first traced effect PlayPart (see BeginNewLogFile).
    }

    public static bool ShouldTrace(ParticleGroup group)
    {
        if (!Enabled || group == null)
        {
            return false;
        }

        return ParticleGroupLifetimeDebug.ShouldTraceUpline(group.name, group.OwnerParticle, group.transform)
            || ShouldTraceHealingPotionLayer(group.name, group.transform);
    }

    public static bool ShouldTrace(ParticleSingle single)
    {
        if (!Enabled || single == null)
        {
            return false;
        }

        return ShouldTraceHealingPotionLayer(single.name, single.transform);
    }

    private static bool ShouldTraceHealingPotionLayer(string layerName, Transform transform)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return false;
        }

        bool isTargetLayer =
            layerName.IndexOf("SpriteEmitter2", StringComparison.OrdinalIgnoreCase) >= 0 ||
            layerName.IndexOf("MeshEmitter3", StringComparison.OrdinalIgnoreCase) >= 0;
        if (!isTargetLayer)
        {
            return false;
        }

        Transform current = transform;
        for (int depth = 0; current != null && depth < 16; depth++, current = current.parent)
        {
            if (!string.IsNullOrEmpty(current.name) &&
                current.name.IndexOf("it_healing_potion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static void OnPlayPart(ParticleGroup group)
    {
        if (!ShouldTrace(group))
        {
            return;
        }

        if (!HasOpenSession())
        {
            BeginNewLogFile();
        }

        OpenEffectSession(group);
    }

    public static void OnPlayPart(ParticleSingle single)
    {
        if (!ShouldTrace(single))
        {
            return;
        }

        if (!HasOpenSession())
        {
            BeginNewLogFile();
        }
        OpenEffectSession(single);
    }

    private static void OpenEffectSession(ParticleGroup group)
    {
        GroupSession session = GetOrCreateSession(group);
        session.Open = true;
        session.TickCounter = 0;
        session.LastSampleTime = -999f;
        session.MeshEmitter3PairVerifyLogged = false;
        session.PrevLocLocalUe.Clear();
        session.PrevLocWorldUe.Clear();
        session.MeshEmitter3StartSpinBySlot.Clear();
        session.PrevSpriteSpinBySlot.Clear();

        L2Particle owner = group.OwnerParticle;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : group.transform;

        string casterHex = FormatPointer(caster);
        string aEmitterHex = FormatPointer(group);
        Vector3 emitterWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(group.transform.position);
        string effectName = ResolveEffectName(owner);

        var body = new StringBuilder(512);
        body.AppendLine("================================================================================");
        string emitterName = group.name.IndexOf("MeshEmitter3", StringComparison.OrdinalIgnoreCase) >= 0
            ? "ParticleGroup/MeshEmitter3"
            : "?";
        body.AppendLine(
            "EFFECT SESSION aEmitter=" + aEmitterHex +
            " aEmitterName=" + emitterName + " effect=" + effectName + " spawnKind=self");
        body.AppendLine("caster=" + casterHex + " sourceActor=" + casterHex);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "emitterWorld=({0:F2}, {1:F2}, {2:F2}){3}",
            emitterWorldUe.x,
            emitterWorldUe.y,
            emitterWorldUe.z,
            Environment.NewLine);
        body.AppendLine("started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        body.AppendLine("log=" + ResolveLogPath());
        body.AppendLine("================================================================================");
        Append(body.ToString());
    }

    private static void OpenEffectSession(ParticleSingle single)
    {
        GroupSession session = GetOrCreateSession(single);
        session.Open = true;
        session.TickCounter = 0;
        session.LastSampleTime = -999f;
        session.MeshEmitter3PairVerifyLogged = false;
        session.PrevLocLocalUe.Clear();
        session.PrevLocWorldUe.Clear();
        session.MeshEmitter3StartSpinBySlot.Clear();
        session.PrevSpriteSpinBySlot.Clear();

        Transform caster = single.transform;
        Vector3 emitterWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(single.transform.position);

        var body = new StringBuilder(512);
        body.AppendLine("================================================================================");
        body.AppendLine(
            "EFFECT SESSION aEmitter=" + FormatPointer(single) +
            " aEmitterName=ParticleSingle effect=UnityEffect.it_healing_potion spawnKind=self");
        body.AppendLine("caster=" + FormatPointer(caster) + " sourceActor=" + FormatPointer(caster));
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "emitterWorld=({0:F2}, {1:F2}, {2:F2}){3}",
            emitterWorldUe.x,
            emitterWorldUe.y,
            emitterWorldUe.z,
            Environment.NewLine);
        body.AppendLine("started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        body.AppendLine("log=" + ResolveLogPath());
        body.AppendLine("================================================================================");
        Append(body.ToString());
    }

    public static void OnParticleActivated(
        ParticleGroup group,
        int slot,
        Renderer renderer,
        float now,
        float shaderStartTime,
        float seed)
    {
        if (!ShouldTrace(group))
        {
            return;
        }

        GroupSession session = GetOrCreateSession(group);
        if (!session.Open)
        {
            OpenEffectSession(group);
        }

        WriteParticleSample(group, session, slot, renderer, now, shaderStartTime, seed, force: true);
    }

    public static void OnFixedUpdateTick(
        ParticleGroup group,
        float now,
        float[] spawnTimes,
        bool[] active,
        Renderer[] particles)
    {
        if (!ShouldTrace(group) || particles == null || active == null || spawnTimes == null)
        {
            return;
        }

        GroupSession session = GetOrCreateSession(group);
        if (!session.Open)
        {
            return;
        }

        if (session.LastSampleTime >= 0f && now - session.LastSampleTime < SampleIntervalSec)
        {
            return;
        }

        session.LastSampleTime = now;
        bool wroteAny = false;
        for (int slot = 0; slot < particles.Length; slot++)
        {
            if (!active[slot] || particles[slot] == null || !particles[slot].gameObject.activeSelf)
            {
                continue;
            }

            WriteParticleSample(
                group,
                session,
                slot,
                particles[slot],
                now,
                ReadShaderStartTime(particles[slot], spawnTimes[slot]),
                ReadSeed(particles[slot]),
                force: false);
            wroteAny = true;
        }

        if (!wroteAny)
        {
            session.Open = false;
        }
    }

    public static void OnParticleActivated(
        ParticleSingle single,
        Renderer renderer,
        float now,
        float shaderStartTime,
        float seed)
    {
        if (!ShouldTrace(single))
        {
            return;
        }

        GroupSession session = GetOrCreateSession(single);
        if (!session.Open)
        {
            OpenEffectSession(single);
        }

        WriteParticleSingleSample(single, session, renderer, now, shaderStartTime, seed, force: true);
    }

    public static void OnFixedUpdateTick(ParticleSingle single, float now, Renderer renderer)
    {
        if (!ShouldTrace(single) || renderer == null || !renderer.gameObject.activeSelf)
        {
            return;
        }

        GroupSession session = GetOrCreateSession(single);
        if (!session.Open || (session.LastSampleTime >= 0f && now - session.LastSampleTime < SampleIntervalSec))
        {
            return;
        }

        session.LastSampleTime = now;
        WriteParticleSingleSample(
            single,
            session,
            renderer,
            now,
            ReadShaderStartTime(renderer, now),
            ReadSeed(renderer),
            force: false);
    }

    public static void OnSlotOff(ParticleSingle single)
    {
        if (!ShouldTrace(single))
        {
            return;
        }

        GroupSession session = GetOrCreateSession(single);
        session.PrevLocLocalUe.Remove(0);
        session.PrevLocWorldUe.Remove(0);
        session.PrevSpriteSpinBySlot.Remove(0);
    }

    public static void OnSlotOff(ParticleGroup group, int slot)
    {
        if (!ShouldTrace(group))
        {
            return;
        }

        GroupSession session = GetOrCreateSession(group);
        session.PrevLocLocalUe.Remove(slot);
        session.PrevLocWorldUe.Remove(slot);
        session.MeshEmitter3StartSpinBySlot.Remove(slot);
        session.PrevSpriteSpinBySlot.Remove(slot);
        session.MeshEmitter3PairVerifyLogged = false;
    }

    private static void WriteParticleSingleSample(
        ParticleSingle single,
        GroupSession session,
        Renderer renderer,
        float now,
        float shaderStartTime,
        float seed,
        bool force)
    {
        Material mat = renderer != null && renderer.materials != null && renderer.materials.Length > 0
            ? renderer.materials[0]
            : null;
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty("_StartSize"))
        {
            WriteMeshEmitter3Sample(single, session, renderer, mat, now, shaderStartTime, seed);
            return;
        }

        if (!mat.HasProperty("_SizeRange"))
        {
            return;
        }

        L2FxQuadSizeDiagnostic.QuadSizeSnapshot size = L2FxQuadSizeDiagnostic.Compute(
            single.name,
            renderer,
            now,
            mat);
        if (size.startSizeMidUU <= 0f || (!force && size.ageSec < 1e-4f))
        {
            return;
        }

        session.TickCounter += 1;
        Vector3 worldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(renderer.transform.position);
        Vector3 localUe = Vector3.zero;
        if (!session.PrevLocLocalUe.TryGetValue(0, out Vector3 oldLocalUe))
        {
            oldLocalUe = localUe;
        }
        if (!session.PrevLocWorldUe.TryGetValue(0, out Vector3 oldWorldUe))
        {
            oldWorldUe = worldUe;
        }

        var body = new StringBuilder(768);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "ParticleSingle SpriteEmitter2 Particle[0] Tick{0}{1}",
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(single) +
            " aEmitterName=ParticleSingle effect=UnityEffect.it_healing_potion spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=3 subLayerName=SpriteEmitter2");
        body.AppendLine("  caster=" + FormatPointer(single.transform) + " sourceActor=" + FormatPointer(single.transform));
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locLocal=(0.000, 0.000, 0.000){0}  oldLocal=({1:F3}, {2:F3}, {3:F3}){0}" +
            "  locWorld=({4:F2}, {5:F2}, {6:F2}){0}  oldWorld=({7:F2}, {8:F2}, {9:F2}){0}",
            Environment.NewLine,
            oldLocalUe.x, oldLocalUe.y, oldLocalUe.z,
            worldUe.x, worldUe.y, worldUe.z,
            oldWorldUe.x, oldWorldUe.y, oldWorldUe.z);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  startSizeUU={0:F4} sizeMul={1:F4} finalSizeUU={2:F4}{3}" +
            "  particleTime={4:F4} maxLifetime={5:F4} lifeRemain={6:F4} lifeNorm={7:F4}{3}" +
            "  shaderStartTime={8:F4} seed={9:F4}{3}",
            size.startSizeMidUU,
            size.sizeMul,
            size.sizeUU,
            Environment.NewLine,
            size.ageSec,
            size.lifetimeSec,
            Mathf.Max(0f, size.lifetimeSec - size.ageSec),
            size.ageNorm,
            shaderStartTime,
            seed);
        AppendSpriteEmitter2SpinSnapshot(
            body,
            session,
            0,
            now,
            shaderStartTime,
            seed,
            mat);
        body.AppendLine(
            "  note=CPU mirror: SpriteEmitter2 size + SpriteSpin; runtimeColorA8 requires a shader-specific color mirror.");

        session.PrevLocLocalUe[0] = localUe;
        session.PrevLocWorldUe[0] = worldUe;
        Append(body.ToString());
    }

    // CPU mirror of HealingPotionTaMeshEmitter3Calib.shader and
    // L2FxMeshColorFade.hlsl. It intentionally reports UE mesh scale units so
    // rows can be compared directly to L2 MeshParticle startSize/finalSize.
    private static void WriteMeshEmitter3Sample(
        ParticleSingle single,
        GroupSession session,
        Renderer renderer,
        Material mat,
        float now,
        float shaderStartTime,
        float seed)
    {
        Vector4 delayRange = mat.GetVector("_InitialDelayRange");
        Vector4 lifetimeRange = mat.GetVector("_LifetimeRange");
        float delay = RandomRange(delayRange.x, delayRange.y, seed, shaderStartTime, 3f);
        float lifetime = Mathf.Max(1e-4f, RandomRange(lifetimeRange.x, lifetimeRange.y, seed, shaderStartTime, 7f));
        float particleTime = Mathf.Max(0f, now - shaderStartTime - delay);
        bool loopPreview = mat.HasProperty("_LoopSizeScalePreview") && mat.GetFloat("_LoopSizeScalePreview") > 0.5f;
        float lifeNorm = loopPreview
            ? Mathf.Repeat(particleTime / lifetime, 1f)
            : Mathf.Clamp01(particleTime / lifetime);
        float shaderAge = lifeNorm * lifetime;

        float sizeMul = SampleScalarKeys(
            lifeNorm,
            mat.GetVector("_SizeKey0"),
            mat.GetVector("_SizeKey1"),
            mat.GetVector("_SizeKey2"),
            mat.GetVector("_SizeKey3"),
            mat.GetVector("_SizeKey4"));
        float startSize = mat.GetFloat("_StartSize");
        float finalSize = startSize * sizeMul;

        Color colorScale = SampleColorKeys(
            lifeNorm,
            mat.GetColor("_ColorKey0"),
            mat.GetColor("_ColorKey1"),
            mat.GetColor("_ColorKey2"),
            mat.GetColor("_ColorKey3"),
            mat.GetColor("_ColorKey4"),
            mat.GetColor("_ColorKey5"));
        float fade = 0f;
        if (mat.HasProperty("_FadeOut") && mat.GetFloat("_FadeOut") > 0.5f)
        {
            float fadeStart = Mathf.Clamp(mat.GetFloat("_FadeOutStartTime"), 0f, lifetime);
            fade = shaderAge > fadeStart
                ? Mathf.Clamp01((shaderAge - fadeStart) / Mathf.Max(1e-4f, lifetime - fadeStart))
                : 0f;
        }

        Color finalColor = new Color(
            Mathf.Max(0f, colorScale.r - fade),
            Mathf.Max(0f, colorScale.g - fade),
            Mathf.Max(0f, colorScale.b - fade),
            Mathf.Max(0f, colorScale.a - fade));
        MeshStartSpinSnapshot startSpin = ReadMeshStartSpinSnapshot(
            mat,
            renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                ? renderer.sharedMaterials[0]
                : null,
            slotIndex: 0,
            liveBaseState: 0u);

        session.TickCounter += 1;
        Vector3 locLocalUe = mat.GetVector("_StartLocationOffsetUU");
        Vector3 locWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(renderer.transform.position);

        var body = new StringBuilder(896);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "MeshEmitter3 ParticleSingle MeshParticle[0] Tick{0}{1}",
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(single) +
            " aEmitterName=ParticleSingle effect=UnityEffect.it_healing_potion spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=1 kind=Mesh name=MeshEmitter3 class=MeshEmitter");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locLocal@+0x00=({0:F4}, {1:F4}, {2:F4}){3}" +
            "  locWorld=({4:F2}, {5:F2}, {6:F2}){3}",
            locLocalUe.x, locLocalUe.y, locLocalUe.z,
            Environment.NewLine,
            locWorldUe.x, locWorldUe.y, locWorldUe.z);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  startSize@+0x24=({0:F4}, {0:F4}, {0:F4}){1}" +
            "  finalSize@+0x6C=({2:F4}, {2:F4}, {2:F4}){1}" +
            "  particleTime={3:F4} maxLifetime={4:F4} lifeNorm={5:F4}{1}" +
            "  colorMultiplier@+0x84=(1.0000, 1.0000, 1.0000) hdrPeak=1.0000{1}" +
            "  runtimeColorA8@+0xA8=({6}, {7}, {8}, {9}) note=CPU MeshColorFade mirror{1}",
            startSize,
            Environment.NewLine,
            finalSize,
            particleTime,
            lifetime,
            lifeNorm,
            ToFloorByte(finalColor.b),
            ToFloorByte(finalColor.g),
            ToFloorByte(finalColor.r),
            ToFloorByte(finalColor.a));
        AppendMeshStartSpinSnapshot(body, startSpin);
        Append(body.ToString());
        TryAppendMeshEmitter3PairVerification(session);
    }

    private static float SampleScalarKeys(float phase, Vector4 key0, Vector4 key1, Vector4 key2, Vector4 key3, Vector4 key4)
    {
        Vector4[] keys = { key0, key1, key2, key3, key4 };
        for (int i = 1; i < keys.Length; i++)
        {
            if (phase <= keys[i].x)
            {
                float u = Mathf.InverseLerp(keys[i - 1].x, keys[i].x, phase);
                return Mathf.Lerp(keys[i - 1].y, keys[i].y, u);
            }
        }

        return keys[keys.Length - 1].y;
    }

    private static Color SampleColorKeys(float phase, Color key0, Color key1, Color key2, Color key3, Color key4, Color key5)
    {
        Color[] colors = { key0, key1, key2, key3, key4, key5 };
        float[] times = { 0f, 0.15f, 0.303571f, 0.685714f, 0.925f, 1f };
        for (int i = 1; i < times.Length; i++)
        {
            if (phase <= times[i])
            {
                return Color.Lerp(colors[i - 1], colors[i], Mathf.InverseLerp(times[i - 1], times[i], phase));
            }
        }

        return colors[colors.Length - 1];
    }

    private static float RandomRange(float min, float max, float seed, float startTime, float salt)
    {
        float hash = Mathf.Repeat(Mathf.Sin((seed * 17f) + (startTime * 31f) + salt) * 43758.5453123f, 1f);
        return Mathf.Lerp(min, max, hash);
    }

    // CPU mirror of Decompile_Common/L2FxSpriteSpin.hlsl's exact appRand path.
    // The runtime state is Unity-owned, but all nine SpawnParticle draws after
    // that state are L2 appRand/appFrand/FRange operations.
    private static void AppendSpriteEmitter2SpinSnapshot(
        StringBuilder body,
        GroupSession session,
        int slot,
        float now,
        float shaderStartTime,
        float seed,
        Material mat)
    {
        int startRangeId = Shader.PropertyToID("_SpriteSpinStartRangeUc");
        int spsRangeId = Shader.PropertyToID("_SpriteSpinSpsRangeUc");
        int ccwOrCwId = Shader.PropertyToID("_SpriteSpinCcwOrCw");
        if (!mat.HasProperty(startRangeId) || !mat.HasProperty(spsRangeId))
        {
            body.AppendLine("  spriteSpin=unavailable note=missing SpriteSpin material properties");
            return;
        }

        Vector4 startRange = mat.GetVector(startRangeId);
        Vector4 spsRange = mat.GetVector(spsRangeId);
        Vector4 ccwOrCw = mat.HasProperty(ccwOrCwId)
            ? mat.GetVector(ccwOrCwId)
            : new Vector4(0f, 1f, 1f, 0f);
        uint stateBeforeStartSpin = L2MaterialPropertyCopier.ReadSpriteSpinRandState(mat);
        if (stateBeforeStartSpin == 0u)
        {
            body.AppendLine("  spriteSpin=unavailable note=missing appRand state on runtime material");
            return;
        }

        float delay = RandomRange(
            mat.GetVector("_InitialDelayRange").x,
            mat.GetVector("_InitialDelayRange").y,
            seed,
            shaderStartTime,
            3f);
        float age = shaderStartTime <= 0f
            ? now
            : Mathf.Max(0f, now - shaderStartTime - delay);
        uint state = stateBeforeStartSpin;
        float startSpinUc = AppRandFRangeVectorX(startRange.x, startRange.y, ref state);
        float spsUc = AppRandFRangeVectorX(spsRange.x, spsRange.y, ref state);
        if (AppRandFrand(ref state) < ccwOrCw.x)
        {
            spsUc *= -1f;
        }
        AppRandFrand(ref state);
        AppRandFrand(ref state);

        float startSpinSlot = startSpinUc * SpinUcToUru;
        float spsSlot = spsUc * SpinUcToUru;
        float roundedInput = spsSlot >= 0f
            ? spsSlot * age + startSpinSlot
            : startSpinSlot - spsSlot * age;
        float wrappedU16 = Mathf.Repeat(Mathf.Floor(roundedInput + 0.5f), 65536f);
        float angleUru = spsSlot >= 0f ? wrappedU16 : 65535f - wrappedU16;
        float angleDegrees = angleUru * (360f / 65536f);
        float expectedSpeedDegPerSec = spsSlot * (360f / 65536f);

        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spriteSpin appRandStateBeforeStartSpin=0x{0:X8} rangeStartUc=({1:F6},{2:F6}) rangeSpsUc=({3:F6},{4:F6}) ccwOrCw=({5:F0},{6:F0},{7:F0}){8}" +
            "  spriteSpin spawn startUc={9:F6} spsUc={10:F6} startSlot@+0x3C={11:F3} spsSlot@+0x30={12:F3}{8}" +
            "  spriteSpin FillVertexBuffer branch={13} ageSec={14:F6} appRoundInput={15:F3} angleURU={16:F0} angleDeg={17:F3}" +
            " expectedSpeedDegPerSec={18:F3}{8}",
            stateBeforeStartSpin,
            startRange.x,
            startRange.y,
            spsRange.x,
            spsRange.y,
            ccwOrCw.x,
            ccwOrCw.y,
            ccwOrCw.z,
            Environment.NewLine,
            startSpinUc,
            spsUc,
            startSpinSlot,
            spsSlot,
            spsSlot >= 0f ? "SPS>=0" : "SPS<0",
            age,
            roundedInput,
            angleUru,
            angleDegrees,
            expectedSpeedDegPerSec);

        if (session.PrevSpriteSpinBySlot.TryGetValue(slot, out SpriteSpinSnapshot previous))
        {
            float deltaTime = now - previous.SampleTime;
            float deltaUru = angleUru - previous.AngleUru;
            if (deltaUru > 32768f)
            {
                deltaUru -= 65536f;
            }
            else if (deltaUru < -32768f)
            {
                deltaUru += 65536f;
            }

            float observedSpeed = deltaTime > 1e-4f
                ? deltaUru * (360f / 65536f) / deltaTime
                : 0f;
            body.AppendFormat(
                CultureInfo.InvariantCulture,
                "  spriteSpin tickDeltaSec={0:F4} deltaURU={1:F0} observedSpeedDegPerSec={2:F3} speedErrorDegPerSec={3:F3}{4}",
                deltaTime,
                deltaUru,
                observedSpeed,
                observedSpeed - expectedSpeedDegPerSec,
                Environment.NewLine);
        }

        session.PrevSpriteSpinBySlot[slot] = new SpriteSpinSnapshot(now, angleUru);
    }

    private static uint AppRand(ref uint state)
    {
        state = unchecked(state * AppRandMultiplier + AppRandIncrement);
        return (state >> 16) & 0x7fffu;
    }

    private static float AppRandFrand(ref uint state)
    {
        return AppRand(ref state) / AppFrandDivisor;
    }

    private static float AppRandFRangeVectorX(float min, float max, ref uint state)
    {
        AppRandFrand(ref state); // Z
        AppRandFrand(ref state); // Y
        return AppRandFrand(ref state) * (min - max) + max; // X
    }

    private static void WriteParticleSample(
        ParticleGroup group,
        GroupSession session,
        int slot,
        Renderer renderer,
        float now,
        float shaderStartTime,
        float seed,
        bool force)
    {
        Material mat = renderer != null && renderer.materials != null && renderer.materials.Length > 0
            ? renderer.materials[0]
            : null;
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty("_StartSize"))
        {
            WriteMeshEmitter3Sample(group, session, slot, renderer, mat, now, shaderStartTime, seed, isSpawnEvent: force);
            return;
        }

        if (!DocExtractorParticleMotionSimulator.TryEvaluate(
                group.transform,
                mat,
                now,
                shaderStartTime,
                seed,
                out DocExtractorParticleMotionSimulator.MotionSample motion))
        {
            return;
        }

        if (!force && motion.ParticleTime < 1e-4f)
        {
            return;
        }

        if (!session.PrevLocLocalUe.TryGetValue(slot, out Vector3 oldLocalUe))
        {
            oldLocalUe = motion.LocLocalUe;
        }

        if (!session.PrevLocWorldUe.TryGetValue(slot, out Vector3 oldWorldUe))
        {
            oldWorldUe = motion.LocWorldUe;
        }

        session.TickCounter += 1;
        L2Particle owner = group.OwnerParticle;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : group.transform;

        string casterHex = FormatPointer(caster);
        string aEmitterHex = FormatPointer(group);
        string subEmitterHex = FormatPointer(group);
        string effectName = ResolveEffectName(owner);

        var body = new StringBuilder(640);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpriteEmitter[{0}] Particle[{1}] Tick{2}{3}",
            SpriteEmitterLogIndex,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + aEmitterHex + " aEmitterName=? effect=" + effectName + " spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + subEmitterHex +
            " layerIndex=" + UplineUcLayerIndex +
            " subLayerName=?");
        body.AppendLine("  caster=" + casterHex + " sourceActor=" + casterHex);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locLocal=({0:F3}, {1:F3}, {2:F3}){3}",
            motion.LocLocalUe.x,
            motion.LocLocalUe.y,
            motion.LocLocalUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  oldLocal=({0:F3}, {1:F3}, {2:F3}){3}",
            oldLocalUe.x,
            oldLocalUe.y,
            oldLocalUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locWorld=({0:F2}, {1:F2}, {2:F2}){3}",
            motion.LocWorldUe.x,
            motion.LocWorldUe.y,
            motion.LocWorldUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  oldWorld=({0:F2}, {1:F2}, {2:F2}){3}",
            oldWorldUe.x,
            oldWorldUe.y,
            oldWorldUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  size=({0:F2}, {1:F2}, {2:F2}){3}",
            motion.SizeUe.x,
            motion.SizeUe.y,
            motion.SizeUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  rotation=({0:F2}, {1:F2}, {2:F2}){3}",
            motion.RotationEuler.x,
            motion.RotationEuler.y,
            motion.RotationEuler.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  startSpin=({0:F2}, {1:F2}, {2:F2}){3}",
            motion.StartSpin.x,
            motion.StartSpin.y,
            motion.StartSpin.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spinsPerSec=({0:F2}, {1:F2}, {2:F2}){3}",
            motion.SpinsPerSec.x,
            motion.SpinsPerSec.y,
            motion.SpinsPerSec.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  revCenter=({0:F3}, {1:F3}, {2:F3}){3}",
            motion.RevCenter.x,
            motion.RevCenter.y,
            motion.RevCenter.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  revPerSec=({0:F3}, {1:F3}, {2:F3}){3}",
            motion.RevPerSec.x,
            motion.RevPerSec.y,
            motion.RevPerSec.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  particleTime={0:F4} maxLifetime={1:F4} lifeRemain={2:F4} lifeNorm={3:F4}{4}",
            motion.ParticleTime,
            motion.MaxLifetime,
            motion.LifeRemain,
            motion.AgeNorm,
            Environment.NewLine);
        // flags/hitCount/boneIndex are L2-engine internals; kept constant so the shared
        // parser format matches. alive=1 because we only log active slots.
        body.AppendLine("  flags=0x00000001 alive=1 hitCount=0 boneIndex=-1");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  colorMultiplier=({0:F4}, {1:F4}, {2:F4}) hdrPeak={3:F4}{4}",
            motion.ColorMultiplier.x,
            motion.ColorMultiplier.y,
            motion.ColorMultiplier.z,
            motion.HdrPeak,
            Environment.NewLine);
        // Additive One+One: Color_14 (+0xA0) BGR is 0 in L2; mirror that for format parity.
        body.AppendLine("  colorByteBgr@+0xA0=(0, 0, 0) note=Color_14 BGR only; A omitted (0 for additive)");
        // runtimeColorA8 (+0xA8) draw color, BGRA. Unity fade is MULTIPLICATIVE (tint*lifeAlpha),
        // whereas L2 is SUBTRACTIVE — comparing these rows shows the hue-evolution difference.
        int rB = ToByte(motion.RuntimeColorRgba.b);
        int rG = ToByte(motion.RuntimeColorRgba.g);
        int rR = ToByte(motion.RuntimeColorRgba.r);
        int rA = ToByte(motion.RuntimeColorRgba.a);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  runtimeColorA8@+0xA8=({0}, {1}, {2}, {3}) note=draw BGRA; A drives color fade",
            rB,
            rG,
            rR,
            rA);

        session.PrevLocLocalUe[slot] = motion.LocLocalUe;
        session.PrevLocWorldUe[slot] = motion.LocWorldUe;
        Append(body.ToString() + Environment.NewLine);
    }

    private static void WriteMeshEmitter3Sample(
        ParticleGroup group,
        GroupSession session,
        int slot,
        Renderer renderer,
        Material mat,
        float now,
        float shaderStartTime,
        float seed,
        bool isSpawnEvent)
    {
        Material sharedMat = renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
            ? renderer.sharedMaterials[0]
            : null;
        MeshStartSpinSnapshot startSpin = ReadMeshStartSpinSnapshot(
            mat,
            sharedMat,
            slot,
            group.MeshEmitter3AppRandBaseState);
        session.MeshEmitter3StartSpinBySlot[slot] = startSpin;

        if (isSpawnEvent)
        {
            AppendMeshEmitter3SpawnVerification(session, slot, seed, shaderStartTime, now, startSpin);
        }

        Vector4 delayRange = mat.GetVector("_InitialDelayRange");
        Vector4 lifetimeRange = mat.GetVector("_LifetimeRange");
        float delay = RandomRange(delayRange.x, delayRange.y, seed, shaderStartTime, 3f);
        float lifetime = Mathf.Max(1e-4f, RandomRange(lifetimeRange.x, lifetimeRange.y, seed, shaderStartTime, 7f));
        float particleTime = Mathf.Max(0f, now - shaderStartTime - delay);
        float lifeNorm = Mathf.Clamp01(particleTime / lifetime);

        float sizeMul = SampleScalarKeys(
            lifeNorm,
            mat.GetVector("_SizeKey0"),
            mat.GetVector("_SizeKey1"),
            mat.GetVector("_SizeKey2"),
            mat.GetVector("_SizeKey3"),
            mat.GetVector("_SizeKey4"));
        float startSize = mat.GetFloat("_StartSize");
        float finalSize = startSize * sizeMul;

        Color colorScale = SampleColorKeys(
            lifeNorm,
            mat.GetColor("_ColorKey0"),
            mat.GetColor("_ColorKey1"),
            mat.GetColor("_ColorKey2"),
            mat.GetColor("_ColorKey3"),
            mat.GetColor("_ColorKey4"),
            mat.GetColor("_ColorKey5"));
        float fade = mat.HasProperty("_FadeOut") && mat.GetFloat("_FadeOut") > 0.5f
            ? Mathf.Clamp01(particleTime / lifetime)
            : 0f;
        Color finalColor = new Color(
            Mathf.Max(0f, colorScale.r - fade),
            Mathf.Max(0f, colorScale.g - fade),
            Mathf.Max(0f, colorScale.b - fade),
            Mathf.Max(0f, colorScale.a - fade));

        session.TickCounter += 1;
        Vector3 locLocalUe = mat.GetVector("_StartLocationOffsetUU");
        Vector3 locWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(renderer.transform.position);
        var body = new StringBuilder(960);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "MeshEmitter3[{0}] MeshParticle[{1}] Tick{2}{3}",
            1,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(group) +
            " aEmitterName=ParticleGroup effect=UnityEffect.it_healing_potion spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=1 kind=Mesh name=MeshEmitter3 class=MeshEmitter");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locLocal@+0x00=({0:F4}, {1:F4}, {2:F4}){3}" +
            "  locWorld=({4:F2}, {5:F2}, {6:F2}){3}",
            locLocalUe.x, locLocalUe.y, locLocalUe.z,
            Environment.NewLine,
            locWorldUe.x, locWorldUe.y, locWorldUe.z);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  startSize@+0x24=({0:F4}, {0:F4}, {0:F4}){1}" +
            "  finalSize@+0x6C=({2:F4}, {2:F4}, {2:F4}){1}" +
            "  particleTime={3:F4} maxLifetime={4:F4} lifeNorm={5:F4}{1}" +
            "  colorMultiplier@+0x84=(1.0000, 1.0000, 1.0000) hdrPeak=1.0000{1}" +
            "  runtimeColorA8@+0xA8=({6}, {7}, {8}, {9}) note=CPU MeshColorFade mirror{1}",
            startSize,
            Environment.NewLine,
            finalSize,
            particleTime,
            lifetime,
            lifeNorm,
            ToFloorByte(finalColor.b),
            ToFloorByte(finalColor.g),
            ToFloorByte(finalColor.r),
            ToFloorByte(finalColor.a));
        AppendMeshStartSpinSnapshot(body, startSpin);
        Append(body.ToString());
        TryAppendMeshEmitter3PairVerification(session);
    }

    private readonly struct MeshStartSpinSnapshot
    {
        public readonly bool HasAppRand;
        public readonly int SlotIndex;
        public readonly uint SharedBaseState;
        public readonly uint RuntimeStateBeforeRoll;
        public readonly uint ExpectedStateBeforeRoll;
        public readonly bool RuntimeStateMatchesExpected;
        public readonly Vector3 YawPitchRollUru;
        public readonly int NonZeroAxisCount;

        public MeshStartSpinSnapshot(
            bool hasAppRand,
            int slotIndex,
            uint sharedBaseState,
            uint runtimeStateBeforeRoll,
            uint expectedStateBeforeRoll,
            Vector3 yawPitchRollUru)
        {
            HasAppRand = hasAppRand;
            SlotIndex = slotIndex;
            SharedBaseState = sharedBaseState;
            RuntimeStateBeforeRoll = runtimeStateBeforeRoll;
            ExpectedStateBeforeRoll = expectedStateBeforeRoll;
            RuntimeStateMatchesExpected = runtimeStateBeforeRoll == expectedStateBeforeRoll;
            YawPitchRollUru = yawPitchRollUru;
            NonZeroAxisCount =
                (Mathf.Abs(yawPitchRollUru.x) > 0.5f ? 1 : 0) +
                (Mathf.Abs(yawPitchRollUru.y) > 0.5f ? 1 : 0) +
                (Mathf.Abs(yawPitchRollUru.z) > 0.5f ? 1 : 0);
        }
    }

    // CPU mirror of L2 Core.dll:
    // state = state * 214013 + 2531011; appRand=(state>>16)&0x7fff;
    // appFrand=appRand/32767; FRangeVector::GetRand draws Z, Y, X.
    private static MeshStartSpinSnapshot ReadMeshStartSpinSnapshot(
        Material mat,
        Material sharedMat,
        int slotIndex,
        uint liveBaseState)
    {
        if (mat == null || !mat.HasProperty("_StartSpinRandStateBits"))
        {
            return new MeshStartSpinSnapshot(false, slotIndex, 0u, 0u, 0u, Vector3.zero);
        }

        uint runtimeStateBeforeRoll = L2MaterialPropertyCopier.ReadStartSpinRandState(mat);
        uint expectedStateBeforeRoll = liveBaseState != 0u
            ? L2MaterialPropertyCopier.ComputeMeshEmitter3StartSpinState(liveBaseState, slotIndex)
            : runtimeStateBeforeRoll;
        if (runtimeStateBeforeRoll == 0u && expectedStateBeforeRoll != 0u)
        {
            runtimeStateBeforeRoll = expectedStateBeforeRoll;
        }

        uint state = runtimeStateBeforeRoll;
        Vector4 yawRange = mat.GetVector("_StartSpinYawRangeUc");
        Vector4 pitchRange = mat.GetVector("_StartSpinPitchRangeUc");
        Vector4 rollRange = mat.GetVector("_StartSpinRollRangeUc");

        float roll = GetL2RangeRand(rollRange.x, rollRange.y, ref state);
        float pitch = GetL2RangeRand(pitchRange.x, pitchRange.y, ref state);
        float yaw = GetL2RangeRand(yawRange.x, yawRange.y, ref state);
        return new MeshStartSpinSnapshot(
            liveBaseState != 0u || runtimeStateBeforeRoll != 0u,
            slotIndex,
            liveBaseState,
            runtimeStateBeforeRoll,
            expectedStateBeforeRoll,
            new Vector3(yaw * SpinUcToUru, pitch * SpinUcToUru, roll * SpinUcToUru));
    }

    private static float GetL2RangeRand(float min, float max, ref uint state)
    {
        state = unchecked(state * AppRandMultiplier + AppRandIncrement);
        uint appRand = (state >> 16) & 0x7fffu;
        float appFrand = appRand / AppFrandDivisor;
        return appFrand * (min - max) + max;
    }

    private static void AppendMeshStartSpinSnapshot(StringBuilder body, MeshStartSpinSnapshot snapshot)
    {
        if (!snapshot.HasAppRand)
        {
            body.AppendLine(
                "  startSpinURU@+0x3C=unavailable note=missing _StartSpinRandStateBits on runtime material");
            return;
        }

        Vector3 spin = snapshot.YawPitchRollUru;
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  startSpinURU@+0x3C=({0:F6},{1:F6},{2:F6}) trunc=({3},{4},{5}){6}",
            spin.x,
            spin.y,
            spin.z,
            Mathf.FloorToInt(spin.x),
            Mathf.FloorToInt(spin.y),
            Mathf.FloorToInt(spin.z),
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  appRandStateBeforeRoll=0x{0:X8} expectedState=0x{1:X8} sharedBaseState=0x{2:X8} slot={3}{4}",
            snapshot.RuntimeStateBeforeRoll,
            snapshot.ExpectedStateBeforeRoll,
            snapshot.SharedBaseState,
            snapshot.SlotIndex,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  startSpinCheck=nonZeroAxes={0}/3 stateCopyOk={1} note=CPU L2FxAppRand mirror{2}",
            snapshot.NonZeroAxisCount,
            snapshot.RuntimeStateMatchesExpected ? "yes" : "no",
            Environment.NewLine);
    }

    private static void AppendMeshEmitter3SpawnVerification(
        GroupSession session,
        int slot,
        float seed,
        float shaderStartTime,
        float now,
        MeshStartSpinSnapshot snapshot)
    {
        var body = new StringBuilder(640);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpawnParticleBegin Unity MeshEmitter3 slot={0} spawnTime={1:F6} shaderStartTime={2:F6} seed={3:F6}{4}",
            slot,
            now,
            shaderStartTime,
            seed,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  slotAdvanceDraws={0}{1}",
            slot * L2MaterialPropertyCopier.MeshEmitter3SlotToSlotDrawCount,
            Environment.NewLine);
        AppendMeshStartSpinSnapshot(body, snapshot);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spawnSpinVerdict={0}{1}",
            BuildMeshEmitter3SpawnVerdict(snapshot),
            Environment.NewLine);
        body.AppendLine("SpawnParticleEnd Unity MeshEmitter3 slot=" + slot);
        Append(body.ToString());
    }

    private static string BuildMeshEmitter3SpawnVerdict(MeshStartSpinSnapshot snapshot)
    {
        if (!snapshot.HasAppRand || snapshot.RuntimeStateBeforeRoll == 0u)
        {
            return "FAIL missing-appRand-state — meshes will use identity spin";
        }

        if (!snapshot.RuntimeStateMatchesExpected)
        {
            return "FAIL runtime-state-mismatch — slot copy did not reach shader material";
        }

        if (snapshot.NonZeroAxisCount < 3)
        {
            return "WARN partial-axis-spin — one or more StartSpin axes are near zero";
        }

        return "PASS appRand-startSpin-ready";
    }

    private static void TryAppendMeshEmitter3PairVerification(GroupSession session)
    {
        if (session.MeshEmitter3PairVerifyLogged ||
            !session.MeshEmitter3StartSpinBySlot.TryGetValue(0, out MeshStartSpinSnapshot slot0) ||
            !session.MeshEmitter3StartSpinBySlot.TryGetValue(1, out MeshStartSpinSnapshot slot1))
        {
            return;
        }

        session.MeshEmitter3PairVerifyLogged = true;
        Vector3 delta = slot1.YawPitchRollUru - slot0.YawPitchRollUru;
        float maxAbsDelta = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
        bool statesEqual = slot0.RuntimeStateBeforeRoll == slot1.RuntimeStateBeforeRoll;
        bool uruEqual = maxAbsDelta < 0.5f;
        string verdict;
        if (!slot0.HasAppRand || !slot1.HasAppRand ||
            slot0.RuntimeStateBeforeRoll == 0u || slot1.RuntimeStateBeforeRoll == 0u)
        {
            verdict = "FAIL missing-appRand-state";
        }
        else if (statesEqual || uruEqual)
        {
            verdict = "FAIL identical-orientation — meshes overlap with same StartSpin";
        }
        else
        {
            verdict = "PASS distinct-random-orientation";
        }

        var body = new StringBuilder(768);
        body.AppendLine("MeshEmitter3StartSpinPairVerify");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  slot0 startSpinURU=({0:F2},{1:F2},{2:F2}) state=0x{3:X8}{4}",
            slot0.YawPitchRollUru.x,
            slot0.YawPitchRollUru.y,
            slot0.YawPitchRollUru.z,
            slot0.RuntimeStateBeforeRoll,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  slot1 startSpinURU=({0:F2},{1:F2},{2:F2}) state=0x{3:X8}{4}",
            slot1.YawPitchRollUru.x,
            slot1.YawPitchRollUru.y,
            slot1.YawPitchRollUru.z,
            slot1.RuntimeStateBeforeRoll,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  uruDelta=({0:F2},{1:F2},{2:F2}) maxAbsDelta={3:F2} statesEqual={4} uruEqual={5}{6}",
            delta.x,
            delta.y,
            delta.z,
            maxAbsDelta,
            statesEqual ? "yes" : "no",
            uruEqual ? "yes" : "no",
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  pairVerdict={0} note=expected two different StartSpin orientations for MaxParticles=2{1}",
            verdict,
            Environment.NewLine);
        Append(body.ToString());
    }

    private static int ToByte(float linear01)
    {
        int v = Mathf.RoundToInt(linear01 * 255f);
        return v < 0 ? 0 : (v > 255 ? 255 : v);
    }

    private static int ToFloorByte(float linear01)
    {
        int v = Mathf.FloorToInt(Mathf.Clamp01(linear01) * 255f);
        return v < 0 ? 0 : (v > 255 ? 255 : v);
    }

    private static float ReadShaderStartTime(Renderer renderer, float spawnedAt)
    {
        if (renderer != null && renderer.materials != null && renderer.materials.Length > 0)
        {
            Material mat = renderer.materials[0];
            if (mat != null && mat.HasProperty("_StartTime"))
            {
                return mat.GetFloat("_StartTime");
            }
        }

        return spawnedAt;
    }

    private static float ReadSeed(Renderer renderer)
    {
        if (renderer == null || renderer.materials == null || renderer.materials.Length == 0)
        {
            return 0f;
        }

        Material mat = renderer.materials[0];
        return mat != null && mat.HasProperty("_Seed") ? mat.GetFloat("_Seed") : 0f;
    }

    private static GroupSession GetOrCreateSession(ParticleGroup group)
    {
        int key = group.GetInstanceID();
        if (!Sessions.TryGetValue(key, out GroupSession session))
        {
            session = new GroupSession();
            Sessions[key] = session;
        }

        return session;
    }

    private static bool HasOpenSession()
    {
        foreach (GroupSession session in Sessions.Values)
        {
            if (session.Open)
            {
                return true;
            }
        }

        return false;
    }

    private static GroupSession GetOrCreateSession(ParticleSingle single)
    {
        int key = single.GetInstanceID();
        if (!Sessions.TryGetValue(key, out GroupSession session))
        {
            session = new GroupSession();
            Sessions[key] = session;
        }

        return session;
    }

    private static string ResolveEffectName(L2Particle owner)
    {
        if (owner == null || string.IsNullOrEmpty(owner.name))
        {
            return UplineEffectName;
        }

        if (owner.name.IndexOf("teleport", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return UplineEffectName;
        }

        return "UnityEffect." + owner.name;
    }

    private static string ResolveLogPath()
    {
        string env = Environment.GetEnvironmentVariable("UNITY_PARTICLE_SNAPSHOT_LOG");
        return string.IsNullOrWhiteSpace(env) ? DefaultLogPath : env.Trim();
    }

    private static string FormatPointer(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return "00000000";
        }

        return unchecked((uint)obj.GetInstanceID()).ToString("X8", CultureInfo.InvariantCulture);
    }

    private static string FormatPointer(Transform transform)
    {
        return transform == null ? "00000000" : FormatPointer((UnityEngine.Object)transform.gameObject);
    }

    private static void BeginNewLogFile()
    {
        if (!Enabled)
        {
            return;
        }

        string path = ResolveLogPath();
        lock (WriteLock)
        {
            Sessions.Clear();

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var body = new StringBuilder(320);
                body.AppendLine(
                    "Unity_ParticleSnapshot.log — new session (ParticleGroup and ParticleSingle hooks installed)");
                body.AppendLine(
                    "started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                body.AppendLine("log=" + path);
                body.AppendLine("================================================================================");
                body.AppendLine();

                File.WriteAllText(path, body.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DocExtractorParticleSnapshotLogger] Cannot reset log file: " + ex.Message);
            }
        }
    }

    private static void Append(string text)
    {
        if (!Enabled || string.IsNullOrEmpty(text))
        {
            return;
        }

        string path = ResolveLogPath();
        lock (WriteLock)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.AppendAllText(path, text, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DocExtractorParticleSnapshotLogger] Write failed: " + ex.Message);
            }
        }
    }
}
#endif
