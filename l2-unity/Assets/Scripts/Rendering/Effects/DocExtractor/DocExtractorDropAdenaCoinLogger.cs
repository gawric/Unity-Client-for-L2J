#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// CPU mirror of MeshEmitter7 Coin spawn/spin vs LIVE SpawnParticleSnapshot.log.
/// Writes Unity_DropAdenaCoin.log next to the Interlude capture.
/// </summary>
public static class DocExtractorDropAdenaCoinLogger
{
    public const string UnityLogPath =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\Unity_DropAdenaCoin.log";
    public const string LiveLogPath =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\SpawnParticleSnapshot.log";

    public const float LiveSlot0AgeSeconds = 0.0059466f;
    public const uint LiveSlot0StateBeforeVelocity = 0x918511C5u;

    public static bool Enabled = true;

    private static readonly object WriteLock = new object();
    private static bool _fileStarted;
    private static bool _liveReplayWritten;
    private static int _unitySlotsLogged;

    public static bool ShouldTrace(ParticleGroup group)
    {
        if (!Enabled || group == null || string.IsNullOrEmpty(group.name))
            return false;
        if (group.name.IndexOf("MeshEmitter7", StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        Transform current = group.transform;
        for (int depth = 0; current != null && depth < 16; depth++, current = current.parent)
        {
            if (!string.IsNullOrEmpty(current.name) &&
                current.name.IndexOf("e_u056_a", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static void OnPlayPart(ParticleGroup group)
    {
        if (!ShouldTrace(group))
            return;

        _unitySlotsLogged = 0;
        BeginFile();
        Material mat = ReadSharedMaterial(group);
        var body = new StringBuilder(1200);
        body.AppendLine("--------------------------------------------------------------------------------");
        body.AppendLine(
            "UNITY Coin session group=" + group.name +
            " meshBase=pending-until-burst material=" + (mat != null ? mat.name : "null"));
        AppendMaterialFingerprint(body, mat);
        Append(body.ToString());
    }

    public static void OnBurst(ParticleGroup group, uint meshBase, int slotCount, float now, float shaderStartTime)
    {
        if (!ShouldTrace(group) || slotCount <= 0)
            return;

        Material mat = ReadSharedMaterial(group);
        BeginFile();
        var body = new StringBuilder(4000);
        body.AppendLine("--------------------------------------------------------------------------------");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "UNITY Coin burst meshBase=0x{0:X8} slots={1} now={2:F6} shaderStart={3:F6}{4}",
            meshBase,
            slotCount,
            now,
            shaderStartTime,
            Environment.NewLine);
        AppendMaterialFingerprint(body, mat);
        int logCount = Mathf.Min(slotCount, 10);
        for (int slot = 0; slot < logCount; slot++)
        {
            float age = Mathf.Max(0f, now - shaderStartTime);
            AppendUnitySlot(body, mat, meshBase, slot, age);
        }

        if (!_liveReplayWritten)
        {
            AppendLiveReplayCompare(body, mat);
            _liveReplayWritten = true;
        }

        Append(body.ToString());
        _unitySlotsLogged += logCount;
        Debug.Log(
            "[DropAdenaCoin] wrote " + logCount + " Unity slots to " + UnityLogPath);
    }

    public static void OnSlot(ParticleGroup group, int slot, uint meshBase, float now, float shaderStartTime)
    {
        if (!ShouldTrace(group) || _unitySlotsLogged >= 10)
            return;

        Material mat = ReadSharedMaterial(group);
        BeginFile();
        var body = new StringBuilder(1600);
        if (_unitySlotsLogged == 0)
        {
            body.AppendLine("--------------------------------------------------------------------------------");
            body.AppendFormat(
                CultureInfo.InvariantCulture,
                "UNITY Coin sequential meshBase=0x{0:X8}{1}",
                meshBase,
                Environment.NewLine);
            AppendMaterialFingerprint(body, mat);
        }

        AppendUnitySlot(body, mat, meshBase, slot, Mathf.Max(0f, now - shaderStartTime));
        _unitySlotsLogged++;
        if (_unitySlotsLogged == 1 && !_liveReplayWritten)
        {
            AppendLiveReplayCompare(body, mat);
            _liveReplayWritten = true;
        }
        Append(body.ToString());
    }

    private static Material ReadSharedMaterial(ParticleGroup group)
    {
        if (group == null)
            return null;

        Renderer[] renderers = group.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].sharedMaterial != null)
                return renderers[i].sharedMaterial;
        }

        return null;
    }

    private static void AppendMaterialFingerprint(StringBuilder body, Material mat)
    {
        if (mat == null)
        {
            body.AppendLine("  material=missing");
            return;
        }

        Vector4 accel = ReadVec(mat, "_AccelerationUc");
        Vector4 offset = ReadVec(mat, "_StartLocationOffsetUc");
        Vector4 velZ = ReadVec(mat, "_StartVelocityRangeZUc");
        Vector4 sizeX = ReadVec(mat, "_StartSizeRangeXUc");
        Vector4 sps = ReadVec(mat, "_SpsYawRangeUc");
        Vector4 ccw = ReadVec(mat, "_SpinCCWorCW");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  UNITY fingerprint SpawnMode={0:F0} SpinSpsMode={1:F0} SpinParticles={2:F0} SizeMode={3:F0}{4}",
            ReadFloat(mat, "_SpawnMode"),
            ReadFloat(mat, "_SpinSpsMode"),
            ReadFloat(mat, "_SpinParticles"),
            ReadFloat(mat, "_SizeMode"),
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  AccelZ={0:F1} OffsetZ={1:F1} SizeX={2:F1} VelZ={3:F1}..{4:F1} SpsYaw={5:F1}..{6:F1} CCW=({7:F2},{8:F2},{9:F2}){10}",
            accel.z,
            offset.z,
            sizeX.x,
            velZ.x,
            velZ.y,
            sps.x,
            sps.y,
            ccw.x,
            ccw.y,
            ccw.z,
            Environment.NewLine);
        body.AppendLine(
            "  LIVE expect AccelZ=-200 OffsetZ=27.1 Size=2.5 VelZ=30..80 Sps=0..5 CCW=(0.50,0.50,0.50) MaxParticles=10");
        bool fingerprintOk =
            Approx(accel.z, -200f) &&
            Approx(offset.z, 27.1f) &&
            Approx(sizeX.x, 2.5f) &&
            Approx(velZ.x, 30f) &&
            Approx(velZ.y, 80f) &&
            Approx(sps.x, 0f) &&
            Approx(sps.y, 5f) &&
            Approx(ccw.x, 0.5f) &&
            Approx(ccw.y, 0.5f) &&
            Approx(ccw.z, 0.5f) &&
            ReadFloat(mat, "_SpawnMode") > 1.5f &&
            ReadFloat(mat, "_SpawnMode") < 2.5f &&
            ReadFloat(mat, "_SpinSpsMode") > 0.5f;
        body.AppendLine("  fingerprintVsLive=" + (fingerprintOk ? "MATCH" : "MISMATCH"));
    }

    private static void AppendUnitySlot(
        StringBuilder body,
        Material mat,
        uint meshBase,
        int slot,
        float ageSeconds)
    {
        CoinSlot cpu = EvaluateSlot(mat, meshBase, slot, ageSeconds);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "SpawnUnityCoinCapture slotIndex={0} meshBase=0x{1:X8} velState=0x{2:X8} spinState=0x{3:X8} age={4:F6}{5}",
            slot,
            meshBase,
            cpu.VelocityState,
            cpu.StartSpinState,
            ageSeconds,
            Environment.NewLine);
        AppendSlotBody(body, cpu, "UNITY");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  ccwSelfCheck flips=({0},{1},{2}) frandLtCcw=({3},{4},{5}) ok={6}{7}",
            cpu.FlipX ? 1 : 0,
            cpu.FlipY ? 1 : 0,
            cpu.FlipZ ? 1 : 0,
            cpu.FrandX < cpu.Ccw.x ? 1 : 0,
            cpu.FrandY < cpu.Ccw.y ? 1 : 0,
            cpu.FrandZ < cpu.Ccw.z ? 1 : 0,
            cpu.CcwSelfOk ? "yes" : "NO",
            Environment.NewLine);
    }

    private static void AppendLiveReplayCompare(StringBuilder body, Material mat)
    {
        LiveCoinCapture live = TryParseFirstLiveCoinSlot(LiveLogPath);
        uint replayState = live.HasCapture ? live.StateBeforeVelocity : LiveSlot0StateBeforeVelocity;
        CoinSlot replay = EvaluateSlot(mat, replayState, 0, LiveSlot0AgeSeconds);

        body.AppendLine("--------------------------------------------------------------------------------");
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "LIVE-REPLAY CPU mirror stateBeforeVel=0x{0:X8} age={1:F7} source={2}{3}",
            replayState,
            LiveSlot0AgeSeconds,
            live.HasCapture ? LiveLogPath : "hardcoded slot0 2026-08-24",
            Environment.NewLine);
        body.AppendLine(
            "  note=LIVE finalSpawnLocation is SpawnParticle Euler: Vel+=Accel*dt, Loc+=Vel*dt (post-accel). spawnLocationT0=Offset+LocRange.");
        AppendSlotBody(body, replay, "REPLAY");

        if (!live.HasCapture)
        {
            live = HardcodedLiveSlot0();
            body.AppendLine("  note=LIVE log parse missed first Coin block; using hardcoded slot 0");
        }

        int mismatches = 0;
        mismatches += CompareVec3(body, "rawVelocity", replay.Velocity, live.Velocity);
        mismatches += CompareVec3(body, "locRangeGetRand", replay.Location, live.Location);
        mismatches += CompareVec3(body, "spawnLocationT0", replay.SpawnLocation, live.SpawnLocation);
        mismatches += CompareVec3(body, "finalVelocityAfterSpawn", replay.VelocityAfterSpawn, live.VelocityAfterSpawn);
        mismatches += CompareVec3(body, "finalSpawnLocation", replay.FinalLocation, live.FinalLocation);
        mismatches += CompareVec3(body, "startSpinUC", replay.StartSpinUc, live.StartSpinUc);
        mismatches += CompareVec3(body, "spsUC_GetRand", replay.SpsUc, live.SpsUc);
        mismatches += CompareVec3(body, "slotStartSpinURU", replay.StartSpinUru, live.StartSpinUru);
        mismatches += CompareVec3(body, "slotSpsURU", replay.SpsUru, live.SpsUru);
        mismatches += CompareVec3(body, "memURU", replay.MemUru, live.MemUru);
        mismatches += CompareVec3(body, "RotationURU_swap", replay.SwappedUru, live.SwappedUru);
        body.AppendLine(
            mismatches == 0
                ? "  LIVE-REPLAY verdict=MATCH all Coin slot0 fields"
                : "  LIVE-REPLAY verdict=MISMATCH fields=" + mismatches);
        Debug.Log(
            mismatches == 0
                ? "[DropAdenaCoin] LIVE replay MATCH slot0"
                : "[DropAdenaCoin] LIVE replay MISMATCH fields=" + mismatches + " log=" + UnityLogPath);
    }

    private static int CompareVec3(StringBuilder body, string name, Vector3 unity, Vector3 live)
    {
        bool ok =
            Approx(unity.x, live.x, 0.02f) &&
            Approx(unity.y, live.y, 0.02f) &&
            Approx(unity.z, live.z, 0.02f);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  cmp {0} UNITY=({1:F6},{2:F6},{3:F6}) LIVE=({4:F6},{5:F6},{6:F6}) {7}{8}",
            name,
            unity.x, unity.y, unity.z,
            live.x, live.y, live.z,
            ok ? "MATCH" : "DIFF",
            Environment.NewLine);
        return ok ? 0 : 1;
    }

    private static void AppendSlotBody(StringBuilder body, CoinSlot slot, string tag)
    {
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} rawVelocity=({1:F9},{2:F9},{3:F9}){4}",
            tag, slot.Velocity.x, slot.Velocity.y, slot.Velocity.z, Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} locRange=({1:F9},{2:F9},{3:F9}) spawnLocationT0=({4:F9},{5:F9},{6:F9}) size=({7:F3},{8:F3},{9:F3}){10}",
            tag,
            slot.Location.x, slot.Location.y, slot.Location.z,
            slot.SpawnLocation.x, slot.SpawnLocation.y, slot.SpawnLocation.z,
            slot.Size.x, slot.Size.y, slot.Size.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} velAfterSpawn=({1:F9},{2:F9},{3:F9}) finalSpawnLocation=({4:F9},{5:F9},{6:F9}){7}",
            tag,
            slot.VelocityAfterSpawn.x, slot.VelocityAfterSpawn.y, slot.VelocityAfterSpawn.z,
            slot.FinalLocation.x, slot.FinalLocation.y, slot.FinalLocation.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} startSpinUC_GetRand=(Yaw={1:F9},Pitch={2:F9},Roll={3:F9}) URU=({4:F3},{5:F3},{6:F3}){7}",
            tag,
            slot.StartSpinUc.x, slot.StartSpinUc.y, slot.StartSpinUc.z,
            slot.StartSpinUru.x, slot.StartSpinUru.y, slot.StartSpinUru.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} spsUC_GetRand=(Yaw={1:F9},Pitch={2:F9},Roll={3:F9}) URU/s=({4:F3},{5:F3},{6:F3}){7}",
            tag,
            slot.SpsUc.x, slot.SpsUc.y, slot.SpsUc.z,
            slot.SpsUruUnsigned.x, slot.SpsUruUnsigned.y, slot.SpsUruUnsigned.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} slotStartSpin@+0x3C=({1:F9},{2:F9},{3:F9}) slotSpinsPerSecond@+0x30=({4:F9},{5:F9},{6:F9}){7}",
            tag,
            slot.StartSpinUru.x, slot.StartSpinUru.y, slot.StartSpinUru.z,
            slot.SpsUru.x, slot.SpsUru.y, slot.SpsUru.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} ccwFrand=({1:F9},{2:F9},{3:F9}) ccw=({4:F3},{5:F3},{6:F3}){7}",
            tag,
            slot.FrandX, slot.FrandY, slot.FrandZ,
            slot.Ccw.x, slot.Ccw.y, slot.Ccw.z,
            Environment.NewLine);
        body.AppendFormat(
            CultureInfo.InvariantCulture,
            "  {0} spinMemURU=({1:F0},{2:F0},{3:F0}) spinComponent01SwappedURU=({4:F0},{5:F0},{6:F0}){7}",
            tag,
            slot.MemUru.x, slot.MemUru.y, slot.MemUru.z,
            slot.SwappedUru.x, slot.SwappedUru.y, slot.SwappedUru.z,
            Environment.NewLine);
    }

    private struct CoinSlot
    {
        public uint VelocityState;
        public uint StartSpinState;
        public Vector3 Velocity;
        public Vector3 Location;
        public Vector3 SpawnLocation;
        public Vector3 FinalLocation;
        public Vector3 Size;
        public Vector3 VelocityAfterSpawn;
        public Vector3 StartSpinUc;
        public Vector3 SpsUc;
        public Vector3 StartSpinUru;
        public Vector3 SpsUruUnsigned;
        public Vector3 SpsUru;
        public Vector3 MemUru;
        public Vector3 SwappedUru;
        public Vector3 Ccw;
        public float FrandX;
        public float FrandY;
        public float FrandZ;
        public bool FlipX;
        public bool FlipY;
        public bool FlipZ;
        public bool CcwSelfOk;
    }

    private static CoinSlot EvaluateSlot(Material mat, uint meshBase, int slot, float ageSeconds)
    {
        uint state = L2AppRand.Advance(meshBase, slot * L2AppRand.MeshSpawnSlotToSlotDrawCount);
        var result = new CoinSlot
        {
            VelocityState = state,
            Ccw = ReadVec(mat, "_SpinCCWorCW")
        };
        if (result.Ccw == Vector3.zero && mat == null)
            result.Ccw = new Vector3(0.5f, 0.5f, 0.5f);
        if (mat != null && !mat.HasProperty("_SpinCCWorCW"))
            result.Ccw = new Vector3(0.5f, 0.5f, 0.5f);

        Vector2 velX = ReadMinMax(mat, "_StartVelocityRangeXUc", 0f, 0f);
        Vector2 velY = ReadMinMax(mat, "_StartVelocityRangeYUc", 0f, 0f);
        Vector2 velZ = ReadMinMax(mat, "_StartVelocityRangeZUc", 30f, 80f);
        Vector2 locX = ReadMinMax(mat, "_StartLocationRangeXUc", -3f, 3f);
        Vector2 locY = ReadMinMax(mat, "_StartLocationRangeYUc", -3f, 3f);
        Vector2 locZ = ReadMinMax(mat, "_StartLocationRangeZUc", -5f, 5f);
        Vector4 colorMin = ReadVec(mat, "_ColorMulMin", Vector4.one);
        Vector4 colorMax = ReadVec(mat, "_ColorMulMax", Vector4.one);
        Vector2 life = ReadMinMax(mat, "_LifetimeRange", 1f, 1f);
        Vector2 delay = ReadMinMax(mat, "_InitialDelayRange", 0f, 0f);
        Vector2 sizeX = ReadMinMax(mat, "_StartSizeRangeXUc", 2.5f, 2.5f);
        Vector2 sizeY = ReadMinMax(mat, "_StartSizeRangeYUc", 2.5f, 2.5f);
        Vector2 sizeZ = ReadMinMax(mat, "_StartSizeRangeZUc", 2.5f, 2.5f);
        Vector2 spinYaw = ReadMinMax(mat, "_StartSpinYawRangeUc", 0f, 1f);
        Vector2 spinPitch = ReadMinMax(mat, "_StartSpinPitchRangeUc", 0f, 1f);
        Vector2 spinRoll = ReadMinMax(mat, "_StartSpinRollRangeUc", 0f, 1f);
        Vector2 spsYaw = ReadMinMax(mat, "_SpsYawRangeUc", 0f, 5f);
        Vector2 spsPitch = ReadMinMax(mat, "_SpsPitchRangeUc", 0f, 5f);
        Vector2 spsRoll = ReadMinMax(mat, "_SpsRollRangeUc", 0f, 5f);
        Vector3 offset = (Vector3)ReadVec(mat, "_StartLocationOffsetUc", new Vector4(0f, 0f, 27.1f, 0f));

        result.Velocity = FRangeVector(velX, velY, velZ, ref state);
        result.Location = FRangeVector(locX, locY, locZ, ref state);
        FRange(0f, 1f, ref state);
        for (int i = 0; i < 6; i++)
            Frand(ref state);
        FRangeVector(
            new Vector2(colorMin.x, colorMax.x),
            new Vector2(colorMin.y, colorMax.y),
            new Vector2(colorMin.z, colorMax.z),
            ref state);
        FRange(life.x, life.y, ref state);
        FRange(delay.x, delay.y, ref state);
        FRange(1f, 1f, ref state);
        result.Size = FRangeVector(sizeX, sizeY, sizeZ, ref state);
        result.StartSpinState = state;
        result.StartSpinUc = FRangeVector(spinYaw, spinPitch, spinRoll, ref state);
        result.SpsUc = FRangeVector(spsYaw, spsPitch, spsRoll, ref state);
        result.FrandX = Frand(ref state);
        result.FrandY = Frand(ref state);
        result.FrandZ = Frand(ref state);
        result.FlipX = result.FrandX < result.Ccw.x;
        result.FlipY = result.FrandY < result.Ccw.y;
        result.FlipZ = result.FrandZ < result.Ccw.z;
        Vector3 spsSigned = result.SpsUc;
        if (result.FlipX) spsSigned.x *= -1f;
        if (result.FlipY) spsSigned.y *= -1f;
        if (result.FlipZ) spsSigned.z *= -1f;
        result.CcwSelfOk =
            result.FlipX == (result.FrandX < result.Ccw.x) &&
            result.FlipY == (result.FrandY < result.Ccw.y) &&
            result.FlipZ == (result.FrandZ < result.Ccw.z);
        Vector3 accel = (Vector3)ReadVec(mat, "_AccelerationUc", new Vector4(0f, 0f, -200f, 0f));
        result.SpawnLocation = offset + result.Location;
        float age = Mathf.Max(ageSeconds, 0f);
        // UParticleEmitter tick: Velocity += Accel*dt, then Location += Velocity*dt (post-accel).
        result.VelocityAfterSpawn = result.Velocity + accel * age;
        result.FinalLocation = result.SpawnLocation + result.VelocityAfterSpawn * age;
        result.StartSpinUru = result.StartSpinUc * 65535f;
        result.SpsUruUnsigned = result.SpsUc * 65535f;
        result.SpsUru = spsSigned * 65535f;
        result.MemUru = new Vector3(
            Trunc(result.StartSpinUru.x + result.SpsUru.x * age),
            Trunc(result.StartSpinUru.y + result.SpsUru.y * age),
            Trunc(result.StartSpinUru.z + result.SpsUru.z * age));
        result.SwappedUru = new Vector3(result.MemUru.y, result.MemUru.x, result.MemUru.z);
        return result;
    }

    private struct LiveCoinCapture
    {
        public bool HasCapture;
        public uint StateBeforeVelocity;
        public Vector3 Velocity;
        public Vector3 Location;
        public Vector3 SpawnLocation;
        public Vector3 FinalLocation;
        public Vector3 VelocityAfterSpawn;
        public Vector3 StartSpinUc;
        public Vector3 SpsUc;
        public Vector3 StartSpinUru;
        public Vector3 SpsUru;
        public Vector3 MemUru;
        public Vector3 SwappedUru;
        public Vector3 Offset;
        public bool HasVelocityAfterSpawn;
    }

    private static LiveCoinCapture HardcodedLiveSlot0()
    {
        return new LiveCoinCapture
        {
            HasCapture = true,
            StateBeforeVelocity = LiveSlot0StateBeforeVelocity,
            Velocity = new Vector3(0f, 0f, 44.992218018f),
            Location = new Vector3(0.282631874f, -1.242133856f, -3.625751734f),
            SpawnLocation = new Vector3(0.282631874f, -1.242133856f, 23.474248266f),
            FinalLocation = new Vector3(0.282631874f, -1.242133856f, 23.734727859f),
            VelocityAfterSpawn = new Vector3(0f, 0f, 43.802898407f),
            Offset = new Vector3(0f, 0f, 27.1f),
            HasVelocityAfterSpawn = true,
            StartSpinUc = new Vector3(0.731070876f, 0.678151786f, 0.897946119f),
            SpsUc = new Vector3(0.804772973f, 0.290078521f, 0.329142213f),
            StartSpinUru = new Vector3(47910.730468750f, 44442.675781250f, 58846.898437500f),
            SpsUru = new Vector3(-52740.796875000f, 19010.294921875f, -21570.333984375f),
            MemUru = new Vector3(47597f, 44555f, 58718f),
            SwappedUru = new Vector3(44555f, 47597f, 58718f)
        };
    }

    private static LiveCoinCapture TryParseFirstLiveCoinSlot(string path)
    {
        var result = new LiveCoinCapture();
        if (!File.Exists(path))
            return result;

        try
        {
            string[] lines = File.ReadAllLines(path);
            bool inCoin = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!inCoin)
                {
                    if (line.IndexOf("kind=DropAdenaCoinMesh", StringComparison.Ordinal) >= 0 &&
                        line.IndexOf("slotIndex=0", StringComparison.Ordinal) >= 0)
                    {
                        inCoin = true;
                    }

                    continue;
                }

                if (line.StartsWith("----", StringComparison.Ordinal) && result.StateBeforeVelocity != 0u)
                    break;

                Vector3 parsed;
                uint parsedState;
                if (TryParseTaggedVec3(line, "rawVelocity(GetRand +0x3A0)=", out parsed))
                    result.Velocity = parsed;
                if (TryParseTaggedVec3(line, "StartLocationOffset@+0x14C=", out parsed))
                    result.Offset = parsed;
                if (line.IndexOf("scope[1] StartLocationRange", StringComparison.Ordinal) >= 0 &&
                    TryParseTaggedVec3(line, "value=", out parsed))
                    result.Location = parsed;
                if (TryParseTaggedVec3(line, "finalSpawnLocation=", out parsed))
                    result.FinalLocation = parsed;
                if (TryParseTaggedVec3(line, "finalVelocityAfterSpawn=", out parsed))
                {
                    result.VelocityAfterSpawn = parsed;
                    result.HasVelocityAfterSpawn = true;
                }
                if (TryParseNamedYpr(line, "startSpinUC_GetRand=", out parsed))
                    result.StartSpinUc = parsed;
                if (TryParseNamedYpr(line, "spsUC_GetRand=", out parsed))
                    result.SpsUc = parsed;
                if (TryParseTaggedVec3(line, "slotStartSpin@+0x3C=", out parsed))
                    result.StartSpinUru = parsed;
                if (TryParseTaggedVec3(line, "slotSpinsPerSecond@+0x30=", out parsed))
                    result.SpsUru = parsed;
                if (line.IndexOf("draw[0] before=", StringComparison.Ordinal) >= 0 &&
                    TryParseHexAfter(line, "before=", out parsedState))
                    result.StateBeforeVelocity = parsedState;
            }

            if (result.StateBeforeVelocity != 0u)
            {
                result.HasCapture = true;
                float age = LiveSlot0AgeSeconds;
                if (result.Offset == Vector3.zero)
                    result.Offset = new Vector3(0f, 0f, 27.1f);
                result.SpawnLocation = result.Location + result.Offset;
                if (!result.HasVelocityAfterSpawn)
                    result.VelocityAfterSpawn =
                        result.Velocity + new Vector3(0f, 0f, -200f) * age;
                result.MemUru = new Vector3(
                    Trunc(result.StartSpinUru.x + result.SpsUru.x * age),
                    Trunc(result.StartSpinUru.y + result.SpsUru.y * age),
                    Trunc(result.StartSpinUru.z + result.SpsUru.z * age));
                result.SwappedUru = new Vector3(result.MemUru.y, result.MemUru.x, result.MemUru.z);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DropAdenaCoin] LIVE log parse failed: " + ex.Message);
        }

        return result;
    }

    private static bool TryParseTaggedVec3(string line, string tag, out Vector3 value)
    {
        value = Vector3.zero;
        int at = line.IndexOf(tag, StringComparison.Ordinal);
        if (at < 0)
            return false;
        Match match = Regex.Match(line.Substring(at + tag.Length), @"\(([^)]+)\)");
        if (!match.Success)
            return false;
        return TryParseCsv3(match.Groups[1].Value, out value);
    }

    private static bool TryParseNamedYpr(string line, string tag, out Vector3 value)
    {
        value = Vector3.zero;
        int at = line.IndexOf(tag, StringComparison.Ordinal);
        if (at < 0)
            return false;
        Match match = Regex.Match(
            line.Substring(at),
            @"Yaw=(?<y>-?\d+(?:\.\d+)?)[,\s]+Pitch=(?<p>-?\d+(?:\.\d+)?)[,\s]+Roll=(?<r>-?\d+(?:\.\d+)?)");
        if (!match.Success)
            return false;
        value = new Vector3(
            ParseF(match.Groups["y"].Value),
            ParseF(match.Groups["p"].Value),
            ParseF(match.Groups["r"].Value));
        return true;
    }

    private static bool TryParseHexAfter(string line, string tag, out uint value)
    {
        value = 0;
        int at = line.IndexOf(tag, StringComparison.Ordinal);
        if (at < 0)
            return false;
        Match match = Regex.Match(line.Substring(at + tag.Length), @"0x([0-9A-Fa-f]+)");
        return match.Success &&
               uint.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseCsv3(string csv, out Vector3 value)
    {
        value = Vector3.zero;
        string[] parts = csv.Split(',');
        if (parts.Length < 3)
            return false;
        value = new Vector3(ParseF(parts[0]), ParseF(parts[1]), ParseF(parts[2]));
        return true;
    }

    private static float ParseF(string text)
    {
        return float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? v
            : 0f;
    }

    private static Vector3 FRangeVector(Vector2 x, Vector2 y, Vector2 z, ref uint state)
    {
        float roll = FRange(z.x, z.y, ref state);
        float pitch = FRange(y.x, y.y, ref state);
        float yaw = FRange(x.x, x.y, ref state);
        return new Vector3(yaw, pitch, roll);
    }

    private static float FRange(float min, float max, ref uint state)
    {
        return Frand(ref state) * (min - max) + max;
    }

    private static float Frand(ref uint state)
    {
        state = unchecked(state * L2AppRand.Multiplier + L2AppRand.Increment);
        return ((state >> 16) & 0x7fffu) / 32767f;
    }

    private static float Trunc(float value)
    {
        return (float)Math.Truncate(value);
    }

    private static Vector4 ReadVec(Material mat, string name)
    {
        return ReadVec(mat, name, Vector4.zero);
    }

    private static Vector4 ReadVec(Material mat, string name, Vector4 fallback)
    {
        if (mat != null && mat.HasProperty(name))
            return mat.GetVector(name);
        return fallback;
    }

    private static Vector2 ReadMinMax(Material mat, string name, float defMin, float defMax)
    {
        if (mat != null && mat.HasProperty(name))
        {
            Vector4 v = mat.GetVector(name);
            return new Vector2(v.x, v.y);
        }

        return new Vector2(defMin, defMax);
    }

    private static float ReadFloat(Material mat, string name)
    {
        return mat != null && mat.HasProperty(name) ? mat.GetFloat(name) : 0f;
    }

    private static bool Approx(float a, float b, float eps = 0.05f)
    {
        return Mathf.Abs(a - b) <= eps;
    }

    private static void BeginFile()
    {
        if (_fileStarted)
            return;
        lock (WriteLock)
        {
            if (_fileStarted)
                return;
            try
            {
                string dir = Path.GetDirectoryName(UnityLogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                var body = new StringBuilder(500);
                body.AppendLine("Unity_DropAdenaCoin.log — e_u056_a MeshEmitter7 Coin");
                body.AppendLine("CPU mirror of L2FxUnified_ResolveSpawn(mode=2) + L2FxMeshSpin spawn CCW");
                body.AppendLine("compare LIVE SpawnParticleSnapshot.log first DropAdenaCoinMesh slot=0");
                body.AppendLine(
                    "started=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                body.AppendLine("unityLog=" + UnityLogPath);
                body.AppendLine("liveLog=" + LiveLogPath);
                body.AppendLine("================================================================================");
                File.WriteAllText(UnityLogPath, body.ToString(), Encoding.UTF8);
                _fileStarted = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DropAdenaCoin] cannot write log: " + ex.Message);
            }
        }
    }

    private static void Append(string text)
    {
        if (!Enabled || string.IsNullOrEmpty(text))
            return;
        lock (WriteLock)
        {
            try
            {
                File.AppendAllText(UnityLogPath, text, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DropAdenaCoin] write failed: " + ex.Message);
            }
        }
    }
}
#endif
