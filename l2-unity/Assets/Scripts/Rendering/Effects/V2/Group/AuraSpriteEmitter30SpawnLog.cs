#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Per-slot CPU mirror of SpriteEmitter30 / Aura spawn (shape0 box + PTVD).
/// Cast d_mon_fire2_ca and read Logs/AuraSpriteEmitter30Spawn.txt.
/// </summary>
public static class AuraSpriteEmitter30SpawnLog
{
    const string Tag = "[AuraSE30]";
    const float UuToMeters = 1f / 52.5f;
    static readonly object Gate = new object();
    static readonly List<SlotRow> Rows = new List<SlotRow>(16);
    static string _path;
    static bool _sessionStarted;

    struct SlotRow
    {
        public int Slot;
        public Vector3 LocUe;
        public Vector3 LocUnity;
        public Vector3 WorldUnity;
        public Vector3 LocUeAt05;
        public Vector3 WorldAt05;
    }

    public static void TryWriteSpawn(
        EffectPart host,
        string debugName,
        int slot,
        int slotCount,
        int spawnedTotal,
        uint spriteRandBase,
        float shaderStartTime,
        float now,
        Vector4 spawnLocationAddUe,
        Material[] gpuMaterials)
    {
        if (!Matches(host, debugName, gpuMaterials))
        {
            return;
        }

        Material mat = ResolveMaterial(host, gpuMaterials);
        if (mat == null)
        {
            return;
        }

        L2AppRand.ResolveGpuInstanceRandBits(
            false,
            false,
            0u,
            spriteRandBase,
            slot,
            out _,
            out _,
            out float spriteMotionRandBits,
            out _);
        uint state = unchecked((uint)System.BitConverter.SingleToInt32Bits(spriteMotionRandBits));
        if (state == 0u)
        {
            return;
        }

        DocExtractorSpriteEmitter0MotionSimulator.SpawnSnapshot spawn =
            DocExtractorSpriteEmitter0MotionSimulator.EvaluateSpawnParticle325(mat, state);

        Vector3 offsetUe = mat.HasProperty("_StartLocationOffsetUc")
            ? (Vector3)mat.GetVector("_StartLocationOffsetUc")
            : Vector3.zero;
        Vector3 locUe = spawn.SpawnPositionUe + offsetUe
            + new Vector3(spawnLocationAddUe.x, spawnLocationAddUe.y, spawnLocationAddUe.z);

        float ptvd = mat.HasProperty("_PtvdMode") ? mat.GetFloat("_PtvdMode") : 0f;
        Vector3 velAfter = ApplyPtvd(spawn.VelocityBeforePtvdUe, locUe, ptvd);
        Vector3 locUeAt05 = locUe + velAfter * 0.5f;

        float worldK = mat.HasProperty("_L2FxWorldCalibration")
            ? mat.GetFloat("_L2FxWorldCalibration")
            : 1.1f;
        float heUnit = ResolveHeUnitScale(mat);

        Transform xf = host != null ? host.transform : null;
        Vector3 locUnity = UeToUnityMeters(locUe, heUnit);
        Vector3 worldUnity = xf != null ? xf.TransformPoint(locUnity) : locUnity;
        Vector3 locUnity05 = UeToUnityMeters(locUeAt05, heUnit);
        Vector3 world05 = xf != null ? xf.TransformPoint(locUnity05) : locUnity05;
        float sizeM = spawn.SpawnSizeUU * heUnit * UuToMeters * worldK * 2f;

        if (spawnedTotal <= 1)
        {
            Rows.Clear();
        }

        Rows.Add(new SlotRow
        {
            Slot = slot,
            LocUe = locUe,
            LocUnity = locUnity,
            WorldUnity = worldUnity,
            LocUeAt05 = locUeAt05,
            WorldAt05 = world05
        });

        StringBuilder sb = new StringBuilder(768);
        sb.Append(Tag).Append(" spawn n=").Append(spawnedTotal)
            .Append(" slot=").Append(slot).Append("/").Append(slotCount)
            .Append(" part=").Append(debugName)
            .Append("\r\n");
        sb.Append("  locUe=").Append(V3(locUe))
            .Append(" locUnityM=").Append(V3(locUnity))
            .Append(" worldUnity=").Append(V3(worldUnity))
            .Append("\r\n");
        sb.Append("  locUe@0.5s=").Append(V3(locUeAt05))
            .Append(" world@0.5s=").Append(V3(world05))
            .Append("\r\n");
        sb.Append("  velRawUe=").Append(V3(spawn.RawVelocityUe))
            .Append(" velAfterPtvdUe=").Append(V3(velAfter))
            .Append(" ptvd=").Append(ptvd.ToString("0.###", CultureInfo.InvariantCulture))
            .Append("\r\n");
        sb.Append("  colorMul=").Append(V3(spawn.ColorMulRgb))
            .Append(" sizeUU=").Append(F(spawn.SpawnSizeUU))
            .Append(" sizeM=").Append(F(sizeM))
            .Append(" k=").Append(F(worldK))
            .Append(" he=").Append(F(heUnit))
            .Append("\r\n");
        if (Rows.Count >= 2)
        {
            SlotRow first = Rows[0];
            float dist0 = Vector3.Distance(worldUnity, first.WorldUnity);
            float dist05 = Vector3.Distance(world05, first.WorldAt05);
            sb.Append("  distToSlot0 t0=").Append(F(dist0))
                .Append("m  t0.5=").Append(F(dist05)).Append("m\r\n");
        }

        if (spawnedTotal == 5 || spawnedTotal == slotCount)
        {
            AppendSummary(sb);
        }

        Write(sb.ToString());
        Debug.Log(
            Tag + " n=" + spawnedTotal +
            " slot=" + slot +
            " locUe=" + V3(locUe) +
            " world=" + V3(worldUnity) +
            " sizeM=" + F(sizeM));
    }

    static void AppendSummary(StringBuilder sb)
    {
        float maxT0 = 0f;
        float maxT05 = 0f;
        for (int i = 0; i < Rows.Count; i++)
        {
            for (int j = i + 1; j < Rows.Count; j++)
            {
                maxT0 = Mathf.Max(maxT0, Vector3.Distance(Rows[i].WorldUnity, Rows[j].WorldUnity));
                maxT05 = Mathf.Max(maxT05, Vector3.Distance(Rows[i].WorldAt05, Rows[j].WorldAt05));
            }
        }

        sb.Append(Tag).Append(" SUMMARY count=").Append(Rows.Count)
            .Append(" maxPairDist t0=").Append(F(maxT0))
            .Append("m  t0.5=").Append(F(maxT05))
            .Append("m  (L2 vs_out had 7 overlapping quads)\r\n");
        for (int i = 0; i < Rows.Count; i++)
        {
            sb.Append("    [").Append(Rows[i].Slot).Append("] t0=")
                .Append(V3(Rows[i].WorldUnity))
                .Append(" t0.5=").Append(V3(Rows[i].WorldAt05))
                .Append("\r\n");
        }
    }

    static bool Matches(EffectPart host, string debugName, Material[] gpuMaterials)
    {
        if (NameIsAura(debugName) || (host != null && NameIsAura(host.name)))
        {
            return true;
        }

        Material mat = ResolveMaterial(host, gpuMaterials);
        Texture tex = mat != null
            ? (mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : mat.mainTexture)
            : null;
        return tex != null &&
               string.Equals(tex.name, "fx_m_t4009", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool NameIsAura(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.IndexOf("SpriteEmitter30", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               string.Equals(name, "Aura", System.StringComparison.OrdinalIgnoreCase);
    }

    static Material ResolveMaterial(EffectPart host, Material[] gpuMaterials)
    {
        if (gpuMaterials != null)
        {
            for (int i = 0; i < gpuMaterials.Length; i++)
            {
                if (gpuMaterials[i] != null)
                {
                    return gpuMaterials[i];
                }
            }
        }

        Renderer renderer = host != null ? host.GetComponentInChildren<Renderer>(true) : null;
        return renderer != null ? renderer.sharedMaterial : null;
    }

    static Vector3 ApplyPtvd(Vector3 velocityUe, Vector3 spawnPositionUe, float ptvdMode)
    {
        float len = spawnPositionUe.magnitude;
        if (len <= 1e-5f || ptvdMode <= 0.5f)
        {
            return ptvdMode > 0.5f ? Vector3.zero : velocityUe;
        }

        Vector3 dir = spawnPositionUe / len;
        if (ptvdMode > 1.5f)
        {
            return Vector3.Scale(velocityUe, dir);
        }

        return Vector3.Scale(-velocityUe, dir);
    }

    static float ResolveHeUnitScale(Material mat)
    {
        if (mat == null ||
            !mat.HasProperty("_L2FxHeUnitScaleEnable") ||
            mat.GetFloat("_L2FxHeUnitScaleEnable") <= 0.5f)
        {
            return 1f;
        }

        float scale = mat.HasProperty("_L2FxHeUnitScale")
            ? mat.GetFloat("_L2FxHeUnitScale")
            : 1f;
        return scale > 1e-6f ? scale : 1f;
    }

    static Vector3 UeToUnityMeters(Vector3 ue, float heUnit)
    {
        return new Vector3(ue.x, ue.z, ue.y) * UuToMeters * heUnit;
    }

    static string V3(Vector3 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }

    static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    static void Write(string text)
    {
        try
        {
            EnsurePath();
            lock (Gate)
            {
                if (!_sessionStarted)
                {
                    File.WriteAllText(
                        _path,
                        Tag + " session " + System.DateTime.Now.ToString("o") + "\r\n\r\n",
                        Encoding.UTF8);
                    _sessionStarted = true;
                    Debug.Log(Tag + " writing " + _path);
                }

                File.AppendAllText(_path, text + "\r\n", Encoding.UTF8);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning(Tag + " write failed: " + ex.Message);
        }
    }

    static void EnsurePath()
    {
        if (!string.IsNullOrEmpty(_path))
        {
            return;
        }

        string root = Directory.GetParent(Application.dataPath) != null
            ? Directory.GetParent(Application.dataPath).FullName
            : Application.persistentDataPath;
        string folder = Path.Combine(root, "Logs");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "AuraSpriteEmitter30Spawn.txt");
    }
}
#endif
