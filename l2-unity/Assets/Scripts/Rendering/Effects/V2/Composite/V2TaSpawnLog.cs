#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Dev-only: delayed / independent composite parts (Might m_u004_b). Filter Console by <c>[V2_TA]</c>.
/// </summary>
public static class V2TaSpawnLog
{
    public const string Tag = "[V2_TA]";

    public static bool Matches(CompositePart part)
    {
        if (part == null)
        {
            return false;
        }

        if (part is IndependentEffectPart)
        {
            return true;
        }

        string name = part.name;
        return !string.IsNullOrEmpty(name) &&
               (name.IndexOf("m_u004_b", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.EndsWith("_ta", System.StringComparison.OrdinalIgnoreCase));
    }

    public static void Info(string message)
    {
        Debug.Log(Tag + " " + message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning(Tag + " " + message);
    }
}
#endif
