using UnityEngine;

/// <summary>
/// Shared Unity layer helpers for the L2 D3D9 compositor.
/// TagManager layer index 18 = SkillEffect.
/// </summary>
public static class L2FxCompositorLayers
{
    public const string SkillEffectName = "SkillEffect";
    public const int SkillEffect = 18;

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
