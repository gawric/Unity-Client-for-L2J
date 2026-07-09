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

    public static bool Enabled = true;

    private static readonly object WriteLock = new object();
    private static readonly Dictionary<int, GroupSession> Sessions = new Dictionary<int, GroupSession>();

    private sealed class GroupSession
    {
        public bool Open;
        public int TickCounter;
        public float LastSampleTime = -999f;
        public readonly Dictionary<int, Vector3> PrevLocLocalUe = new Dictionary<int, Vector3>();
        public readonly Dictionary<int, Vector3> PrevLocWorldUe = new Dictionary<int, Vector3>();
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

        return ParticleGroupLifetimeDebug.ShouldTraceUpline(group.name, group.OwnerParticle, group.transform);
    }

    public static void OnPlayPart(ParticleGroup group)
    {
        if (!ShouldTrace(group))
        {
            return;
        }

        BeginNewLogFile();
        OpenEffectSession(group);
    }

    private static void OpenEffectSession(ParticleGroup group)
    {
        GroupSession session = GetOrCreateSession(group);
        session.Open = true;
        session.TickCounter = 0;
        session.LastSampleTime = -999f;
        session.PrevLocLocalUe.Clear();
        session.PrevLocWorldUe.Clear();

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
        body.AppendLine(
            "EFFECT SESSION aEmitter=" + aEmitterHex +
            " aEmitterName=? effect=" + effectName + " spawnKind=self");
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

    public static void OnSlotOff(ParticleGroup group, int slot)
    {
        if (!ShouldTrace(group))
        {
            return;
        }

        GroupSession session = GetOrCreateSession(group);
        session.PrevLocLocalUe.Remove(slot);
        session.PrevLocWorldUe.Remove(slot);
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

    private static int ToByte(float linear01)
    {
        int v = Mathf.RoundToInt(linear01 * 255f);
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
                    "Unity_ParticleSnapshot.log — new session (ParticleGroup::FixedUpdate hook installed)");
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
