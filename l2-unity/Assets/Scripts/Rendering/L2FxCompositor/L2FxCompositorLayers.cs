using UnityEngine;

/// <summary>
/// Shared Unity layer helpers for the L2 D3D9 compositor.
    /// TagManager: 18 SkillEffect, 20 L2Sky (sun/moon/stars), 21 L2Haze (WhiteRing), 22 L2Clouds.
/// </summary>
public static class L2FxCompositorLayers
{
    public const string SkillEffectName = "SkillEffect";
    public const int SkillEffect = 18;
    public const string L2SkyName = "L2Sky";
    public const int L2Sky = 20;
    public const string L2HazeName = "L2Haze";
    public const int L2Haze = 21;
    public const string L2CloudsName = "L2Clouds";
    public const int L2Clouds = 22;

    public static int ResolveL2SkyLayer()
    {
        int layer = LayerMask.NameToLayer(L2SkyName);
        return layer >= 0 ? layer : L2Sky;
    }

    public static int ResolveL2HazeLayer()
    {
        int layer = LayerMask.NameToLayer(L2HazeName);
        return layer >= 0 ? layer : L2Haze;
    }

    public static void ApplyL2SkyLayer(GameObject go)
    {
        if (go == null)
            return;

        SetLayerRecursive(go, ResolveL2SkyLayer());
    }

    public static void ApplyL2HazeLayer(GameObject go)
    {
        if (go == null)
            return;

        SetLayerRecursive(go, ResolveL2HazeLayer());
    }

    public static int ResolveL2CloudsLayer()
    {
        int layer = LayerMask.NameToLayer(L2CloudsName);
        return layer >= 0 ? layer : L2Clouds;
    }

    public static void ApplyL2CloudsLayer(GameObject go)
    {
        if (go == null)
            return;

        SetLayerRecursive(go, ResolveL2CloudsLayer());
    }

    public static void ApplySkillEffectLayer(GameObject go)
    {
        if (go == null)
            return;

        int layer = LayerMask.NameToLayer(SkillEffectName);
        if (layer < 0)
            layer = SkillEffect;

        SetLayerRecursive(go, layer);
    }

    public static void SetLayerRecursive(GameObject go, int layer)
    {
        if (go == null)
            return;

        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i).gameObject, layer);
    }
}
