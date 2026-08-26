#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

/// <summary>
/// Who hides/destroys e_u056_a, CoinJunk, and HitPointProxy. Filter Console: [DropAdenaDestroy]
/// </summary>
public static class DropAdenaDestroyLog
{
    public static bool IsDropAdena(string name, Transform transform, Component component = null)
    {
        if (ContainsDropToken(name))
            return true;
        if (component != null && ContainsDropToken(component.name))
            return true;
        Transform current = transform;
        int depth = 0;
        while (current != null && depth < 8)
        {
            if (ContainsDropToken(current.name))
                return true;
            current = current.parent;
            depth++;
        }

        return false;
    }

    public static void Event(
        string action,
        Component source,
        string extra,
        bool includeStack)
    {
        if (source == null || !IsDropAdena(source.name, source.transform, source))
            return;

        Transform t = source.transform;
        Transform root = t.root;
        Transform parent = t.parent;
        string extraText = string.IsNullOrEmpty(extra) ? string.Empty : " " + extra;
        string stack = includeStack ? "\nstack=" + Environment.StackTrace : string.Empty;
        Debug.Log(
            $"[DropAdenaDestroy] {action} now={Time.time:F3}s frame={Time.frameCount} " +
            $"src='{source.name}' type={source.GetType().Name} " +
            $"parent='{(parent != null ? parent.name : "null")}' " +
            $"root='{(root != null ? root.name : "null")}' " +
            $"activeSelf={t.gameObject.activeSelf} activeInHierarchy={t.gameObject.activeInHierarchy} " +
            $"srcId={source.GetInstanceID()}{extraText}{stack}");
    }

    static bool ContainsDropToken(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.IndexOf("e_u056_a", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("CoinJunk", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("MeshEmitter6", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("HitPointProxy", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif
