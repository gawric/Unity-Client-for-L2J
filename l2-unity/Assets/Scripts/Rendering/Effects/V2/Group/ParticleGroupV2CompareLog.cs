using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes ParticleGroupV2 play/spawn snapshots in the High Elf EffectLog layout
/// so Unity 1147 can be diffed against system/logs/EffectLog.txt.
/// </summary>
public static class ParticleGroupV2CompareLog
{
    const string Tag = "[PGV2Compare]";
    static readonly object Gate = new object();
    static bool _sessionStarted;
    static string _path;

    public static string LogPath
    {
        get
        {
            EnsurePath();
            return _path;
        }
    }

    public static void WritePlay(ParticleGroupV2 group, ParticleGroupAuthoring authoring, Material material)
    {
        if (group == null)
        {
            return;
        }

        StringBuilder sb = new StringBuilder(2048);
        string kind = InferKind(material);
        string effect = InferEffectName(group);
        string part = group.name;
        sb.Append(kind).Append("[0] layer Play\r\n");
        sb.Append("  effect=").Append(effect)
            .Append(" part=").Append(part)
            .Append(" spawnKind=unity gpu=").Append(group.IsGpuDraw ? 1 : 0)
            .Append(" skillID=1147\r\n");
        sb.Append("  maxParticles=").Append(authoring.maxCount)
            .Append(" ips=").Append(F(authoring.countPerSecond))
            .Append(" burst=").Append(authoring.isBurstSpawning ? 1 : 0)
            .Append(" respawn=").Append(authoring.respawnDeadParticles ? 1 : 0)
            .Append(" duration=").Append(F(authoring.duration))
            .Append("\r\n");
        sb.Append("  warmup Relative=").Append(F(authoring.relativeWarmupTime))
            .Append(" Ticks=").Append(F(authoring.warmupTicksPerSecond))
            .Append(" startDelay=").Append(F(authoring.startDelay))
            .Append("\r\n");
        AppendEmitterConfig(sb, kind, material, authoring);
        Write(sb.ToString());
    }

    public static void WriteSpawn(
        ParticleGroupV2 group,
        int slot,
        int slotCount,
        float now,
        float shaderStart,
        float particleLife)
    {
        if (group == null)
        {
            return;
        }

        float particleTime = Mathf.Max(0f, now - shaderStart);
        StringBuilder sb = new StringBuilder(512);
        sb.Append(InferKind(ResolveMaterial(group)))
            .Append("ParticleSpawn slot=").Append(slot)
            .Append("/").Append(slotCount)
            .Append(" part=").Append(group.name)
            .Append("\r\n");
        sb.Append("    particleTime=").Append(F6(particleTime))
            .Append(" maxLife=").Append(F(particleLife))
            .Append(" shaderStart=").Append(F(shaderStart))
            .Append(" now=").Append(F(now))
            .Append("\r\n");
        Write(sb.ToString());
    }

    static void AppendEmitterConfig(
        StringBuilder sb,
        string kind,
        Material material,
        ParticleGroupAuthoring authoring)
    {
        sb.Append("  emitterConfig kind=").Append(kind)
            .Append(" coordinateSystem=").Append(authoring.coordinateSystem)
            .Append(" (Unity ParticleGroupV2; compare to HE EffectLog Tick1)\r\n");
        if (material == null)
        {
            sb.Append("    material=missing\r\n");
            return;
        }

        sb.Append("    nativeCS=").Append(F(GetFloat(material, "_CoordinateSystem")))
            .Append(" independentSprayAccel=")
            .Append(GetFloat(material, "_IndependentSprayAccel") > 0.5f ? 1 : 0)
            .Append("\r\n");

        Vector4 accel = GetVector(material, "_AccelerationUc");
        Vector4 offset = GetVector(material, "_StartLocationOffsetUc");
        Vector4 life = GetVector(material, "_LifetimeRange");
        Vector4 velLoss = GetVector(material, "_VelocityLossRangeUc");
        Vector4 maxAbsVelocity = GetVector(material, "_MaxAbsVelocityUc");
        Vector4 polarX = GetVector(material, "_PolarThetaRangeUc");
        Vector4 polarY = GetVector(material, "_PolarPhiRangeUc");
        Vector4 polarZ = GetVector(material, "_PolarRadiusRangeUc");
        Vector4 sphere = GetVector(material, "_SphereRadiusRangeUc");
        Vector4 locX = GetVector(material, "_StartLocationRangeXUc");
        Vector4 locY = GetVector(material, "_StartLocationRangeYUc");
        Vector4 locZ = GetVector(material, "_StartLocationRangeZUc");
        Vector4 velX = GetVector(material, "_StartVelocityRangeXUc");
        Vector4 velY = GetVector(material, "_StartVelocityRangeYUc");
        Vector4 velZ = GetVector(material, "_StartVelocityRangeZUc");
        Vector4 size = GetVector(material, "_SizeRange");
        if (size.x == 0f && size.y == 0f)
        {
            size = GetVector(material, "_StartSizeRange");
        }

        float shape = GetFloat(material, "_HeLocationShape");
        float polar = GetFloat(material, "_FullTlsShape");
        float useRev = GetFloat(material, "_UseRevolution");
        float useRevScale = GetFloat(material, "_UseRevolutionScale");
        float useVelocityScale = GetFloat(material, "_UseVelocityScale");
        float orient = GetFloat(material, "_OrientationMode");
        float ptvd = GetFloat(material, "_PtvdMode");
        float flip = GetFloat(material, "_FlipbookMode");
        float uSub = GetFloat(material, "_TextureUSubdivisions");
        float vSub = GetFloat(material, "_TextureVSubdivisions");
        float subStart = GetFloat(material, "_SubdivisionStart");
        float subEnd = GetFloat(material, "_SubdivisionEnd");
        float fadeIn = GetFloat(material, "_FadeIn");
        float fadeInEnd = GetFloat(material, "_FadeInEndTime");
        float fadeOut = GetFloat(material, "_FadeOut", "_Fadeout");
        float fadeOutStart = GetFloat(material, "_FadeOutStartTime", "_FadeoutStartTime");
        float opacity = GetFloat(material, "_Opacity");
        float useSize = GetFloat(material, "_UseSizeScale");
        Texture tex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : material.mainTexture;

        sb.Append("    accel@shader=").Append(V3(accel))
            .Append(" locOffset=").Append(V3(offset))
            .Append("\r\n");
        sb.Append("    locShape=").Append(HeShapeName(shape, polar))
            .Append(" polarOn=").Append(polar > 0.5f ? 1 : 0)
            .Append(" sphereRadius=[").Append(F(sphere.x)).Append("..").Append(F(sphere.y)).Append("]")
            .Append("\r\n");
        sb.Append("    polarRange X[").Append(F(polarX.x)).Append("..").Append(F(polarX.y)).Append("]")
            .Append(" Y[").Append(F(polarY.x)).Append("..").Append(F(polarY.y)).Append("]")
            .Append(" Z[").Append(F(polarZ.x)).Append("..").Append(F(polarZ.y)).Append("]")
            .Append("\r\n");
        sb.Append("    locRange X[").Append(F(locX.x)).Append("..").Append(F(locX.y)).Append("]")
            .Append(" Y[").Append(F(locY.x)).Append("..").Append(F(locY.y)).Append("]")
            .Append(" Z[").Append(F(locZ.x)).Append("..").Append(F(locZ.y)).Append("]")
            .Append("\r\n");
        sb.Append("    startVelRange X[").Append(F(velX.x)).Append("..").Append(F(velX.y)).Append("]")
            .Append(" Y[").Append(F(velY.x)).Append("..").Append(F(velY.y)).Append("]")
            .Append(" Z[").Append(F(velZ.x)).Append("..").Append(F(velZ.y)).Append("]")
            .Append("\r\n");
        sb.Append("    startSizeRange=[").Append(F(size.x)).Append("..").Append(F(size.y)).Append("]")
            .Append(" useSizeScale=").Append(useSize > 0.5f ? 1 : 0)
            .Append(" lifetime=[").Append(F(life.x)).Append("..").Append(F(life.y)).Append("]")
            .Append("\r\n");
        sb.Append("    velLoss=").Append(V3(velLoss))
            .Append(" maxAbsVel=").Append(V3(maxAbsVelocity))
            .Append(" ptvd=").Append(PtvdName(ptvd))
            .Append(" UseDirectionAs=").Append(OrientationName(orient))
            .Append("\r\n");
        sb.Append("    UseRevolution=").Append(useRev > 0.5f ? 1 : 0)
            .Append(" revCenter X").Append(Range(material, "_RevolutionCenterOffsetRangeXUc"))
            .Append(" Y").Append(Range(material, "_RevolutionCenterOffsetRangeYUc"))
            .Append(" Z").Append(Range(material, "_RevolutionCenterOffsetRangeZUc"))
            .Append("\r\n");
        sb.Append("    revPerSec X").Append(Range(material, "_RevolutionsPerSecondRangeXUc"))
            .Append(" Y").Append(Range(material, "_RevolutionsPerSecondRangeYUc"))
            .Append(" Z").Append(Range(material, "_RevolutionsPerSecondRangeZUc"))
            .Append("\r\n");
        AppendVectorScale(
            sb, material, "VelocityScale", useVelocityScale, "_VelocityScale");
        AppendVectorScale(
            sb, material, "RevolutionScale", useRevScale, "_RevolutionScale");
        sb.Append("    fadeIn=").Append(fadeIn > 0.5f ? 1 : 0)
            .Append(" fadeInEnd=").Append(F(fadeInEnd))
            .Append(" fadeOut=").Append(fadeOut > 0.5f ? 1 : 0)
            .Append(" fadeOutStart=").Append(F(fadeOutStart))
            .Append(" opacity=").Append(F(opacity))
            .Append("\r\n");
        sb.Append("    texUV=").Append((int)uSub).Append("/").Append((int)vSub)
            .Append(" subdiv=[").Append((int)subStart).Append("..").Append((int)subEnd).Append("]")
            .Append(" flipbook=").Append(FlipbookName(flip))
            .Append(" texture=").Append(tex != null ? tex.name : "-")
            .Append("\r\n");
        sb.Append("    warmup authored Relative=").Append(F(authoring.relativeWarmupTime))
            .Append(" Ticks=").Append(F(authoring.warmupTicksPerSecond))
            .Append("\r\n");
        sb.Append("    note=atlas cut=L2FxFlipbook geometry fit=L2FxCoreGeometryTest (locked)\r\n");
    }

    static void AppendVectorScale(
        StringBuilder sb,
        Material material,
        string label,
        float enabled,
        string propertyPrefix)
    {
        int count = Mathf.Clamp(
            Mathf.RoundToInt(GetFloat(material, propertyPrefix + "Count")),
            0,
            7);
        sb.Append("    ").Append(label)
            .Append(" use=").Append(enabled > 0.5f ? 1 : 0)
            .Append(" repeats=").Append(F(GetFloat(material, propertyPrefix + "Repeats")))
            .Append(" count=").Append(count);
        for (int i = 0; i < count; i++)
        {
            Vector4 key = GetVector(material, propertyPrefix + "Key" + i);
            sb.Append(" k").Append(i)
                .Append("=(t=").Append(F(key.x))
                .Append(",v=").Append(F(key.y)).Append(",")
                .Append(F(key.z)).Append(",").Append(F(key.w)).Append(")");
        }
        sb.Append("\r\n");
    }

    static Material ResolveMaterial(ParticleGroupV2 group)
    {
        Material[] gpu = group != null ? group.GpuMaterials : null;
        if (gpu != null)
        {
            for (int i = 0; i < gpu.Length; i++)
            {
                if (gpu[i] != null)
                {
                    return gpu[i];
                }
            }
        }

        Renderer renderer = group != null ? group.GetComponentInChildren<Renderer>(true) : null;
        return renderer != null ? renderer.sharedMaterial : null;
    }

    static string InferKind(Material material)
    {
        if (material == null || material.shader == null)
        {
            return "Sprite";
        }

        string shader = material.shader.name;
        if (shader.IndexOf("Mesh", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Mesh";
        }

        if (shader.IndexOf("Beam", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Beam";
        }

        return "Sprite";
    }

    static string InferEffectName(ParticleGroupV2 group)
    {
        Transform current = group.transform;
        while (current.parent != null)
        {
            current = current.parent;
            if (current.GetComponent<L2Particle>() != null)
            {
                return current.name;
            }
        }

        return group.transform.root.name;
    }

    static string HeShapeName(float heShape, float polarToggle)
    {
        if (heShape > 1.5f || polarToggle > 0.5f)
        {
            return "2(PTLS_Polar)";
        }

        if (heShape > 0.5f)
        {
            return "1(PTLS_Sphere)";
        }

        return "0(PTLS_Box)";
    }

    static string OrientationName(float mode)
    {
        if (mode > 2.5f) return "PTDU_Forward";
        if (mode > 1.5f) return "PTDU_Normal";
        if (mode > 0.5f) return "PTDU_Up";
        return "PTDU_None";
    }

    static string PtvdName(float mode)
    {
        if (mode > 1.5f) return "PTVD_OwnerAndStartPosition";
        if (mode > 0.5f) return "PTVD_StartPositionAndOwner";
        return "PTVD_None";
    }

    static string FlipbookName(float mode)
    {
        if (mode > 2.5f) return "BlendBetween";
        if (mode > 1.5f) return "Random";
        if (mode > 0.5f) return "Timed";
        return "Static";
    }

    static string Range(Material material, string property)
    {
        Vector4 v = GetVector(material, property);
        return "[" + F(v.x) + ".." + F(v.y) + "]";
    }

    static Vector4 GetVector(Material material, string property)
    {
        return material != null && material.HasProperty(property)
            ? material.GetVector(property)
            : Vector4.zero;
    }

    static float GetFloat(Material material, string property, string alias = null)
    {
        if (material != null && material.HasProperty(property))
        {
            return material.GetFloat(property);
        }

        if (alias != null && material != null && material.HasProperty(alias))
        {
            return material.GetFloat(alias);
        }

        return 0f;
    }

    static string V3(Vector4 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }

    static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    static string F6(float value)
    {
        return value.ToString("0.000000", CultureInfo.InvariantCulture);
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
        _path = Path.Combine(folder, "ParticleGroupV2Compare.txt");
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
}
