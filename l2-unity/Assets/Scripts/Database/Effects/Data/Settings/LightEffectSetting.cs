using UnityEngine;

/// <summary>
/// Who to illuminate — combat roles, NOT "where the VFX prefab sits".
/// Example: player hits a dog → player = Caster, dog = Target.
/// Soulshot impact FX also appears on the dog, but that does not make the dog Caster.
/// </summary>
public enum LightAttachSubject
{
    /// <summary>Attacker / skill user (e.g. player).</summary>
    Caster = 0,

    /// <summary>Victim being hit (e.g. NPC/dog). Use this for shot_N_atk hit flash.</summary>
    Target = 1,

    /// <summary>Raw impact point from SetImpactHit (HitPointProxy path), no Entity required.</summary>
    HitPoint = 2
}

[CreateAssetMenu(fileName = "LightEffectSetting", menuName = "VFX/Settings/LightEffect")]
public class LightEffectSetting : ScriptableObject
{
    [Header("Look")]
    public Color color = Color.white;
    [Tooltip("Peak Light.intensity")]
    public float intensity = 0.6f;
    public float durationSeconds = 0.4f;
    public float rangeMeters = 2f;
    public float spotAngle = 71.5f;
    public float innerSpotAngle = 32.5f;

    [Header("Attach")]
    [Tooltip(
        "Combat subject to light.\n" +
        "Caster = attacker (player).\n" +
        "Target = victim (NPC) — soulshot flash on dog.\n" +
        "HitPoint = SetImpactHit world point only.\n" +
        "VFX spawning on the NPC does NOT mean Attach=Caster.")]
    public LightAttachSubject attachSubject = LightAttachSubject.Target;

    [Tooltip("Place on attacker-facing side of capsule (Location - dir * radius * scale).")]
    public bool useFaceOffset = true;
    public float faceOffsetRadiusScale = 2f;
}
