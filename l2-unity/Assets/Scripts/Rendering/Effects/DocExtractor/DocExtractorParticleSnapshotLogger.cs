#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes Unity_ParticleSnapshot.log for shot_N_atk MeshEmitter226 ShockWave
/// and SpriteEmitter325 Smog — direction sync diagnostics (caster vs layer motion).
/// </summary>
public static class DocExtractorParticleSnapshotLogger
{
    public const string DefaultLogPath =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\Unity_ParticleSnapshot.log";

    public const float SampleIntervalSec = 0.05f;

    private const uint AppRandMultiplier = 214013u;
    private const uint AppRandIncrement = 2531011u;
    private const float AppFrandDivisor = 32767f;
    private const float SpinUcToUru = 65535f;

    public static bool Enabled = true;

    private static readonly object WriteLock = new object();
    private static readonly Dictionary<int, GroupSession> Sessions = new Dictionary<int, GroupSession>();
    private static int OpenEffectOwnerId;

    private sealed class GroupSession
    {
        public bool Open;
        public int TickCounter;
        public float LastSampleTime = -999f;
        public readonly Dictionary<int, Vector3> PrevLocLocalUe = new Dictionary<int, Vector3>();
        public readonly Dictionary<int, float> PrevSampleTimeBySlot = new Dictionary<int, float>();
        public readonly Dictionary<int, float> StartSpinRollUruBySlot = new Dictionary<int, float>();
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
                (Mathf.Abs(yawPitchRollUru.x) > 1e-3f ? 1 : 0) +
                (Mathf.Abs(yawPitchRollUru.y) > 1e-3f ? 1 : 0) +
                (Mathf.Abs(yawPitchRollUru.z) > 1e-3f ? 1 : 0);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeLoad()
    {
    }

    public static bool ShouldTrace(ParticleGroup group)
    {
        if (!Enabled || group == null || string.IsNullOrEmpty(group.name))
        {
            return false;
        }

        bool isShockWave = group.name.IndexOf("MeshEmitter226", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isSmog = group.name.IndexOf("SpriteEmitter325", StringComparison.OrdinalIgnoreCase) >= 0;
        if (!isShockWave && !isSmog)
        {
            return false;
        }

        Transform current = group.transform;
        for (int depth = 0; current != null && depth < 16; depth++, current = current.parent)
        {
            if (string.IsNullOrEmpty(current.name))
            {
                continue;
            }

            if (current.name.IndexOf("shot_N_atk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                current.name.IndexOf("e_u505", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldTrace(ParticleSingle single)
    {
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

    public static void OnParticleActivated(
        ParticleSingle single,
        Renderer renderer,
        float now,
        float shaderStartTime,
        float seed)
    {
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

    public static void OnFixedUpdateTick(ParticleSingle single, float now, Renderer renderer)
    {
    }

    public static void OnSlotOff(ParticleSingle single)
    {
    }

    public static void OnSlotOff(ParticleGroup group, int slot)
    {
        if (!ShouldTrace(group))
        {
            return;
        }

        GroupSession session = GetOrCreateSession(group);
        session.PrevLocLocalUe.Remove(slot);
        session.PrevSampleTimeBySlot.Remove(slot);
        session.StartSpinRollUruBySlot.Remove(slot);
    }

    private static void OpenEffectSession(ParticleGroup group)
    {
        GroupSession session = GetOrCreateSession(group);
        session.Open = true;
        session.TickCounter = 0;
        session.LastSampleTime = -999f;
        session.PrevLocLocalUe.Clear();
        session.PrevSampleTimeBySlot.Clear();
        session.StartSpinRollUruBySlot.Clear();

        L2Particle owner = group.OwnerParticle;
        int ownerId = owner != null ? owner.GetInstanceID() : group.GetInstanceID();
        if (OpenEffectOwnerId == ownerId)
        {
            return;
        }

        OpenEffectOwnerId = ownerId;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : group.transform;

        Vector3 emitterWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(group.transform.position);
        Vector3 casterFwdUnity = caster != null ? caster.forward : Vector3.forward;
        Vector3 casterRightUnity = caster != null ? caster.right : Vector3.right;
        Vector3 casterFwdUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(casterFwdUnity);
        Vector3 casterRightUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(casterRightUnity);
        string effectName = ResolveShotNAtkEffectName(owner);
        string layerHint = ResolveLayerName(group);

        var body = new StringBuilder(700);
        body.AppendLine("================================================================================");
        body.AppendLine(
            "EFFECT SESSION aEmitter=" + FormatPointer(owner != null ? (UnityEngine.Object)owner : group) +
            " layers=MeshEmitter226+SpriteEmitter325 effect=" + effectName + " spawnKind=self");
        body.AppendLine(
            "firstLayer=" + layerHint +
            " caster=" + FormatPointer(caster) + " sourceActor=" + FormatPointer(caster));
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "emitterWorld=({0:F2}, {1:F2}, {2:F2}){3}",
            emitterWorldUe.x,
            emitterWorldUe.y,
            emitterWorldUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "casterAxesUnity fwd=({0:F4},{1:F4},{2:F4}) right=({3:F4},{4:F4},{5:F4}){6}",
            casterFwdUnity.x, casterFwdUnity.y, casterFwdUnity.z,
            casterRightUnity.x, casterRightUnity.y, casterRightUnity.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "casterAxesUe fwd=({0:F4},{1:F4},{2:F4}) right=({3:F4},{4:F4},{5:F4}){6}",
            casterFwdUe.x, casterFwdUe.y, casterFwdUe.z,
            casterRightUe.x, casterRightUe.y, casterRightUe.z,
            Environment.NewLine);
        body.AppendLine(
            "axisContract UE(x,y,z)->OS(x,z,y); coneTravel=UE+X=OS+X=emitter.right; " +
            "shockwave00 thin=X disc=YZ; Smog=billboard pivot only");
        body.AppendLine(
            "compare=coneDiag.verdict ShockWave vs Smog (ON_CONE_AXIS/IN_CONE) — not caster.forward");
        body.AppendLine("started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        body.AppendLine("log=" + ResolveLogPath());
        body.AppendLine("================================================================================");
        Append(body.ToString());
    }

    private static string ResolveLayerName(ParticleGroup group)
    {
        if (group == null || string.IsNullOrEmpty(group.name))
        {
            return "unknown";
        }

        if (group.name.IndexOf("MeshEmitter226", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "MeshEmitter226/ShockWave";
        }

        if (group.name.IndexOf("SpriteEmitter325", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "SpriteEmitter325/Smog";
        }

        return group.name;
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
        if (renderer == null || renderer.materials == null || renderer.materials.Length == 0)
        {
            return;
        }

        Material mat = renderer.materials[0];
        if (mat == null)
        {
            return;
        }

        if (IsShotNAtkMeshEmitter226ShockWave(group, mat))
        {
            WriteMeshEmitter226ShockWaveSample(
                group, session, slot, renderer, mat, now, shaderStartTime, seed, isSpawnEvent: force);
            return;
        }

        if (IsShotNAtkSpriteEmitter325Smog(group, mat))
        {
            WriteSpriteEmitter325SmogSample(
                group, session, slot, renderer, mat, now, shaderStartTime, seed, isSpawnEvent: force);
        }
    }

    private static bool IsShotNAtkMeshEmitter226ShockWave(ParticleGroup group, Material mat)
    {
        if (group != null &&
            !string.IsNullOrEmpty(group.name) &&
            group.name.IndexOf("MeshEmitter226", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return L2MaterialPropertyCopier.IsShotNAtkMeshEmitter226ShockWaveMaterial(mat);
    }

    private static bool IsShotNAtkSpriteEmitter325Smog(ParticleGroup group, Material mat)
    {
        if (group != null &&
            !string.IsNullOrEmpty(group.name) &&
            group.name.IndexOf("SpriteEmitter325", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return DocExtractorSpriteEmitter0MotionSimulator.IsShotNAtkSpriteEmitter325Material(mat);
    }

    private static void WriteMeshEmitter226ShockWaveSample(
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
        uint meshSpawnState = L2MaterialPropertyCopier.ReadMeshSpawnRandState(mat);
        bool usedMeshSpawnAppRand = meshSpawnState != 0u;

        Vector3 velocityUe;
        Vector3 locationUe;
        Vector3 colorMulRgb;
        float lifetime;
        float delay;
        Vector3 startSizeUe;

        if (usedMeshSpawnAppRand)
        {
            L2MaterialPropertyCopier.SampleMeshSpawnLocVelSize(
                mat,
                meshSpawnState,
                out velocityUe,
                out locationUe,
                out colorMulRgb,
                out lifetime,
                out delay,
                out startSizeUe);
        }
        else
        {
            Vector4 delayRange = mat.GetVector("_InitialDelayRange");
            Vector4 lifetimeRange = mat.GetVector("_LifetimeRange");
            delay = RandomRange(delayRange.x, delayRange.y, seed, shaderStartTime, 3f);
            lifetime = Mathf.Max(1e-4f, RandomRange(lifetimeRange.x, lifetimeRange.y, seed, shaderStartTime, 7f));
            velocityUe = new Vector3(
                RandomRange(mat.GetVector("_StartVelocityRangeXUc").x, mat.GetVector("_StartVelocityRangeXUc").y, seed, shaderStartTime, 17f),
                RandomRange(mat.GetVector("_StartVelocityRangeYUc").x, mat.GetVector("_StartVelocityRangeYUc").y, seed, shaderStartTime, 19f),
                RandomRange(mat.GetVector("_StartVelocityRangeZUc").x, mat.GetVector("_StartVelocityRangeZUc").y, seed, shaderStartTime, 23f));
            locationUe = Vector3.zero;
            startSizeUe = new Vector3(
                RandomRange(mat.GetVector("_StartSizeRangeXUc").x, mat.GetVector("_StartSizeRangeXUc").y, seed, shaderStartTime, 29f),
                RandomRange(mat.GetVector("_StartSizeRangeYUc").x, mat.GetVector("_StartSizeRangeYUc").y, seed, shaderStartTime, 31f),
                RandomRange(mat.GetVector("_StartSizeRangeZUc").x, mat.GetVector("_StartSizeRangeZUc").y, seed, shaderStartTime, 37f));
            colorMulRgb = mat.HasProperty("_ColorMultiplier")
                ? (Vector3)mat.GetVector("_ColorMultiplier")
                : new Vector3(0.8f, 0.8f, 0.7f);
        }

        lifetime = Mathf.Max(1e-4f, lifetime);
        float particleTime = Mathf.Max(0f, now - shaderStartTime - delay);
        float lifeNorm = Mathf.Clamp01(particleTime / lifetime);
        float sizeMul = SampleL2MeshSizeScale(
            lifeNorm,
            4,
            mat.GetVector("_SizeKey0"),
            mat.GetVector("_SizeKey1"),
            mat.GetVector("_SizeKey2"),
            mat.GetVector("_SizeKey3"),
            mat.GetVector("_SizeKey4"));
        Vector3 finalSizeUe = startSizeUe * sizeMul;

        Vector3 offsetUe = mat.HasProperty("_StartLocationOffsetUe")
            ? (Vector3)mat.GetVector("_StartLocationOffsetUe")
            : Vector3.zero;
        Vector3 locLocalUe = locationUe + offsetUe + velocityUe * particleTime;

        float worldK = mat.HasProperty("_L2FxWorldCalibration") ? mat.GetFloat("_L2FxWorldCalibration") : 1.8f;
        Vector3 motionUnity = DocExtractorSpriteEmitter0MotionSimulator.UcPositionToUnityMeters(locLocalUe, worldK);
        Vector3 locWorldUe = DocExtractorParticleMotionSimulator.UnityWorldToUe(
            renderer.transform.position + motionUnity);

        MeshStartSpinSnapshot startSpin = ReadMeshStartSpinSnapshot(
            mat,
            renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                ? renderer.sharedMaterials[0]
                : null,
            slot,
            group.MeshEmitter3AppRandBaseState);

        Vector3 startSpinC012 = startSpin.HasAppRand ? startSpin.YawPitchRollUru : Vector3.zero;
        Vector3 spinRateC012 = Vector3.zero;
        Vector3 runtimeC012 = new Vector3(
            Mathf.Floor(startSpinC012.x + spinRateC012.x * particleTime),
            Mathf.Floor(startSpinC012.y + spinRateC012.y * particleTime),
            Mathf.Floor(startSpinC012.z + spinRateC012.z * particleTime));
        Vector3 runtimePitchYawRoll = new Vector3(runtimeC012.y, runtimeC012.x, runtimeC012.z);

        float opacity = mat.HasProperty("_Opacity") ? mat.GetFloat("_Opacity") : 0.6f;
        float fadeOutStart = mat.HasProperty("_FadeOutStartTime") ? mat.GetFloat("_FadeOutStartTime") : 0.0375f;
        bool fadeOut = !mat.HasProperty("_FadeOut") || mat.GetFloat("_FadeOut") > 0.5f;
        float fadeOutAmt = fadeOut && particleTime > fadeOutStart
            ? (particleTime - fadeOutStart) / Mathf.Max(1e-4f, lifetime - fadeOutStart)
            : 0f;
        float r = Mathf.Max(0f, 1f * colorMulRgb.x - fadeOutAmt) * opacity;
        float g = Mathf.Max(0f, 1f * colorMulRgb.y - fadeOutAmt) * opacity;
        float b = Mathf.Max(0f, 1f * colorMulRgb.z - fadeOutAmt) * opacity;
        float a = Mathf.Max(0f, 1f - fadeOutAmt);

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
            "MeshEmitter226[{0}] MeshParticle[{1}] Tick{2}{3}",
            0,
            slot,
            session.TickCounter,
            Environment.NewLine);
        string effectName = ResolveShotNAtkEffectName(group.OwnerParticle);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(group) +
            " aEmitterName=ParticleGroup/MeshEmitter226 effect=" + effectName + " spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=3 kind=Mesh name=ShockWave class=MeshEmitter note=L2 e_u505 MeshEmitter layerIndex=3");

        if (isSpawnEvent)
        {
            var spawnBody = new StringBuilder(2048);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "SpawnParticleBegin Unity MeshEmitter226 ShockWave slot={0} spawnTime={1:F6} shaderStartTime={2:F6} seed={3:F6}{4}",
                slot, now, shaderStartTime, seed, Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  appRandStateBeforeSpawn=0x{0:X8} meshSpawnAppRand={1}{2}",
                meshSpawnState, usedMeshSpawnAppRand ? 1 : 0, Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  rawVelocityUU=({0:F6}, {1:F6}, {2:F6}) note=UC X=4 YZ=0; Accel=0 no PTVD{3}",
                velocityUe.x, velocityUe.y, velocityUe.z, Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  spawnPositionUU=({0:F6}, {1:F6}, {2:F6}) note=LocRange zeros no Offset{3}",
                locLocalUe.x, locLocalUe.y, locLocalUe.z, Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  colorMulRgb=({0:F6}, {1:F6}, {2:F6}) lifetimeSec={3:F6} spawnSizeUU=({4:F6},{5:F6},{6:F6}){7}",
                colorMulRgb.x, colorMulRgb.y, colorMulRgb.z, lifetime,
                startSizeUe.x, startSizeUe.y, startSizeUe.z, Environment.NewLine);
            if (startSpin.HasAppRand)
            {
                AppendMeshStartSpinSnapshot(spawnBody, startSpin);
                AppendShockWaveSpinCompare(spawnBody, session, slot, startSpin.YawPitchRollUru.z);
            }

            if (usedMeshSpawnAppRand)
            {
                AppendMeshSpawnRngStream(spawnBody, mat, meshSpawnState);
            }

            spawnBody.AppendLine("SpawnParticleEnd Unity MeshEmitter226 ShockWave slot=" + slot);
            Append(spawnBody.ToString());
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
            "  spinVelocityURU@+0x30=(c0={0:F4},c1={1:F4},c2={2:F4}) note=UC no SPS{3}" +
            "  startRotationURU@+0x3C=(c0={4:F4},c1={5:F4},c2={6:F4}) trunc=({7},{8},{9}){3}" +
            "  runtimeC012=(c0={10:F0},c1={11:F0},c2={12:F0}) FRotationMatrix(Pitch,Yaw,Roll)=({13:F0},{14:F0},{15:F0}){3}",
            spinRateC012.x, spinRateC012.y, spinRateC012.z,
            Environment.NewLine,
            startSpinC012.x, startSpinC012.y, startSpinC012.z,
            (int)startSpinC012.x, (int)startSpinC012.y, (int)startSpinC012.z,
            runtimeC012.x, runtimeC012.y, runtimeC012.z,
            runtimePitchYawRoll.x, runtimePitchYawRoll.y, runtimePitchYawRoll.z);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  colorMultiplier@+0x84=({0:F4}, {1:F4}, {2:F4}) opacity={3:F4} fadeOutStart={4:F4}{5}" +
            "  runtimeColorA8@+0xA8=({6}, {7}, {8}, {9}) note=CPU MeshColorFade+Opacity; compare L2 e_u505 layerIndex=3{5}" +
            "  unityDiag motionUnityMeters=({10:F4}, {11:F4}, {12:F4}) K={13:F3} meshSpawn=0x{14:X8}{5}" +
            "  unityDiag deltaLocLocalUU=({15:F4}, {16:F4}, {17:F4}) dt={18:F4}s{5}" +
            "  compareNote=vs L2 ShockWave: life=0.2 FadeOut@0.0375 size 0.1/0.14 velX=4 StartSpinZ only{5}",
            colorMulRgb.x, colorMulRgb.y, colorMulRgb.z,
            opacity, fadeOutStart,
            Environment.NewLine,
            ToFloorByte(b), ToFloorByte(g), ToFloorByte(r), ToFloorByte(a),
            motionUnity.x, motionUnity.y, motionUnity.z,
            worldK, meshSpawnState,
            deltaLoc.x, deltaLoc.y, deltaLoc.z, dt);
        if (startSpin.HasAppRand)
        {
            AppendMeshStartSpinSnapshot(body, startSpin);
        }

        AppendDirectionDiag(body, group, mat, velocityUe, locLocalUe, deltaLoc);
        Append(body.ToString());
    }

    private static void WriteSpriteEmitter325SmogSample(
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
        if (!DocExtractorSpriteEmitter0MotionSimulator.TryEvaluate(
                group.transform,
                mat,
                now,
                shaderStartTime,
                out DocExtractorSpriteEmitter0MotionSimulator.MotionSample motion))
        {
            if (isSpawnEvent)
            {
                uint state = DocExtractorSpriteEmitter0MotionSimulator.ReadAppRandStateBits(mat);
                Append(string.Format(
                    CultureInfo.InvariantCulture,
                    "SpriteEmitter325 SKIP slot={0} note=TryEvaluate failed appRandState=0x{1:X8}{2}",
                    slot,
                    state,
                    Environment.NewLine));
            }

            return;
        }

        if (!isSpawnEvent && motion.ParticleTime < 1e-4f)
        {
            return;
        }

        if (!session.PrevLocLocalUe.TryGetValue(slot, out Vector3 oldLocalUe))
        {
            oldLocalUe = motion.LocLocalUe;
        }

        Vector3 deltaLoc = motion.LocLocalUe - oldLocalUe;
        session.PrevLocLocalUe[slot] = motion.LocLocalUe;
        session.PrevSampleTimeBySlot.TryGetValue(slot, out float prevSampleTime);
        float dt = prevSampleTime > 0f ? Mathf.Max(1e-4f, now - prevSampleTime) : 0f;
        session.PrevSampleTimeBySlot[slot] = now;

        Vector3 accelUe = mat.HasProperty("_AccelerationUc")
            ? new Vector3(
                mat.GetVector("_AccelerationUc").x,
                mat.GetVector("_AccelerationUc").y,
                mat.GetVector("_AccelerationUc").z)
            : new Vector3(20f, 0f, 30f);
        Vector3 velLossUe = mat.HasProperty("_VelocityLossRangeUc")
            ? new Vector3(
                mat.GetVector("_VelocityLossRangeUc").x,
                mat.GetVector("_VelocityLossRangeUc").y,
                mat.GetVector("_VelocityLossRangeUc").z)
            : new Vector3(0.5f, 0.5f, 0.5f);

        float speedNow = motion.VelocityNowUe.magnitude;
        float speedSpawn = motion.Spawn.VelocityAfterPtvdUe.magnitude;
        float locLen = motion.LocLocalUe.magnitude;

        session.TickCounter += 1;
        L2Particle owner = group.OwnerParticle;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : group.transform;
        string effectName = ResolveShotNAtkEffectName(owner);

        if (isSpawnEvent)
        {
            var spawnBody = new StringBuilder(1200);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "SpawnParticleBegin Unity SpriteEmitter325 Smog slot={0} spawnTime={1:F6} shaderStartTime={2:F6} seed={3:F6}{4}",
                slot, now, shaderStartTime, seed, Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  appRandStateBeforeSpawn=0x{0:X8}{1}",
                motion.Spawn.AppRandStateBeforeSpawn,
                Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  rawVelocityUU=({0:F3}, {1:F3}, {2:F3}) vel0=({3:F3}, {4:F3}, {5:F3}) note=no PTVD; Accel*SpawnDt baked{6}",
                motion.Spawn.RawVelocityUe.x,
                motion.Spawn.RawVelocityUe.y,
                motion.Spawn.RawVelocityUe.z,
                motion.Spawn.VelocityAfterPtvdUe.x,
                motion.Spawn.VelocityAfterPtvdUe.y,
                motion.Spawn.VelocityAfterPtvdUe.z,
                Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  spawnPositionUU=({0:F3}, {1:F3}, {2:F3}) lifetimeSec={3:F4} spawnSizeUU={4:F4}{5}",
                motion.Spawn.SpawnPositionUe.x,
                motion.Spawn.SpawnPositionUe.y,
                motion.Spawn.SpawnPositionUe.z,
                motion.Spawn.LifetimeSeconds,
                motion.Spawn.SpawnSizeUU,
                Environment.NewLine);
            spawnBody.AppendFormat(
                CultureInfo.InvariantCulture,
                "  colorMulRgb=({0:F4}, {1:F4}, {2:F4}) accel=({3:F1},{4:F1},{5:F1}) velLoss=({6:F2},{7:F2},{8:F2}){9}",
                motion.Spawn.ColorMulRgb.x,
                motion.Spawn.ColorMulRgb.y,
                motion.Spawn.ColorMulRgb.z,
                accelUe.x, accelUe.y, accelUe.z,
                velLossUe.x, velLossUe.y, velLossUe.z,
                Environment.NewLine);
            AppendDirectionDiag(
                spawnBody,
                group,
                mat,
                motion.Spawn.VelocityAfterPtvdUe,
                motion.Spawn.SpawnPositionUe,
                motion.Spawn.VelocityAfterPtvdUe);
            spawnBody.AppendLine("SpawnParticleEnd Unity SpriteEmitter325 Smog slot=" + slot);
            Append(spawnBody.ToString());
        }

        var body = new StringBuilder(1400);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpriteEmitter325[{0}] Particle[{1}] Tick{2}{3}",
            1,
            slot,
            session.TickCounter,
            Environment.NewLine);
        body.AppendLine(
            "  aEmitter=" + FormatPointer(group) +
            " aEmitterName=ParticleGroup/SpriteEmitter325 effect=" + effectName + " spawnKind=self");
        body.AppendLine(
            "  subEmitter=" + FormatPointer(renderer) +
            " layerIndex=1 kind=Sprite name=Smog class=SpriteEmitter note=L2 e_u505 SpriteEmitter layerIndex=1");
        body.AppendLine("  caster=" + FormatPointer(caster) + " sourceActor=" + FormatPointer(caster));
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locLocal=({0:F3}, {1:F3}, {2:F3}) |loc|={3:F3} oldLocal=({4:F3}, {5:F3}, {6:F3}){7}",
            motion.LocLocalUe.x, motion.LocLocalUe.y, motion.LocLocalUe.z, locLen,
            oldLocalUe.x, oldLocalUe.y, oldLocalUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  locWorld=({0:F2}, {1:F2}, {2:F2}){3}",
            motion.LocWorldUe.x, motion.LocWorldUe.y, motion.LocWorldUe.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  size=({0:F3}, {1:F3}, {2:F3}) spawnSizeUU={3:F4}{4}",
            motion.SizeUe.x, motion.SizeUe.y, motion.SizeUe.z,
            motion.Spawn.SpawnSizeUU,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  velocityNow=({0:F3}, {1:F3}, {2:F3}) speed={3:F3} speedSpawn={4:F3}{5}",
            motion.VelocityNowUe.x, motion.VelocityNowUe.y, motion.VelocityNowUe.z,
            speedNow, speedSpawn,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  acceleration=({0:F1}, {1:F1}, {2:F1}) particleTime={3:F4} maxLife={4:F4} lifeNorm={5:F4} dt={6:F4}s{7}",
            accelUe.x, accelUe.y, accelUe.z,
            motion.ParticleTime, motion.MaxLifetime, motion.AgeNorm, dt,
            Environment.NewLine);
        AppendDirectionDiag(body, group, mat, motion.VelocityNowUe, motion.LocLocalUe, deltaLoc);
        Append(body.ToString());
    }

    /// <summary>
    /// Fixed axis contract + motion path for ShockWave mesh vs Smog sprite.
    /// UE (X,Y,Z) -&gt; Unity OS (X,Z,Y) via L2Fx_UcPositionToUnityMeters / UcPositionToUnityMeters.
    /// Cone + travel axis = UE +X = Unity OS +X (= emitter.right when rotation identity).
    /// shockwave00 bounds: thin on X, disc in YZ — mesh opening axis matches travel axis.
    /// Smog: camera billboard (no mesh forward); only the pivot uses the same OS motion.
    /// </summary>
    private static void AppendDirectionDiag(
        StringBuilder body,
        ParticleGroup group,
        Material mat,
        Vector3 velUe,
        Vector3 locLocalUe,
        Vector3 deltaLocUe)
    {
        if (body == null || group == null)
        {
            return;
        }

        Transform emitter = group.transform;
        L2Particle owner = group.OwnerParticle;
        Transform caster = group.FollowTarget != null
            ? group.FollowTarget
            : owner != null
                ? owner.transform
                : emitter;

        string layerTag = "unknown";
        string meshNote = "n/a";
        if (!string.IsNullOrEmpty(group.name))
        {
            if (group.name.IndexOf("MeshEmitter226", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                layerTag = "ShockWave/Mesh";
                meshNote = "shockwave00 thin=X disc=YZ; shader scale (sx,sz,sy); spin Roll around travel";
            }
            else if (group.name.IndexOf("SpriteEmitter325", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                layerTag = "Smog/Sprite";
                meshNote = "Unity Quad + camera billboard; pivot only follows motionOS";
            }
        }

        float worldK = mat != null && mat.HasProperty("_L2FxWorldCalibration")
            ? mat.GetFloat("_L2FxWorldCalibration")
            : 1.8f;

        // Same path as L2Fx_UcPositionToUnityMeters: (ue.x, ue.z, ue.y) * UU/52.5 (no size K)
        Vector3 velOs = DocExtractorSpriteEmitter0MotionSimulator.UcPositionToUnityMeters(velUe, worldK);
        Vector3 deltaOs = DocExtractorSpriteEmitter0MotionSimulator.UcPositionToUnityMeters(deltaLocUe, worldK);
        Vector3 locOs = DocExtractorSpriteEmitter0MotionSimulator.UcPositionToUnityMeters(locLocalUe, worldK);

        Vector3 velWorld = emitter.TransformDirection(velOs);
        Vector3 deltaWorld = emitter.TransformDirection(deltaOs);
        Vector3 locWorldOffset = emitter.TransformDirection(locOs);

        Vector3 emitterRight = emitter.right;
        Vector3 emitterUp = emitter.up;
        Vector3 emitterFwd = emitter.forward;
        Vector3 casterFwd = caster != null ? caster.forward : Vector3.forward;
        Vector3 casterRight = caster != null ? caster.right : Vector3.right;

        // Fixed cone/travel axis in world = emitter local +X (UE +X after axis swap).
        Vector3 coneAxisWorld = emitterRight;

        float velMag = velWorld.magnitude;
        Vector3 velN = velMag > 1e-6f ? velWorld / velMag : Vector3.zero;
        float lateralYzUe = Mathf.Sqrt(velUe.y * velUe.y + velUe.z * velUe.z);
        float angVsConeDeg = velMag > 1e-6f ? Vector3.Angle(velWorld, coneAxisWorld) : -1f;
        float dotCone = Vector3.Dot(velN, coneAxisWorld);
        float halfConeDeg = Mathf.Abs(velUe.x) > 1e-6f
            ? Mathf.Atan2(lateralYzUe, Mathf.Abs(velUe.x)) * Mathf.Rad2Deg
            : (lateralYzUe > 1e-6f ? 90f : 0f);
        string coneVerdict = ClassifyDirVsConeAxis(angVsConeDeg);

        float angVsCaster = velMag > 1e-6f ? Vector3.Angle(velWorld, casterFwd) : -1f;
        float dotCaster = Vector3.Dot(velN, casterFwd);
        float dotCasterRight = Vector3.Dot(velN, casterRight);
        string casterVerdict = ClassifyDirVsCaster(dotCaster, angVsCaster);

        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  axisMap layer={0} K={1:F3} UE(x,y,z)->OS(x,z,y) coneTravel=UE+X=OS+X=emitter.right{2}" +
            "  axisMap meshNote={3}{2}" +
            "  axisMap emitterOS right=({4:F3},{5:F3},{6:F3}) up=({7:F3},{8:F3},{9:F3}) " +
            "fwd=({10:F3},{11:F3},{12:F3}){2}" +
            "  axisMap casterFwd=({13:F3},{14:F3},{15:F3}) casterRight=({16:F3},{17:F3},{18:F3}){2}" +
            "  movePath velUe=({19:F2},{20:F2},{21:F2}) -> velOS=({22:F4},{23:F4},{24:F4}) " +
            "-> velWorld=({25:F3},{26:F3},{27:F3}){2}" +
            "  movePath locUe=({28:F3},{29:F3},{30:F3}) -> locOS=({31:F4},{32:F4},{33:F4}) " +
            "-> locWorldOfs=({34:F3},{35:F3},{36:F3}){2}" +
            "  movePath deltaUe=({37:F3},{38:F3},{39:F3}) deltaWorld=({40:F3},{41:F3},{42:F3}){2}" +
            "  coneDiag lateralYZ_Ue={43:F2} halfConeDeg={44:F1} angVsConeXDeg={45:F1} " +
            "dotConeAxis={46:F3} verdict={47}{2}" +
            "  casterDiag angVsCasterDeg={48:F1} dotCasterFwd={49:F3} dotCasterRight={50:F3} " +
            "verdict={51}{2}",
            layerTag,
            worldK,
            Environment.NewLine,
            meshNote,
            emitterRight.x, emitterRight.y, emitterRight.z,
            emitterUp.x, emitterUp.y, emitterUp.z,
            emitterFwd.x, emitterFwd.y, emitterFwd.z,
            casterFwd.x, casterFwd.y, casterFwd.z,
            casterRight.x, casterRight.y, casterRight.z,
            velUe.x, velUe.y, velUe.z,
            velOs.x, velOs.y, velOs.z,
            velWorld.x, velWorld.y, velWorld.z,
            locLocalUe.x, locLocalUe.y, locLocalUe.z,
            locOs.x, locOs.y, locOs.z,
            locWorldOffset.x, locWorldOffset.y, locWorldOffset.z,
            deltaLocUe.x, deltaLocUe.y, deltaLocUe.z,
            deltaWorld.x, deltaWorld.y, deltaWorld.z,
            lateralYzUe, halfConeDeg, angVsConeDeg, dotCone, coneVerdict,
            angVsCaster, dotCaster, dotCasterRight, casterVerdict);
    }

    private static string ClassifyDirVsConeAxis(float angVsConeDeg)
    {
        if (angVsConeDeg < 0f)
        {
            return "NO_MOTION";
        }

        if (angVsConeDeg <= 25f)
        {
            return "ON_CONE_AXIS";
        }

        if (angVsConeDeg <= 55f)
        {
            return "IN_CONE";
        }

        return "WIDE_OFF_AXIS";
    }

    private static string ClassifyDirVsCaster(float dotVelCaster, float angVelCasterDeg)
    {
        if (angVelCasterDeg < 0f)
        {
            return "NO_MOTION";
        }

        if (dotVelCaster > 0.5f)
        {
            return "WITH_CASTER_FWD";
        }

        if (dotVelCaster < -0.5f)
        {
            return "AGAINST_CASTER_FWD";
        }

        return "SIDEWAYS_VS_CASTER";
    }

    private static float SampleL2MeshSizeScale(
        float lifeNorm,
        int sizeScaleCount,
        Vector4 key0,
        Vector4 key1,
        Vector4 key2,
        Vector4 key3,
        Vector4 key4)
    {
        if (sizeScaleCount <= 0)
        {
            return 1f;
        }

        Vector4[] keys = { key0, key1, key2, key3, key4 };
        float sp = Mathf.Repeat(lifeNorm, 1f);
        int idx = 0;
        while (idx < sizeScaleCount && keys[idx].x < sp)
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
            nextS = keys[0].y;
            nextT = keys[0].x;
        }
        else
        {
            prevS = keys[idx - 1].y;
            prevT = keys[idx - 1].x;
            if (idx < sizeScaleCount)
            {
                nextS = keys[idx].y;
                nextT = keys[idx].x;
            }
            else
            {
                nextS = prevS;
                nextT = prevT + 1e-4f;
            }
        }

        if (Mathf.Abs(nextT - prevT) < 1e-4f)
        {
            return prevS;
        }

        float ts = (sp - prevT) / (nextT - prevT);
        return Mathf.Lerp(prevS, nextS, ts);
    }

    private static float RandomRange(float min, float max, float seed, float startTime, float salt)
    {
        float hash = Mathf.Repeat(Mathf.Sin((seed * 17f) + (startTime * 31f) + salt) * 43758.5453123f, 1f);
        return Mathf.Lerp(min, max, hash);
    }

    private static void AppendShockWaveSpinCompare(
        StringBuilder body,
        GroupSession session,
        int slot,
        float rollUru)
    {
        session.StartSpinRollUruBySlot[slot] = rollUru;
        if (session.StartSpinRollUruBySlot.Count < 2)
        {
            body.AppendLine(
                "  ringSpinCompare=waitingOtherSlot note=Roll is ring plane; |dRoll| small => rings stack as one");
            return;
        }

        float otherRoll = 0f;
        int otherSlot = -1;
        foreach (KeyValuePair<int, float> pair in session.StartSpinRollUruBySlot)
        {
            if (pair.Key == slot)
            {
                continue;
            }

            otherSlot = pair.Key;
            otherRoll = pair.Value;
            break;
        }

        float dRoll = rollUru - otherRoll;
        float absD = Mathf.Abs(dRoll);
        float wrapAbs = Mathf.Min(absD, 65535f - absD);
        string look = wrapAbs < 8000f
            ? "STACKED_LOOKS_LIKE_ONE"
            : (wrapAbs > 20000f ? "CLEARLY_TWO_RINGS" : "PARTIAL_SEPARATION");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  ringSpinCompare slot={0} rollURU={1:F1} vs slot={2} rollURU={3:F1} " +
            "|dRoll|={4:F1} wrapMin={5:F1} look={6}{7}",
            slot,
            rollUru,
            otherSlot,
            otherRoll,
            absD,
            wrapAbs,
            look,
            Environment.NewLine);
    }

    private static void AppendMeshSpawnRngStream(StringBuilder body, Material mat, uint stateBeforeVelocity)
    {
        uint state = stateBeforeVelocity;
        Vector2 velX = ReadMatMinMax(mat, "_StartVelocityRangeXUc", 4f, 4f);
        Vector2 velY = ReadMatMinMax(mat, "_StartVelocityRangeYUc", 0f, 0f);
        Vector2 velZ = ReadMatMinMax(mat, "_StartVelocityRangeZUc", 0f, 0f);
        Vector2 locX = ReadMatMinMax(mat, "_StartLocationRangeXUc", 0f, 0f);
        Vector2 locY = ReadMatMinMax(mat, "_StartLocationRangeYUc", 0f, 0f);
        Vector2 locZ = ReadMatMinMax(mat, "_StartLocationRangeZUc", 0f, 0f);
        Vector4 colorMul = mat != null && mat.HasProperty("_ColorMultiplier")
            ? mat.GetVector("_ColorMultiplier")
            : new Vector4(0.8f, 0.8f, 0.7f, 0f);
        Vector2 life = ReadMatMinMax(mat, "_LifetimeRange", 0.2f, 0.2f);
        Vector2 delay = ReadMatMinMax(mat, "_InitialDelayRange", 0f, 0f);
        Vector2 sizeX = ReadMatMinMax(mat, "_StartSizeRangeXUc", 0.1f, 0.1f);
        Vector2 sizeY = ReadMatMinMax(mat, "_StartSizeRangeYUc", 0.14f, 0.14f);
        Vector2 sizeZ = ReadMatMinMax(mat, "_StartSizeRangeZUc", 0.14f, 0.14f);
        Vector4 yawRange = mat != null && mat.HasProperty("_StartSpinYawRangeUc")
            ? mat.GetVector("_StartSpinYawRangeUc")
            : Vector4.zero;
        Vector4 pitchRange = mat != null && mat.HasProperty("_StartSpinPitchRangeUc")
            ? mat.GetVector("_StartSpinPitchRangeUc")
            : Vector4.zero;
        Vector4 rollRange = mat != null && mat.HasProperty("_StartSpinRollRangeUc")
            ? mat.GetVector("_StartSpinRollRangeUc")
            : new Vector4(0f, 1f, 0f, 0f);

        var draws = new StringBuilder(1800);
        body.AppendLine(
            "  rngStream draws=28 scopes=12 truncated=0 note=CPU MeshSpawn mirror vs L2 SpawnSoulShotShockWaveCapture");

        AppendRngVectorScope(body, draws, 0, "StartVelocityRange", 0x3A0, 0, velX, velY, velZ, ref state);
        AppendRngVectorScope(body, draws, 1, "StartLocationRange", 0x158, 3, locX, locY, locZ, ref state);
        AppendRngScalarScope(body, draws, 2, "Mesh/OtherScalar", 0x2FC, 6, 0f, 1f, ref state);
        AppendRngVectorScope(body, draws, 3, "UnusedRangeVectorA", 0x1FC, 7, Vector2.zero, Vector2.zero, Vector2.zero, ref state);
        AppendRngVectorScope(body, draws, 4, "UnusedRangeVectorB", 0x214, 10, Vector2.zero, Vector2.zero, Vector2.zero, ref state);
        AppendRngVectorScope(
            body,
            draws,
            5,
            "ColorMultiplierRange",
            0xB8,
            13,
            new Vector2(colorMul.x, colorMul.x),
            new Vector2(colorMul.y, colorMul.y),
            new Vector2(colorMul.z, colorMul.z),
            ref state);
        AppendRngScalarScope(body, draws, 6, "LifetimeRange", 0x380, 16, life.x, life.y, ref state);
        AppendRngScalarScope(body, draws, 7, "InitialDelayRange", 0x378, 17, delay.x, delay.y, ref state);
        AppendRngScalarScope(body, draws, 8, "StartVelocityRadialRange", 0x198, 18, 1f, 1f, ref state);
        AppendRngVectorScope(body, draws, 9, "StartSizeRange", 0x2CC, 19, sizeX, sizeY, sizeZ, ref state);

        uint spinBefore = state;
        float roll = StepL2Range(rollRange.x, rollRange.y, ref state, out uint d22Before, out uint d22App, out uint d22After);
        float pitch = StepL2Range(pitchRange.x, pitchRange.y, ref state, out uint d23Before, out uint d23App, out uint d23After);
        float yaw = StepL2Range(yawRange.x, yawRange.y, ref state, out uint d24Before, out uint d24App, out uint d24After);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "    scope[10] StartSpinRange emitter+0x278 vector draws=[22,25) value=({0:F9},{1:F9},{2:F9}) " +
            "URU trunc=({3},{4},{5}) note=Roll=ring{6}",
            yaw,
            pitch,
            roll,
            Mathf.FloorToInt(yaw * SpinUcToUru),
            Mathf.FloorToInt(pitch * SpinUcToUru),
            Mathf.FloorToInt(roll * SpinUcToUru),
            Environment.NewLine);
        AppendRngDraw(draws, 22, d22Before, d22App, d22After);
        AppendRngDraw(draws, 23, d23Before, d23App, d23After);
        AppendRngDraw(draws, 24, d24Before, d24App, d24After);

        float spsZ = StepL2Range(0f, 0f, ref state, out uint d25Before, out uint d25App, out uint d25After);
        float spsY = StepL2Range(0f, 0f, ref state, out uint d26Before, out uint d26App, out uint d26After);
        float spsX = StepL2Range(0f, 0f, ref state, out uint d27Before, out uint d27App, out uint d27After);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "    scope[11] SpinsPerSecondRange emitter+0x260 vector draws=[25,28) value=({0:F9},{1:F9},{2:F9}){3}",
            spsX,
            spsY,
            spsZ,
            Environment.NewLine);
        AppendRngDraw(draws, 25, d25Before, d25App, d25After);
        AppendRngDraw(draws, 26, d26Before, d26App, d26After);
        AppendRngDraw(draws, 27, d27Before, d27App, d27After);

        body.Append(draws);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  rngSpinEntryState=0x{0:X8} note=before StartSpin draw22; compare L2 StartSpinRange scope{1}",
            spinBefore,
            Environment.NewLine);
    }

    private static Vector2 ReadMatMinMax(Material mat, string property, float defMin, float defMax)
    {
        if (mat != null && mat.HasProperty(property))
        {
            Vector4 v = mat.GetVector(property);
            return new Vector2(v.x, v.y);
        }

        return new Vector2(defMin, defMax);
    }

    private static void AppendRngVectorScope(
        StringBuilder body,
        StringBuilder draws,
        int scopeIndex,
        string name,
        int emitterOffset,
        int drawStart,
        Vector2 xRange,
        Vector2 yRange,
        Vector2 zRange,
        ref uint state)
    {
        float z = StepL2Range(zRange.x, zRange.y, ref state, out uint b0, out uint a0, out uint aft0);
        float y = StepL2Range(yRange.x, yRange.y, ref state, out uint b1, out uint a1, out uint aft1);
        float x = StepL2Range(xRange.x, xRange.y, ref state, out uint b2, out uint a2, out uint aft2);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "    scope[{0}] {1} emitter+0x{2:X} vector draws=[{3},{4}) value=({5:F9},{6:F9},{7:F9}){8}",
            scopeIndex,
            name,
            emitterOffset,
            drawStart,
            drawStart + 3,
            x,
            y,
            z,
            Environment.NewLine);
        AppendRngDraw(draws, drawStart, b0, a0, aft0);
        AppendRngDraw(draws, drawStart + 1, b1, a1, aft1);
        AppendRngDraw(draws, drawStart + 2, b2, a2, aft2);
    }

    private static void AppendRngScalarScope(
        StringBuilder body,
        StringBuilder draws,
        int scopeIndex,
        string name,
        int emitterOffset,
        int drawStart,
        float min,
        float max,
        ref uint state)
    {
        float value = StepL2Range(min, max, ref state, out uint before, out uint appRand, out uint after);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "    scope[{0}] {1} emitter+0x{2:X} scalar draws=[{3},{4}) range=[{5:F9},{6:F9}] value={7:F9}{8}",
            scopeIndex,
            name,
            emitterOffset,
            drawStart,
            drawStart + 1,
            min,
            max,
            value,
            Environment.NewLine);
        AppendRngDraw(draws, drawStart, before, appRand, after);
    }

    private static float StepL2Range(
        float min,
        float max,
        ref uint state,
        out uint before,
        out uint appRand,
        out uint after)
    {
        before = state;
        state = unchecked(state * AppRandMultiplier + AppRandIncrement);
        after = state;
        appRand = (state >> 16) & 0x7fffu;
        float frand = appRand / AppFrandDivisor;
        return frand * (min - max) + max;
    }

    private static void AppendRngDraw(StringBuilder body, int index, uint before, uint appRand, uint after)
    {
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "    draw[{0}] before=0x{1:X8} appRand={2} after=0x{3:X8}{4}",
            index,
            before,
            appRand,
            after,
            Environment.NewLine);
    }

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

    private static string ResolveShotNAtkEffectName(L2Particle owner)
    {
        if (owner != null && !string.IsNullOrEmpty(owner.name))
        {
            if (owner.name.IndexOf("e_u505", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "LineageEffect.e_u505_c";
            }

            return "UnityEffect." + owner.name;
        }

        return "UnityEffect.shot_N_atk";
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
            OpenEffectOwnerId = 0;
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var body = new StringBuilder(384);
                body.AppendLine(
                    "Unity_ParticleSnapshot.log — MeshEmitter226 ShockWave + SpriteEmitter325 Smog");
                body.AppendLine(
                    "axisContract UE(x,y,z)->OS(x,z,y); coneTravel=UE+X=OS+X=emitter.right");
                body.AppendLine(
                    "compare coneDiag (ON_CONE/IN_CONE) ShockWave vs Smog; casterDiag secondary");
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
