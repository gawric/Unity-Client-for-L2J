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
    TargetCenter = 8
}
