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
    private const int SpriteEmitter0LogIndex = 2;
    public const int SpriteEmitter0SlotToSlotDrawCount =
        DocExtractorSpriteEmitter0MotionSimulator.SlotToSlotDrawCount;
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
        public readonly Dictionary<int, float> PrevSampleTimeBySlot = new Dictionary<int, float>();
        public float Se0TickHorizMin = float.PositiveInfinity;
        public float Se0TickHorizMax = float.NegativeInfinity;
        public float Se0TickZMin = float.PositiveInfinity;
        public float Se0TickZMax = float.NegativeInfinity;
        public int Se0TickSampleCount;
        public float Se0TickParticleTime;
        public float Se0TickWorldK = 1.8f;
        public bool Se0L2MotionReplayDiagnosticLogged;
        public bool WaveAxisDiagLogged;
        public int WaveBurstIndex;
        public int WaveSpawnSlotsThisBurst;
        public readonly List<float> WaveBurstLocZ = new List<float>(5);
        public readonly List<float> WaveBurstVelZ = new List<float>(5);
        public readonly List<string> WaveBurstGapSummaries = new List<string>(10);
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
            layerName.IndexOf("SpriteEmitter0", StringComparison.OrdinalIgnoreCase) >= 0 ||
            layerName.IndexOf("SpriteEmitter2", StringComparison.OrdinalIgnoreCase) >= 0 ||
            layerName.IndexOf("SpriteEmitter7", StringComparison.OrdinalIgnoreCase) >= 0 ||
            layerName.IndexOf("MeshEmitter0", StringComparison.OrdinalIgnoreCase) >= 0 ||
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
        session.PrevSampleTimeBySlot.Clear();
        session.Se0TickHorizMin = float.PositiveInfinity;
        session.Se0TickHorizMax = float.NegativeInfinity;
        session.Se0TickZMin = float.PositiveInfinity;
        session.Se0TickZMax = float.NegativeInfinity;
        session.Se0TickSampleCount = 0;
        session.Se0TickParticleTime = 0f;
        session.Se0L2MotionReplayDiagnosticLogged = false;
        session.WaveAxisDiagLogged = false;
        session.WaveBurstIndex += 1;
        session.WaveSpawnSlotsThisBurst = 0;
        session.WaveBurstLocZ.Clear();
        session.WaveBurstVelZ.Clear();

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
        string emitterName = ResolveHealingPotionEmitterName(group.name);
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
        session.PrevSampleTimeBySlot.Clear();

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
        session.Se0TickHorizMin = float.PositiveInfinity;
        session.Se0TickHorizMax = float.NegativeInfinity;
        session.Se0TickZMin = float.PositiveInfinity;
        session.Se0TickZMax = float.NegativeInfinity;
        session.Se0TickSampleCount = 0;
        session.Se0TickParticleTime = 0f;
        session.Se0TickWorldK = 1.8f;
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
        else if (group.name.IndexOf("SpriteEmitter0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                 session.Se0TickSampleCount > 0)
        {
            AppendSpriteEmitter0GroupSpreadSummary(session);
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
        session.PrevSampleTimeBySlot.Remove(slot);
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

        if (mat.HasProperty("_StartSizeXY") && mat.HasProperty("_StartVelocityZRangeUU"))
        {
            WriteMeshEmitter0WaveSample(
                group, session, slot, renderer, mat, now, shaderStartTime, seed, isSpawnEvent: force);
            return;
        }

        if (mat.HasProperty("_StartSize"))
        {
            WriteMeshEmitter3Sample(group, session, slot, renderer, mat, now, shaderStartTime, seed, isSpawnEvent: force);
            return;
        }

        // Kirakira SE7 shares appRand props with SE0 calib but needs ColorScale A8 logging.
        if (IsHealingPotionSpriteEmitter7(group, mat))
        {
            WriteSpriteEmitter7KirakiraSample(group, session, slot, renderer, mat, now, shaderStartTime, seed, force);
            return;
        }

        if (DocExtractorSpriteEmitter0MotionSimulator.IsSpriteEmitter0Material(mat))
        {
            WriteSpriteEmitter0Sample(group, session, slot, renderer, mat, now, shaderStartTime, seed, force);
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
        bool isKirakiraSe7 =
            !string.IsNullOrEmpty(group.name) &&
            group.name.IndexOf("SpriteEmitter7", StringComparison.OrdinalIgnoreCase) >= 0;
        int spriteLogIndex = isKirakiraSe7 ? 7 : SpriteEmitterLogIndex;
        string subLayerName = isKirakiraSe7 ? "SpriteEmitter7" : "?";
        int layerIndex = isKirakiraSe7 ? 7 : UplineUcLayerIndex;

        var body = new StringBuilder(640);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpriteEmitter[{0}] Particle[{1}] Tick{2}{3}",
            spriteLogIndex,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + aEmitterHex + " aEmitterName=? effect=" + effectName + " spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + subEmitterHex +
            " layerIndex=" + layerIndex +
            " subLayerName=" + subLayerName);
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
            "  colorMultiplier=({0:F4}, {1:F4}, {2:F4}) hdrPeak={3:F4} opacity={4:F4}{5}",
            motion.ColorMultiplier.x,
            motion.ColorMultiplier.y,
            motion.ColorMultiplier.z,
            motion.HdrPeak,
            motion.Opacity,
            Environment.NewLine);
        // Brighten / additive: Color_14 (+0xA0) BGR is 0 in L2; mirror that for format parity.
        body.AppendLine("  colorByteBgr@+0xA0=(0, 0, 0) note=Color_14 BGR only; A omitted (0 for additive)");
        // runtimeColorA8 (+0xA8): ColorScale*ColorMul - subtractive Fade (matches L2 A8; Opacity not in A8).
        int rB = ToByte(motion.RuntimeColorRgba.b);
        int rG = ToByte(motion.RuntimeColorRgba.g);
        int rR = ToByte(motion.RuntimeColorRgba.r);
        int rA = ToByte(motion.RuntimeColorRgba.a);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  runtimeColorA8@+0xA8=({0}, {1}, {2}, {3}) note=CPU ColorScale*Mul-Fade BGRA; Opacity={4:F2} not in A8; Brighten RGB ignores A",
            rB,
            rG,
            rR,
            rA,
            motion.Opacity);

        session.PrevLocLocalUe[slot] = motion.LocLocalUe;
        session.PrevLocWorldUe[slot] = motion.LocWorldUe;
        Append(body.ToString() + Environment.NewLine);
    }

    private static void WriteSpriteEmitter0Sample(
        ParticleGroup group,
        GroupSession session,
        int slot,
        Renderer renderer,
        Material mat,
        float now,
        float shaderStartTime,
        float seed,
        bool force)
    {
        if (!DocExtractorSpriteEmitter0MotionSimulator.TryEvaluate(
                group.transform,
                mat,
                now,
                shaderStartTime,
                out DocExtractorSpriteEmitter0MotionSimulator.MotionSample motion))
        {
            return;
        }

        if (!force && motion.ParticleTime < 1e-4f)
        {
            return;
        }

        if (force)
        {
            AppendSpriteEmitter0SpawnVerification(session, slot, now, shaderStartTime, seed, motion.Spawn);
            AppendSpriteEmitter0L2MotionReplayDiagnostic(
                group.transform,
                session,
                slot,
                mat,
                shaderStartTime);
            session.PrevLocLocalUe[slot] = motion.LocLocalUe;
            session.PrevLocWorldUe[slot] = motion.LocWorldUe;
            session.PrevSampleTimeBySlot[slot] = now;
            if (motion.ParticleTime < 1e-4f)
            {
                return;
            }
        }

        if (!session.PrevLocLocalUe.TryGetValue(slot, out Vector3 oldLocalUe))
        {
            oldLocalUe = motion.LocLocalUe;
        }

        if (!session.PrevLocWorldUe.TryGetValue(slot, out Vector3 oldWorldUe))
        {
            oldWorldUe = motion.LocWorldUe;
        }

        Vector3 rendererPivotWorldUe = renderer != null
            ? DocExtractorParticleMotionSimulator.UnityWorldToUe(renderer.transform.position)
            : motion.LocWorldUe;

        float observedSpeedUePerSec = 0f;
        float expectedSpeedUePerSec = motion.VelocityNowUe.magnitude;
        float tickDeltaSec = 0f;
        Vector3 deltaLocalUe = motion.LocLocalUe - oldLocalUe;
        if (session.PrevSampleTimeBySlot.TryGetValue(slot, out float previousSampleTime))
        {
            tickDeltaSec = now - previousSampleTime;
            if (tickDeltaSec > 1e-4f)
            {
                observedSpeedUePerSec = deltaLocalUe.magnitude / tickDeltaSec;
            }
        }

        session.TickCounter += 1;
        L2Particle owner = group.OwnerParticle;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : group.transform;

        var body = new StringBuilder(1024);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpriteEmitter[{0}] Particle[{1}] Tick{2}{3}",
            SpriteEmitter0LogIndex,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(group) +
            " aEmitterName=ParticleGroup/SpriteEmitter0 effect=UnityEffect.it_healing_potion spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=2 subLayerName=SpriteEmitter0");
        body.AppendLine("  caster=" + FormatPointer(caster) + " sourceActor=" + FormatPointer(caster));
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
            "  rendererPivotWorld=({0:F2}, {1:F2}, {2:F2}) note=Transform pivot only; SE0 visual center uses locWorld from shader{3}",
            rendererPivotWorldUe.x,
            rendererPivotWorldUe.y,
            rendererPivotWorldUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  deltaLocal=({0:F3}, {1:F3}, {2:F3}) deltaMag={3:F3}{4}",
            deltaLocalUe.x,
            deltaLocalUe.y,
            deltaLocalUe.z,
            deltaLocalUe.magnitude,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spawnPositionUU=({0:F3}, {1:F3}, {2:F3}) polarOffsetUU=({3:F3}, {4:F3}, {5:F3}){6}",
            motion.Spawn.SpawnPositionUe.x,
            motion.Spawn.SpawnPositionUe.y,
            motion.Spawn.SpawnPositionUe.z,
            motion.Spawn.PolarOffsetUe.x,
            motion.Spawn.PolarOffsetUe.y,
            motion.Spawn.PolarOffsetUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  rawVelocityUU=({0:F3}, {1:F3}, {2:F3}) velocityBeforePtvdUU=({3:F3}, {4:F3}, {5:F3}){6}",
            motion.Spawn.RawVelocityUe.x,
            motion.Spawn.RawVelocityUe.y,
            motion.Spawn.RawVelocityUe.z,
            motion.Spawn.VelocityBeforePtvdUe.x,
            motion.Spawn.VelocityBeforePtvdUe.y,
            motion.Spawn.VelocityBeforePtvdUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  ptvdDirectionUU=({0:F6}, {1:F6}, {2:F6}) velocityAfterPtvdUU=({3:F3}, {4:F3}, {5:F3}) velocityNowUU=({6:F3}, {7:F3}, {8:F3}){9}",
            motion.Spawn.PtvdDirectionUe.x,
            motion.Spawn.PtvdDirectionUe.y,
            motion.Spawn.PtvdDirectionUe.z,
            motion.Spawn.VelocityAfterPtvdUe.x,
            motion.Spawn.VelocityAfterPtvdUe.y,
            motion.Spawn.VelocityAfterPtvdUe.z,
            motion.VelocityNowUe.x,
            motion.VelocityNowUe.y,
            motion.VelocityNowUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  size=({0:F2}, {1:F2}, {2:F2}) spawnSizeUU={3:F4}{4}",
            motion.SizeUe.x,
            motion.SizeUe.y,
            motion.SizeUe.z,
            motion.Spawn.SpawnSizeUU,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  particleTime={0:F4} maxLifetime={1:F4} lifeRemain={2:F4} lifeNorm={3:F4}{4}",
            motion.ParticleTime,
            motion.MaxLifetime,
            motion.LifeRemain,
            motion.AgeNorm,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  motionTickDeltaSec={0:F4} observedSpeedUU={1:F3} expectedSpeedUU={2:F3} speedErrorUU={3:F3}{4}",
            tickDeltaSec,
            observedSpeedUePerSec,
            expectedSpeedUePerSec,
            observedSpeedUePerSec - expectedSpeedUePerSec,
            Environment.NewLine);

        float worldK = mat.HasProperty("_L2FxWorldCalibration") ? mat.GetFloat("_L2FxWorldCalibration") : 1.8f;
        if (session.Se0TickSampleCount == 0)
        {
            session.Se0TickWorldK = worldK;
        }

        float horizRadiusUe = Mathf.Sqrt(
            motion.LocLocalUe.x * motion.LocLocalUe.x +
            motion.LocLocalUe.y * motion.LocLocalUe.y);
        Vector3 dispFromSpawnVecUe = motion.LocLocalUe - motion.Spawn.SpawnPositionUe;
        float dispFromSpawnUe = dispFromSpawnVecUe.magnitude;
        Vector3 locMeters = DocExtractorSpriteEmitter0MotionSimulator.UcPositionToUnityMeters(
            motion.LocLocalUe,
            worldK);
        float spawnHorizUe = Mathf.Sqrt(
            motion.Spawn.SpawnPositionUe.x * motion.Spawn.SpawnPositionUe.x +
            motion.Spawn.SpawnPositionUe.y * motion.Spawn.SpawnPositionUe.y);

        session.Se0TickHorizMin = Mathf.Min(session.Se0TickHorizMin, horizRadiusUe);
        session.Se0TickHorizMax = Mathf.Max(session.Se0TickHorizMax, horizRadiusUe);
        session.Se0TickZMin = Mathf.Min(session.Se0TickZMin, motion.LocLocalUe.z);
        session.Se0TickZMax = Mathf.Max(session.Se0TickZMax, motion.LocLocalUe.z);
        session.Se0TickSampleCount += 1;
        session.Se0TickParticleTime = motion.ParticleTime;

        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  se0Spread horizRadiusUU={0:F3} spawnHorizUU={1:F3} heightUU={2:F3} dispFromSpawnUU={3:F3}{4}",
            horizRadiusUe,
            spawnHorizUe,
            motion.LocLocalUe.z,
            dispFromSpawnUe,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  se0SpreadMeters loc=({0:F4}, {1:F4}, {2:F4}) worldK={3:F2} note=UU/52.5*K; compare with L2 locLocal in UU first{4}",
            locMeters.x,
            locMeters.y,
            locMeters.z,
            worldK,
            Environment.NewLine);
        body.AppendLine(
            "  se0SpreadRef L2@t~0.057 horizRadiusUU=0.5..1.3 heightUU=6.9..9.9 spawnHorizUU~2.4 polarRadius");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  appRandStateBeforeSpawn=0x{0:X8} shaderStartTime={1:F4} seed={2:F4}{3}",
            motion.Spawn.AppRandStateBeforeSpawn,
            shaderStartTime,
            seed,
            Environment.NewLine);
        AppendSpriteEmitter2SpinSnapshot(
            body,
            session,
            slot,
            now,
            shaderStartTime,
            seed,
            mat);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  motionVerdict={0} note=compare locLocal/locWorld ticks with L2 ParticleSnapshot.log; speed check uses CPU mirror only{1}",
            BuildSpriteEmitter0MotionVerdict(
                motion.Spawn.AppRandStateBeforeSpawn,
                motion.ParticleTime,
                tickDeltaSec,
                observedSpeedUePerSec - expectedSpeedUePerSec),
            Environment.NewLine);

        session.PrevLocLocalUe[slot] = motion.LocLocalUe;
        session.PrevLocWorldUe[slot] = motion.LocWorldUe;
        session.PrevSampleTimeBySlot[slot] = now;
        Append(body.ToString());
    }

    private static bool IsHealingPotionSpriteEmitter7(ParticleGroup group, Material mat)
    {
        if (group != null &&
            !string.IsNullOrEmpty(group.name) &&
            group.name.IndexOf("SpriteEmitter7", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return DocExtractorSpriteEmitter0MotionSimulator.IsKirakiraSpriteEmitter7Material(mat);
    }

    private static void WriteSpriteEmitter7KirakiraSample(
        ParticleGroup group,
        GroupSession session,
        int slot,
        Renderer renderer,
        Material mat,
        float now,
        float shaderStartTime,
        float seed,
        bool force)
    {
        if (!DocExtractorSpriteEmitter0MotionSimulator.TryEvaluate(
                group.transform,
                mat,
                now,
                shaderStartTime,
                out DocExtractorSpriteEmitter0MotionSimulator.MotionSample motion))
        {
            return;
        }

        if (!force && motion.ParticleTime < 1e-4f)
        {
            return;
        }

        if (force)
        {
            AppendSpriteEmitter7SpawnVerification(session, slot, now, shaderStartTime, seed, motion.Spawn);
            session.PrevLocLocalUe[slot] = motion.LocLocalUe;
            session.PrevLocWorldUe[slot] = motion.LocWorldUe;
            session.PrevSampleTimeBySlot[slot] = now;
        }

        if (!session.PrevLocLocalUe.TryGetValue(slot, out Vector3 oldLocalUe))
        {
            oldLocalUe = motion.LocLocalUe;
        }

        if (!session.PrevLocWorldUe.TryGetValue(slot, out Vector3 oldWorldUe))
        {
            oldWorldUe = motion.LocWorldUe;
        }

        float tickDeltaSec = 0f;
        if (session.PrevSampleTimeBySlot.TryGetValue(slot, out float previousSampleTime))
        {
            tickDeltaSec = now - previousSampleTime;
        }

        DocExtractorParticleMotionSimulator.TryEvaluateColor(
            mat,
            seed,
            shaderStartTime,
            motion.ParticleTime,
            motion.MaxLifetime,
            out Vector3 colorMul,
            out float hdrPeak,
            out float opacity,
            out Color runtimeColor);

        session.TickCounter += 1;
        L2Particle owner = group.OwnerParticle;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : group.transform;
        string effectName = ResolveEffectName(owner);

        var body = new StringBuilder(896);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpriteEmitter[{0}] Particle[{1}] Tick{2}{3}",
            7,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(group) +
            " aEmitterName=ParticleGroup/SpriteEmitter7 effect=" + effectName + " spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=7 subLayerName=SpriteEmitter7");
        body.AppendLine("  caster=" + FormatPointer(caster) + " sourceActor=" + FormatPointer(caster));
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
            "  size=({0:F2}, {1:F2}, {2:F2}) spawnSizeUU={3:F4}{4}",
            motion.SizeUe.x,
            motion.SizeUe.y,
            motion.SizeUe.z,
            motion.Spawn.SpawnSizeUU,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  particleTime={0:F4} maxLifetime={1:F4} lifeRemain={2:F4} lifeNorm={3:F4}{4}",
            motion.ParticleTime,
            motion.MaxLifetime,
            motion.LifeRemain,
            motion.AgeNorm,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  colorMultiplier=({0:F4}, {1:F4}, {2:F4}) hdrPeak={3:F4} opacity={4:F4}{5}",
            colorMul.x,
            colorMul.y,
            colorMul.z,
            hdrPeak,
            opacity,
            Environment.NewLine);
        body.AppendLine("  colorByteBgr@+0xA0=(0, 0, 0) note=Color_14 BGR only; A omitted (Brighten)");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  runtimeColorA8@+0xA8=({0}, {1}, {2}, {3}) note=CPU ColorScale*Mul-Fade BGRA; Opacity not in A8; Brighten RGB ignores A{4}",
            ToByte(runtimeColor.b),
            ToByte(runtimeColor.g),
            ToByte(runtimeColor.r),
            ToByte(runtimeColor.a),
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  appRandStateBeforeSpawn=0x{0:X8} shaderStartTime={1:F4} seed={2:F4} tickDeltaSec={3:F4}{4}",
            motion.Spawn.AppRandStateBeforeSpawn,
            shaderStartTime,
            seed,
            tickDeltaSec,
            Environment.NewLine);

        session.PrevLocLocalUe[slot] = motion.LocLocalUe;
        session.PrevLocWorldUe[slot] = motion.LocWorldUe;
        session.PrevSampleTimeBySlot[slot] = now;
        Append(body.ToString());
    }

    private static void AppendSpriteEmitter7SpawnVerification(
        GroupSession session,
        int slot,
        float now,
        float shaderStartTime,
        float seed,
        DocExtractorSpriteEmitter0MotionSimulator.SpawnSnapshot spawn)
    {
        var body = new StringBuilder(512);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpawnParticleBegin Unity SpriteEmitter7 slot={0} spawnTime={1:F6} shaderStartTime={2:F6} seed={3:F6}{4}",
            slot,
            now,
            shaderStartTime,
            seed,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  appRandStateBeforeSpawn=0x{0:X8}{1}",
            spawn.AppRandStateBeforeSpawn,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  rawVelocityUU=({0:F6}, {1:F6}, {2:F6}) polarOffsetUU=({3:F6}, {4:F6}, {5:F6}) spawnPositionUU=({6:F6}, {7:F6}, {8:F6}){9}",
            spawn.RawVelocityUe.x,
            spawn.RawVelocityUe.y,
            spawn.RawVelocityUe.z,
            spawn.PolarOffsetUe.x,
            spawn.PolarOffsetUe.y,
            spawn.PolarOffsetUe.z,
            spawn.SpawnPositionUe.x,
            spawn.SpawnPositionUe.y,
            spawn.SpawnPositionUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  velocityAfterPtvdUU=({0:F6}, {1:F6}, {2:F6}) note=CPU SE0-style PTVD mirror; shader uses PTVD_OwnerAndStartPosition{3}",
            spawn.VelocityAfterPtvdUe.x,
            spawn.VelocityAfterPtvdUe.y,
            spawn.VelocityAfterPtvdUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  lifetimeSec={0:F6} spawnSizeUU={1:F6}{2}",
            spawn.LifetimeSeconds,
            spawn.SpawnSizeUU,
            Environment.NewLine);
        body.AppendLine("SpawnParticleEnd Unity SpriteEmitter7 slot=" + slot);
        Append(body.ToString());
    }

    private static void AppendSpriteEmitter0SpawnVerification(
        GroupSession session,
        int slot,
        float now,
        float shaderStartTime,
        float seed,
        DocExtractorSpriteEmitter0MotionSimulator.SpawnSnapshot spawn)
    {
        var body = new StringBuilder(768);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpawnParticleBegin Unity SpriteEmitter0 slot={0} spawnTime={1:F6} shaderStartTime={2:F6} seed={3:F6}{4}",
            slot,
            now,
            shaderStartTime,
            seed,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  slotAdvanceDraws={0}{1}",
            slot * SpriteEmitter0SlotToSlotDrawCount,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  appRandStateBeforeSpawn=0x{0:X8}{1}",
            spawn.AppRandStateBeforeSpawn,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  rawVelocityUU=({0:F6}, {1:F6}, {2:F6}) polarOffsetUU=({3:F6}, {4:F6}, {5:F6}) spawnPositionUU=({6:F6}, {7:F6}, {8:F6}){9}",
            spawn.RawVelocityUe.x,
            spawn.RawVelocityUe.y,
            spawn.RawVelocityUe.z,
            spawn.PolarOffsetUe.x,
            spawn.PolarOffsetUe.y,
            spawn.PolarOffsetUe.z,
            spawn.SpawnPositionUe.x,
            spawn.SpawnPositionUe.y,
            spawn.SpawnPositionUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  velocityBeforePtvdUU=({0:F6}, {1:F6}, {2:F6}) ptvdDirectionUU=({3:F6}, {4:F6}, {5:F6}) velocityAfterPtvdUU=({6:F6}, {7:F6}, {8:F6}){9}",
            spawn.VelocityBeforePtvdUe.x,
            spawn.VelocityBeforePtvdUe.y,
            spawn.VelocityBeforePtvdUe.z,
            spawn.PtvdDirectionUe.x,
            spawn.PtvdDirectionUe.y,
            spawn.PtvdDirectionUe.z,
            spawn.VelocityAfterPtvdUe.x,
            spawn.VelocityAfterPtvdUe.y,
            spawn.VelocityAfterPtvdUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  lifetimeSec={0:F6} spawnSizeUU={1:F6}{2}",
            spawn.LifetimeSeconds,
            spawn.SpawnSizeUU,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spawnMotionVerdict={0}{1}",
            BuildSpriteEmitter0SpawnVerdict(spawn),
            Environment.NewLine);
        body.AppendLine("SpawnParticleEnd Unity SpriteEmitter0 slot=" + slot);
        Append(body.ToString());
    }

    private static string BuildSpriteEmitter0SpawnVerdict(
        DocExtractorSpriteEmitter0MotionSimulator.SpawnSnapshot spawn)
    {
        if (spawn.AppRandStateBeforeSpawn == 0u)
        {
            return "FAIL missing-appRand-state — slots will share spawn values";
        }

        if (spawn.SpawnPositionUe.sqrMagnitude < 1e-4f)
        {
            return "WARN zero-spawn-position — polar offset may be missing";
        }

        if (spawn.VelocityAfterPtvdUe.sqrMagnitude < 1e-4f)
        {
            return "WARN zero-velocity-after-PTVD — check direction/velocity ranges";
        }

        return "PASS spawn-ready";
    }

    private static string BuildSpriteEmitter0MotionVerdict(
        uint appRandState,
        float particleTime,
        float tickDeltaSec,
        float speedErrorUe)
    {
        if (appRandState == 0u)
        {
            return "FAIL missing-appRand-state";
        }

        if (particleTime < 1e-4f || tickDeltaSec < 1e-4f)
        {
            return "PASS spawn-sample (speed check pending)";
        }

        float absSpeedError = Mathf.Abs(speedErrorUe);
        if (absSpeedError > 5f)
        {
            return "FAIL locLocal-speed-mismatch";
        }

        if (absSpeedError > 2f)
        {
            return "WARN locLocal-speed-drift";
        }

        return "PASS locLocal-motion-aligned";
    }

    private static void AppendSpriteEmitter0L2MotionReplayDiagnostic(
        Transform groupTransform,
        GroupSession session,
        int slot,
        Material mat,
        float shaderStartTime)
    {
        const string replayProperty = "_L2MotionReplayEnabled";
        if (slot != 0 || session.Se0L2MotionReplayDiagnosticLogged ||
            mat == null || !mat.HasProperty(replayProperty) ||
            mat.GetFloat(replayProperty) <= 0.5f)
        {
            return;
        }

        // Captured from L2 SpawnParticleSnapshot.log / ParticleSnapshot.log:
        // m_u004_b, SpriteEmitter8 (15475F00), slot 0, 2026-07-15.
        var body = new StringBuilder(1024);
        body.AppendLine("Se0L2MotionReplayDiagnostic");
        body.AppendLine(
            "  source=L2 m_u004_b SpriteEmitter8 slot=0 state=0x6FEC3FC2 spawnDt=0.0111764");
        body.AppendLine(
            "  mode=current Unity continuous displacement; compare against L2 discrete tick positions");

        float[] ages = { 0.0111764f, 0.0574f, 0.0704f };
        Vector3[] l2Locations =
        {
            new Vector3(1.947048783f, 0.304097384f, 8.153479576f),
            new Vector3(1.245f, 0.194f, 8.394f),
            new Vector3(1.048f, 0.164f, 8.446f),
        };
        float[] l2VelocityZ = { 6.437448978f, 4.589f, 4.070f };

        for (int i = 0; i < ages.Length; i++)
        {
            float replayNow = shaderStartTime + ages[i];
            if (!DocExtractorSpriteEmitter0MotionSimulator.TryEvaluate(
                    groupTransform,
                    mat,
                    replayNow,
                    shaderStartTime,
                    out DocExtractorSpriteEmitter0MotionSimulator.MotionSample replay))
            {
                body.AppendLine("  result=FAIL unable-to-evaluate-replay");
                break;
            }

            Vector3 delta = replay.LocLocalUe - l2Locations[i];
            body.AppendFormat(
                CultureInfo.InvariantCulture,
                "  age={0:F7}s unityLoc=({1:F6},{2:F6},{3:F6}) l2Loc=({4:F6},{5:F6},{6:F6}) locErrorUU={7:F6} unityVz={8:F6} l2Vz={9:F6}{10}",
                ages[i],
                replay.LocLocalUe.x,
                replay.LocLocalUe.y,
                replay.LocLocalUe.z,
                l2Locations[i].x,
                l2Locations[i].y,
                l2Locations[i].z,
                delta.magnitude,
                replay.VelocityNowUe.z,
                l2VelocityZ[i],
                Environment.NewLine);
        }

        body.AppendLine(
            "  interpretation=loc error is the continuous-vs-discrete integration delta; velocity Z should remain near L2.");
        Append(body.ToString());
        session.Se0L2MotionReplayDiagnosticLogged = true;
    }

    private static void AppendSpriteEmitter0GroupSpreadSummary(GroupSession session)
    {
        float horizSpanUe = session.Se0TickHorizMax - session.Se0TickHorizMin;
        float heightSpanUe = session.Se0TickZMax - session.Se0TickZMin;
        float horizSpanM = horizSpanUe / 52.5f * session.Se0TickWorldK;
        float heightSpanM = heightSpanUe / 52.5f * session.Se0TickWorldK;
        float zCenterM = ((session.Se0TickZMin + session.Se0TickZMax) * 0.5f) / 52.5f * session.Se0TickWorldK;

        var body = new StringBuilder(512);
        body.AppendLine("Se0GroupSpreadSummary");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  activeSlots={0} particleTime~={1:F4}s worldK={2:F2}{3}",
            session.Se0TickSampleCount,
            session.Se0TickParticleTime,
            session.Se0TickWorldK,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  horizRadiusUU=[{0:F3},{1:F3}] span={2:F3} heightUU=[{3:F3},{4:F3}] span={5:F3}{6}",
            session.Se0TickHorizMin,
            session.Se0TickHorizMax,
            horizSpanUe,
            session.Se0TickZMin,
            session.Se0TickZMax,
            heightSpanUe,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  horizSpanM={0:F4} heightSpanM={1:F4} heightCenterM={2:F4}{3}",
            horizSpanM,
            heightSpanM,
            zCenterM,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  l2Ref@t~0.057 horizSpanUU~0.8 heightSpanUU~3.0 horizSpanM~{0:F4}@K={1:F2}{2}",
            0.8f / 52.5f * session.Se0TickWorldK,
            session.Se0TickWorldK,
            Environment.NewLine);
        body.AppendLine(
            "  spreadVerdict=compare horizSpanUU first; if Unity>>L2 in UU then formula/RNG, if UU~match but looks wide then K or sprite size");
        Append(body.ToString());
    }

    private static void AppendWaveBurstSummary(StringBuilder body, GroupSession session)
    {
        if (session.WaveBurstLocZ.Count == 0)
        {
            return;
        }

        float[] sorted = session.WaveBurstLocZ.ToArray();
        Array.Sort(sorted);
        var gaps = new List<float>(sorted.Length - 1);
        for (int i = 0; i + 1 < sorted.Length; i++)
        {
            gaps.Add(sorted[i + 1] - sorted[i]);
        }

        float worldK = 1.8f;
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  WaveBurstSummary burst={0}/10 slots={1} baseNote=LCG31 appRandLocVelSize=1{2}",
            session.WaveBurstIndex,
            session.WaveBurstLocZ.Count,
            Environment.NewLine);
        body.Append("  WaveBurstLocZUU=[");
        for (int i = 0; i < session.WaveBurstLocZ.Count; i++)
        {
            if (i > 0)
            {
                body.Append(", ");
            }

            body.Append(session.WaveBurstLocZ[i].ToString("F4", CultureInfo.InvariantCulture));
        }

        body.Append(']').Append(Environment.NewLine);
        body.Append("  WaveBurstLocZSortedUU=[");
        for (int i = 0; i < sorted.Length; i++)
        {
            if (i > 0)
            {
                body.Append(", ");
            }

            body.Append(sorted[i].ToString("F4", CultureInfo.InvariantCulture));
        }

        body.Append(']').Append(Environment.NewLine);
        body.Append("  WaveBurstGapsUU=[");
        for (int i = 0; i < gaps.Count; i++)
        {
            if (i > 0)
            {
                body.Append(", ");
            }

            body.Append(gaps[i].ToString("F4", CultureInfo.InvariantCulture));
        }

        body.Append(']').Append(Environment.NewLine);
        body.Append("  WaveBurstGapsCmK18=[");
        for (int i = 0; i < gaps.Count; i++)
        {
            if (i > 0)
            {
                body.Append(", ");
            }

            float cm = gaps[i] / 52.5f * worldK * 100f;
            body.Append(cm.ToString("F2", CultureInfo.InvariantCulture));
        }

        body.Append(']').Append(Environment.NewLine);

        float minGap = gaps.Count > 0 ? gaps[0] : 0f;
        float maxGap = gaps.Count > 0 ? gaps[0] : 0f;
        for (int i = 1; i < gaps.Count; i++)
        {
            minGap = Mathf.Min(minGap, gaps[i]);
            maxGap = Mathf.Max(maxGap, gaps[i]);
        }

        string line = string.Format(
            CultureInfo.InvariantCulture,
            "burst{0}: minGapUU={1:F4} maxGapUU={2:F4} minCm={3:F2} maxCm={4:F2}",
            session.WaveBurstIndex,
            minGap,
            maxGap,
            minGap / 52.5f * worldK * 100f,
            maxGap / 52.5f * worldK * 100f);
        session.WaveBurstGapSummaries.Add(line);
        body.Append("  ").Append(line).Append(Environment.NewLine);

        if (session.WaveBurstIndex == 10)
        {
            body.AppendLine("  WaveBurstCampaignDone=10 note=compare minGap clustering vs L2 SpawnWaveCapture");
            for (int i = 0; i < session.WaveBurstGapSummaries.Count; i++)
            {
                body.Append("  ").Append(session.WaveBurstGapSummaries[i]).Append(Environment.NewLine);
            }
        }
    }

    // CPU mirror of MeshEmitter0_Wave.shader / L2 MeshEmitter4 (Wave) live slots.
    // Compare Unity_ParticleSnapshot.log vs ParticleSnapshot.log MeshEmitter[1] MeshEmitter4.
    private static void WriteMeshEmitter0WaveSample(
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

        float startSizeXY = mat.GetFloat("_StartSizeXY");
        Vector4 startSizeZRange = mat.GetVector("_StartSizeZRange");
        Vector4 startLocZRange = mat.GetVector("_StartLocationZRangeUU");
        Vector4 startVelZRange = mat.GetVector("_StartVelocityZRangeUU");

        float startSizeZ;
        float startLocZ;
        float startVelZ;
        uint meshSpawnState = L2MaterialPropertyCopier.ReadMeshSpawnRandState(mat);
        bool usedMeshSpawnAppRand = meshSpawnState != 0u;
        if (usedMeshSpawnAppRand)
        {
            L2MaterialPropertyCopier.SampleMeshSpawnLocVelSizeZ(
                mat,
                meshSpawnState,
                out startLocZ,
                out startVelZ,
                out startSizeZ);
        }
        else
        {
            startSizeZ = RandomRange(startSizeZRange.x, startSizeZRange.y, seed, shaderStartTime, 11f);
            startLocZ = RandomRange(startLocZRange.x, startLocZRange.y, seed, shaderStartTime, 13f);
            startVelZ = RandomRange(startVelZRange.x, startVelZRange.y, seed, shaderStartTime, 17f);
        }

        Vector3 startSizeUe = new Vector3(startSizeXY, startSizeXY, startSizeZ);
        Vector3 finalSizeUe = startSizeUe * sizeMul;

        float worldK = mat.HasProperty("_L2FxWorldCalibration") ? mat.GetFloat("_L2FxWorldCalibration") : 1.8f;
        Vector3 locLocalUe = new Vector3(0f, 0f, startLocZ + startVelZ * particleTime);
        Vector3 motionUnity = DocExtractorSpriteEmitter0MotionSimulator.UcPositionToUnityMeters(locLocalUe, worldK);
        Vector3 locWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(
            renderer.transform.position + motionUnity);

        Vector4 sps = mat.HasProperty("_SpsYawPitchRollUc")
            ? mat.GetVector("_SpsYawPitchRollUc")
            : Vector4.zero;
        Vector4 spinCcw = mat.HasProperty("_SpinCCWorCW")
            ? mat.GetVector("_SpinCCWorCW")
            : Vector4.zero;
        // PTRS_Actor / RenderParticles: slot c0,c1,c2; FRotationMatrix(Pitch,Yaw,Roll)=(c1,c0,c2).
        // SpinCCWorCW.X==0 => negate (matches L2Fx_ApplySpinCCWorCW_Scalar).
        float sps0 = (spinCcw.x == 0f ? -1f : 1f) * sps.x;
        float sps1 = (spinCcw.y == 0f ? -1f : 1f) * sps.y;
        float sps2 = (spinCcw.z == 0f ? -1f : 1f) * sps.z;
        Vector3 spinRateC012 = new Vector3(sps0, sps1, sps2) * SpinUcToUru;

        MeshStartSpinSnapshot startSpin = ReadMeshStartSpinSnapshot(
            mat,
            renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                ? renderer.sharedMaterials[0]
                : null,
            slot,
            group.MeshEmitter3AppRandBaseState);
        Vector3 startSpinC012;
        if (startSpin.HasAppRand)
        {
            startSpinC012 = startSpin.YawPitchRollUru;
        }
        else
        {
            Vector4 yawRange = mat.HasProperty("_StartSpinYawRangeUc")
                ? mat.GetVector("_StartSpinYawRangeUc")
                : Vector4.zero;
            Vector4 pitchRange = mat.HasProperty("_StartSpinPitchRangeUc")
                ? mat.GetVector("_StartSpinPitchRangeUc")
                : Vector4.zero;
            Vector4 rollRange = mat.HasProperty("_StartSpinRollRangeUc")
                ? mat.GetVector("_StartSpinRollRangeUc")
                : Vector4.zero;
            startSpinC012 = new Vector3(
                RandomRange(yawRange.x, yawRange.y, seed, shaderStartTime, 401f) * SpinUcToUru,
                RandomRange(pitchRange.x, pitchRange.y, seed, shaderStartTime, 409f) * SpinUcToUru,
                RandomRange(rollRange.x, rollRange.y, seed, shaderStartTime, 419f) * SpinUcToUru);
        }

        // Runtime FRotationMatrix input (Pitch,Yaw,Roll)=(c1,c0,c2).
        Vector3 runtimeC012 = new Vector3(
            Mathf.Floor(startSpinC012.x + spinRateC012.x * particleTime),
            Mathf.Floor(startSpinC012.y + spinRateC012.y * particleTime),
            Mathf.Floor(startSpinC012.z + spinRateC012.z * particleTime));
        Vector3 runtimePitchYawRoll = new Vector3(runtimeC012.y, runtimeC012.x, runtimeC012.z);
        Vector3 spinVelUru = spinRateC012;
        Vector3 runtimeRotUru = runtimeC012;

        Vector4 colorMul = mat.HasProperty("_ColorMultiplier")
            ? mat.GetVector("_ColorMultiplier")
            : Vector4.one;
        float opacity = mat.HasProperty("_Opacity") ? mat.GetFloat("_Opacity") : 1f;
        float fadeInEnd = mat.HasProperty("_FadeInEndTime") ? mat.GetFloat("_FadeInEndTime") : 0f;
        float fadeOutStart = mat.HasProperty("_FadeOutStartTime") ? mat.GetFloat("_FadeOutStartTime") : lifetime;
        bool fadeIn = !mat.HasProperty("_FadeIn") || mat.GetFloat("_FadeIn") > 0.5f;
        bool fadeOut = !mat.HasProperty("_FadeOut") || mat.GetFloat("_FadeOut") > 0.5f;
        float fadeInAmt = fadeIn && fadeInEnd > 1e-6f && particleTime < fadeInEnd
            ? (fadeInEnd - particleTime) / fadeInEnd
            : 0f;
        float fadeOutAmt = fadeOut && particleTime > fadeOutStart
            ? (particleTime - fadeOutStart) / Mathf.Max(1e-4f, lifetime - fadeOutStart)
            : 0f;
        // Live L2: ColorScale*ColorMultiplier - fade, then Opacity multiplies RGB only.
        float r = Mathf.Max(0f, 1f * colorMul.x - fadeInAmt - fadeOutAmt) * opacity;
        float g = Mathf.Max(0f, 1f * colorMul.y - fadeInAmt - fadeOutAmt) * opacity;
        float b = Mathf.Max(0f, 1f * colorMul.z - fadeInAmt - fadeOutAmt) * opacity;
        float a = Mathf.Max(0f, 1f - fadeInAmt - fadeOutAmt);

        if (!session.PrevLocLocalUe.TryGetValue(slot, out Vector3 prevLoc))
        {
            prevLoc = locLocalUe;
        }

        Vector3 deltaLoc = locLocalUe - prevLoc;
        session.PrevLocLocalUe[slot] = locLocalUe;
        session.PrevSampleTimeBySlot.TryGetValue(slot, out float prevSampleTime);
        float dt = prevSampleTime > 0f ? Mathf.Max(1e-4f, now - prevSampleTime) : 0f;
        session.PrevSampleTimeBySlot[slot] = now;

        session.TickCounter += 1;
        var body = new StringBuilder(1400);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "MeshEmitter0[{0}] MeshParticle[{1}] Tick{2}{3}",
            0,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(group) +
            " aEmitterName=ParticleGroup/MeshEmitter0 effect=UnityEffect.it_healing_potion_ta spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=2 kind=Mesh name=Wave class=MeshEmitter note=L2 log name=MeshEmitter4");
        if (isSpawnEvent)
        {
            body.AppendFormat(
                CultureInfo.InvariantCulture,
                "  MeshParticleSpawn slot={0} seed={1:F4} shaderStart={2:F4} startLocZUU={3:F4} startVelZUU={4:F4} startSizeZ={5:F4} appRand={6} meshSpawnState=0x{7:X8}{8}",
                slot,
                seed,
                shaderStartTime,
                startLocZ,
                startVelZ,
                startSizeZ,
                usedMeshSpawnAppRand ? 1 : 0,
                meshSpawnState,
                Environment.NewLine);

            session.WaveSpawnSlotsThisBurst += 1;
            session.WaveBurstLocZ.Add(startLocZ);
            session.WaveBurstVelZ.Add(startVelZ);
            if (session.WaveSpawnSlotsThisBurst >= 5)
            {
                AppendWaveBurstSummary(body, session);
            }
        }

        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locLocal@+0x00=({0:F4}, {1:F4}, {2:F4}){3}" +
            "  locWorld=({4:F2}, {5:F2}, {6:F2}){3}" +
            "  startSize@+0x24=({7:F4}, {8:F4}, {9:F4}){3}" +
            "  finalSize@+0x6C=({10:F4}, {11:F4}, {12:F4}){3}" +
            "  particleTime={13:F4} maxLifetime={14:F4} lifeNorm={15:F4} sizeMul={16:F4}{3}",
            locLocalUe.x, locLocalUe.y, locLocalUe.z,
            Environment.NewLine,
            locWorldUe.x, locWorldUe.y, locWorldUe.z,
            startSizeUe.x, startSizeUe.y, startSizeUe.z,
            finalSizeUe.x, finalSizeUe.y, finalSizeUe.z,
            particleTime, lifetime, lifeNorm, sizeMul);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spinVelocityURU@+0x30=(c0={0:F4},c1={1:F4},c2={2:F4}) trunc=({3},{4},{5}){6}" +
            "  startRotationURU@+0x3C=(c0={7:F4},c1={8:F4},c2={9:F4}) trunc=({10},{11},{12}){6}" +
            "  runtimeC012=(c0={13:F0},c1={14:F0},c2={15:F0}) FRotationMatrix(Pitch,Yaw,Roll)=({16:F0},{17:F0},{18:F0}){6}",
            spinVelUru.x, spinVelUru.y, spinVelUru.z,
            (int)spinVelUru.x, (int)spinVelUru.y, (int)spinVelUru.z,
            Environment.NewLine,
            startSpinC012.x, startSpinC012.y, startSpinC012.z,
            (int)startSpinC012.x, (int)startSpinC012.y, (int)startSpinC012.z,
            runtimeRotUru.x, runtimeRotUru.y, runtimeRotUru.z,
            runtimePitchYawRoll.x, runtimePitchYawRoll.y, runtimePitchYawRoll.z);
        AppendWaveSpinNormalDiagnostics(body, runtimePitchYawRoll);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  colorMultiplier@+0x84=({0:F4}, {1:F4}, {2:F4}) opacity={3:F4} hdrPeak={4:F4}{5}" +
            "  runtimeColorA8@+0xA8=({6}, {7}, {8}, {9}) note=CPU MeshColorFade+Opacity mirror{5}",
            colorMul.x, colorMul.y, colorMul.z,
            opacity,
            Mathf.Max(colorMul.x, Mathf.Max(colorMul.y, colorMul.z)),
            Environment.NewLine,
            ToFloorByte(b), ToFloorByte(g), ToFloorByte(r), ToFloorByte(a));
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  unityDiag motionUnityMeters=({0:F4}, {1:F4}, {2:F4}) finalSizeUe=(XY,XY,Z)=({3:F4}, {3:F4}, {4:F4}) K={5:F3}{6}" +
            "  unityDiag deltaLocLocalUU=({7:F4}, {8:F4}, {9:F4}) dt={10:F4}s impliedVelZUU={11:F4}{6}" +
            "  unityDiag spinPath=L2FxMeshSpin S(XY,Z,XY)*R(c1,c0,c2) note=no PTRS_Actor on Wave{6}",
            motionUnity.x, motionUnity.y, motionUnity.z,
            startSizeXY * sizeMul * worldK,
            startSizeZ * sizeMul * worldK,
            worldK,
            Environment.NewLine,
            deltaLoc.x, deltaLoc.y, deltaLoc.z,
            dt,
            dt > 0f ? deltaLoc.z / dt : 0f);
        if (startSpin.HasAppRand)
        {
            AppendMeshStartSpinSnapshot(body, startSpin);
        }

        AppendWaveAxisDiagnostics(
            body,
            session,
            renderer,
            scaleXy: startSizeXY * sizeMul * worldK,
            scaleZ: startSizeZ * sizeMul * worldK,
            runtimeRotUru,
            particleTime);
        Append(body.ToString());
    }

    // Mirrors L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll for the
    // remapped Wave plane normal. This logs the GPU formula's expected result;
    // it is not Transform.rotation (spin is applied in the vertex shader).
    private static void AppendWaveSpinNormalDiagnostics(
        StringBuilder body,
        Vector3 pitchYawRollUru)
    {
        Vector3 pitchYawRollRadians = new Vector3(
            pitchYawRollUru.x * Mathf.PI * 2f / 65536f,
            pitchYawRollUru.y * Mathf.PI * 2f / 65536f,
            pitchYawRollUru.z * Mathf.PI * 2f / 65536f);
        Vector3 normalAfterSpin = RotateUnityLocalAsMeshSpin(
            Vector3.up,
            pitchYawRollRadians).normalized;
        float tiltDeg = Mathf.Acos(Mathf.Clamp(
            Mathf.Abs(Vector3.Dot(normalAfterSpin, Vector3.up)),
            -1f,
            1f)) * Mathf.Rad2Deg;
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  spinNormalDiag preUnity=(0,1,0) postUnity=({0:F5},{1:F5},{2:F5}) tiltFromUnityY={3:F3}deg{4}",
            normalAfterSpin.x,
            normalAfterSpin.y,
            normalAfterSpin.z,
            tiltDeg,
            Environment.NewLine);
    }

    /// <summary>
    /// Compares mesh AABB under two StartSize axis layouts:
    /// A) UE-layout mesh (bakeAxisConversion=0): scale (XY, XY, Z)
    /// B) Unity-remapped mesh: scale (XY, Z, XY)
    /// A flat ring should stay thin on its thickness axis after scale.
    /// </summary>
    private static void AppendWaveAxisDiagnostics(
        StringBuilder body,
        GroupSession session,
        Renderer renderer,
        float scaleXy,
        float scaleZ,
        Vector3 runtimeRotUru,
        float particleTime)
    {
        MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
        {
            body.AppendLine("  axisDiag=unavailable note=missing MeshFilter/sharedMesh");
            return;
        }

        Bounds raw = mesh.bounds;
        Vector3 rawSize = raw.size;
        string thinAxis = DescribeThinnestAxis(rawSize);
        Vector3 scaleUeLayout = new Vector3(scaleXy, scaleXy, scaleZ);
        Vector3 scaleUnityRemap = new Vector3(scaleXy, scaleZ, scaleXy);
        Vector3 sizeUeLayout = AbsMul(rawSize, scaleUeLayout);
        Vector3 sizeUnityRemap = AbsMul(rawSize, scaleUnityRemap);

        Vector3 sizeAfterSpinRemap = MeasureAabbSizeAfterScaleAndMeshSpin(
            raw,
            scaleUnityRemap,
            runtimeRotUru);
        Vector3 sizeAfterSpinUe = MeasureAabbSizeAfterScaleAndUeRotator(
            raw,
            scaleUeLayout,
            runtimeRotUru);

        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  axisDiag mesh='{0}' rawBoundsSize=({1:F4}, {2:F4}, {3:F4}) thinAxis={4} age={5:F4}s{6}" +
            "  axisDiag scaleUeLayout(XY,XY,Z)=({7:F4}, {7:F4}, {8:F4}) -> size=({9:F4}, {10:F4}, {11:F4}) thin={12}{6}" +
            "  axisDiag scaleUnityRemap(XY,Z,XY)=({7:F4}, {8:F4}, {7:F4}) -> size=({13:F4}, {14:F4}, {15:F4}) thin={16}{6}" +
            "  axisDiag afterSpinUeLayout size=({17:F4}, {18:F4}, {19:F4}) thin={20}{6}" +
            "  axisDiag afterSpinUnityRemap size=({21:F4}, {22:F4}, {23:F4}) thin={24}{6}",
            mesh.name,
            rawSize.x, rawSize.y, rawSize.z,
            thinAxis,
            particleTime,
            Environment.NewLine,
            scaleXy, scaleZ,
            sizeUeLayout.x, sizeUeLayout.y, sizeUeLayout.z,
            DescribeThinnestAxis(sizeUeLayout),
            sizeUnityRemap.x, sizeUnityRemap.y, sizeUnityRemap.z,
            DescribeThinnestAxis(sizeUnityRemap),
            sizeAfterSpinUe.x, sizeAfterSpinUe.y, sizeAfterSpinUe.z,
            DescribeThinnestAxis(sizeAfterSpinUe),
            sizeAfterSpinRemap.x, sizeAfterSpinRemap.y, sizeAfterSpinRemap.z,
            DescribeThinnestAxis(sizeAfterSpinRemap));

        if (!session.WaveAxisDiagLogged)
        {
            session.WaveAxisDiagLogged = true;
            string verdict;
            if (thinAxis == "Y")
            {
                verdict =
                    "raw mesh thin on Y => Unity-remapped verts. " +
                    "L2FxPTRSActor Unity->UE, S(XY,XY,Z)*R(c1,c0,c2), UE->Unity is " +
                    "algebraically equivalent to Unity scale (XY,Z,XY) followed by " +
                    "the conjugated UE rotator. Do not apply scaleUeLayout directly " +
                    "to raw Unity vertices; that leaves the thin axis on Unity Z.";
            }
            else if (thinAxis == "Z")
            {
                verdict =
                    "raw mesh thin on Z => UE-axis verts. S(XY,XY,Z)*R in place is enough.";
            }
            else
            {
                verdict =
                    "raw mesh thin on X (unexpected for ring). Inspect FBX import/orientation.";
            }

            body.AppendLine("  axisDiagVerdict=" + verdict);
        }
    }

    private static Vector3 AbsMul(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Abs(a.x * b.x), Mathf.Abs(a.y * b.y), Mathf.Abs(a.z * b.z));
    }

    private static string DescribeThinnestAxis(Vector3 size)
    {
        float ax = Mathf.Abs(size.x);
        float ay = Mathf.Abs(size.y);
        float az = Mathf.Abs(size.z);
        if (ax <= ay && ax <= az)
        {
            return "X";
        }

        if (ay <= ax && ay <= az)
        {
            return "Y";
        }

        return "Z";
    }

    private static Vector3 MeasureAabbSizeAfterScaleAndMeshSpin(Bounds raw, Vector3 scale, Vector3 yawPitchRollUru)
    {
        Vector3 pitchYawRollRad = YawPitchRollUruToPitchYawRollRadians(yawPitchRollUru);
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in EnumerateBoundsCorners(raw))
        {
            Vector3 scaled = Vector3.Scale(corner, scale);
            Vector3 rotated = RotateUnityLocalAsMeshSpin(scaled, pitchYawRollRad);
            min = Vector3.Min(min, rotated);
            max = Vector3.Max(max, rotated);
        }

        return max - min;
    }

    private static Vector3 MeasureAabbSizeAfterScaleAndUeRotator(Bounds raw, Vector3 scale, Vector3 yawPitchRollUru)
    {
        Vector3 pitchYawRollRad = YawPitchRollUruToPitchYawRollRadians(yawPitchRollUru);
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in EnumerateBoundsCorners(raw))
        {
            Vector3 scaled = Vector3.Scale(corner, scale);
            Vector3 rotated = RotateUeLocalPitchYawRoll(scaled, pitchYawRollRad);
            min = Vector3.Min(min, rotated);
            max = Vector3.Max(max, rotated);
        }

        return max - min;
    }

    private static IEnumerable<Vector3> EnumerateBoundsCorners(Bounds b)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;
        for (int ix = -1; ix <= 1; ix += 2)
        {
            for (int iy = -1; iy <= 1; iy += 2)
            {
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    yield return c + new Vector3(e.x * ix, e.y * iy, e.z * iz);
                }
            }
        }
    }

    private static Vector3 YawPitchRollUruToPitchYawRollRadians(Vector3 yawPitchRollUru)
    {
        const float uruToRad = (Mathf.PI * 2f) / 65536f;
        return new Vector3(
            yawPitchRollUru.y * uruToRad,
            yawPitchRollUru.x * uruToRad,
            yawPitchRollUru.z * uruToRad);
    }

    private static Vector3 RotateUnityLocalAsMeshSpin(Vector3 unityLocal, Vector3 pitchYawRollRadians)
    {
        Vector3 ue = new Vector3(unityLocal.x, unityLocal.z, unityLocal.y);
        Vector3 rotatedUe = RotateUeLocalPitchYawRoll(ue, pitchYawRollRadians);
        return new Vector3(rotatedUe.x, rotatedUe.z, rotatedUe.y);
    }

    private static Vector3 RotateUeLocalPitchYawRoll(Vector3 ueLocal, Vector3 pitchYawRollRadians)
    {
        float sinPitch = Mathf.Sin(pitchYawRollRadians.x);
        float cosPitch = Mathf.Cos(pitchYawRollRadians.x);
        float sinYaw = Mathf.Sin(pitchYawRollRadians.y);
        float cosYaw = Mathf.Cos(pitchYawRollRadians.y);
        float sinRoll = Mathf.Sin(pitchYawRollRadians.z);
        float cosRoll = Mathf.Cos(pitchYawRollRadians.z);

        float m00 = cosPitch * cosYaw;
        float m01 = cosPitch * sinYaw;
        float m02 = sinPitch;
        float m10 = sinRoll * sinPitch * cosYaw - cosRoll * sinYaw;
        float m11 = sinRoll * sinPitch * sinYaw + cosRoll * cosYaw;
        float m12 = -sinRoll * cosPitch;
        float m20 = -(cosRoll * sinPitch * cosYaw + sinRoll * sinYaw);
        float m21 = cosYaw * sinRoll - cosRoll * sinPitch * sinYaw;
        float m22 = cosRoll * cosPitch;

        return new Vector3(
            ueLocal.x * m00 + ueLocal.y * m10 + ueLocal.z * m20,
            ueLocal.x * m01 + ueLocal.y * m11 + ueLocal.z * m21,
            ueLocal.x * m02 + ueLocal.y * m12 + ueLocal.z * m22);
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
        // Mesh spawn base = before StartVelocity (+22 to StartSpin). MeshEmitter3 base = before StartSpin.
        bool isMeshSpawn = L2MaterialPropertyCopier.IsMeshSpawnParticleMaterial(sharedMat) ||
            L2MaterialPropertyCopier.IsMeshSpawnParticleMaterial(mat);
        uint expectedStateBeforeRoll = liveBaseState != 0u
            ? (isMeshSpawn
                ? L2MaterialPropertyCopier.ComputeMeshSpawnStartSpinState(liveBaseState, slotIndex)
                : L2MaterialPropertyCopier.ComputeMeshEmitter3StartSpinState(liveBaseState, slotIndex))
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

    private static string ResolveHealingPotionEmitterName(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return "?";
        }

        if (groupName.IndexOf("SpriteEmitter0", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "ParticleGroup/SpriteEmitter0";
        }

        if (groupName.IndexOf("SpriteEmitter7", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "ParticleGroup/SpriteEmitter7";
        }

        if (groupName.IndexOf("MeshEmitter0", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "ParticleGroup/MeshEmitter0";
        }

        if (groupName.IndexOf("MeshEmitter3", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "ParticleGroup/MeshEmitter3";
        }

        if (groupName.IndexOf("SpriteEmitter2", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "ParticleGroup/SpriteEmitter2";
        }

        return "?";
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
