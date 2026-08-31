using System;
using UnityEngine;

/// <summary>
/// Temporary probe: is SpriteEmitter2911 spawned, GPU-bound, or skipped.
/// Filter Console by [NpcDeco:2911].
/// </summary>
public static class NpcDeco2911Trace
{
    public const string Tag = "[NpcDeco:2911]";

    public static bool Matches(string name)
    {
        return !string.IsNullOrEmpty(name) &&
               name.IndexOf("SpriteEmitter2911", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static void Log(string message)
    {
        Debug.Log(Tag + " " + message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning(Tag + " " + message);
    }

    public static void DumpSpawnedPrefab(BaseEffect instance)
    {
        if (instance == null)
        {
            Warn("prefab instance is null");
            return;
        }

        ParticleGroupV2[] groups = instance.GetComponentsInChildren<ParticleGroupV2>(true);
        ParticleGroupV2 found = null;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && Matches(groups[i].name))
            {
                found = groups[i];
                break;
            }
        }

        if (found == null)
        {
            Warn(
                "NOT IN HIERARCHY of " + instance.gameObject.name +
                " groups=" + groups.Length + " — prefab skip or wrong name");
            return;
        }

        Log(
            "found GO='" + found.name +
            "' active=" + found.gameObject.activeInHierarchy +
            " enabled=" + found.enabled +
            " childIndex=" + found.transform.GetSiblingIndex() +
            " worldPos=" + found.transform.position);
        DumpGroup(found);
    }

    public static void DumpGroup(ParticleGroupV2 group)
    {
        if (group == null)
            return;

        Renderer slot = group.GetComponentInChildren<Renderer>(true);
        Material mat = slot != null ? slot.sharedMaterial : null;
        MeshFilter filter = slot != null ? slot.GetComponent<MeshFilter>() : null;
        string meshName = filter != null && filter.sharedMesh != null
            ? filter.sharedMesh.name
            : "none";

        Log(
            "group '" + group.name +
            "' gpuFlag=" + group.IsGpuDraw +
            " slots=" + CountRenderers(group) +
            " mesh=" + meshName +
            " mat=" + (mat != null ? mat.name : "null") +
            " shader=" + (mat != null && mat.shader != null ? mat.shader.name : "null") +
            " orient=" + ReadFloat(mat, "_OrientationMode") +
            " size=" + ReadVector(mat, "_SizeRange") +
            " life=" + ReadVector(mat, "_LifetimeRange") +
            " fadeIn=" + ReadFloat(mat, "_FadeIn") +
            " fadeInEnd=" + ReadFloat(mat, "_FadeInEndTime") +
            " opacity=" + ReadFloat(mat, "_Opacity") +
            " rendererOn=" + (slot != null && slot.enabled) +
            " slotGoOn=" + (slot != null && slot.gameObject.activeSelf) +
            " isVisible=" + (slot != null && slot.isVisible));
    }

    static int CountRenderers(ParticleGroupV2 group)
    {
        Renderer[] renderers = group.GetComponentsInChildren<Renderer>(true);
        return renderers != null ? renderers.Length : 0;
    }

    static string ReadFloat(Material mat, string property)
    {
        if (mat == null || !mat.HasProperty(property))
            return "-";
        return mat.GetFloat(property).ToString("0.###");
    }

    static string ReadVector(Material mat, string property)
    {
        if (mat == null || !mat.HasProperty(property))
            return "-";
        Vector4 v = mat.GetVector(property);
        return "(" + v.x.ToString("0.###") + "," + v.y.ToString("0.###") + ")";
    }
}
