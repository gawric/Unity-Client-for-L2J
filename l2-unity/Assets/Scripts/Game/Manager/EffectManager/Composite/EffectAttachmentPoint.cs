public enum EffectAttachmentPoint
{
    CasterRoot = 0,
    CasterLowerBody = 1,
    WeaponSocket = 2,
    TargetRoot = 3,
    TargetLowerBody = 4,
    WorldHitPoint = 5,
    CasterPosition = 6,
    TargetPosition = 7,
    /// <summary>
    /// Capsule center from CharacterController (local center → world). Works for quadrupeds and humanoids
    /// when the controller is authored per prefab; falls back to combined renderer bounds center.
    /// </summary>
    TargetCenter = 8,
    /// <summary>
    /// Center of target horizontally, above the visual mesh (renderer bounds top or capsule top).
    /// </summary>
    TargetOverHead = 9,
    /// <summary>
    /// Capsule / renderer bounds center on caster (waist–chest area for humanoids).
    /// </summary>
    CasterCenter = 10
}
