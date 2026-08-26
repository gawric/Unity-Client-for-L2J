/// <summary>
/// Placeholder IDs and names for ground-item presentation.
/// Wire real EffectManager database ids and animator triggers here.
/// </summary>
public static class ItemDropPresentationIds
{
    // --- EffectManager.database.effects[].id (0 = disabled stub) ---
    public const int LandBurstEffectId = 56001;
    public const int GroundGlowEffectId = 56002;
    public const int WeaponFallTrailEffectId = 0;
    /// <summary>
    /// Throw-time trail. Must stay 0: 56001 is e_u056_a land burst only.
    /// Sharing LandBurst here spawned two coin piles per drop.
    /// </summary>
    public const int CoinSparkleEffectId = 0;
    /// <summary>
    /// LIVE OnDeleteItem / GetItem only destroys AL2Pickup (drop_mesh + attached e_u056_a/b).
    /// Do not play 56001/56002 here: that respawns Kira white flashes on pickup.
    /// </summary>
    public const int PickupBurstEffectId = 0;

    // --- IAnimationManager.PlayAnimationTrigger ---
    /// <summary>Player throws item from inventory (ASM drop gesture).</summary>
    public const string DropAnimationTrigger = "drop";
    /// <summary>Player_Basic state name (CrossFade, no weapon suffix).</summary>
    public const string PickupAnimationState = "pickup";

    /// <summary>e_u056_b spawn window: 30 particles at 2/s plus lifetime/delay (~17s).</summary>
    public const float GroundGlowDurationSeconds = 18f;

    /// <summary>Simplified throw arc when DropItem packet has a dropper charObjId.</summary>
    public const float DropThrowArcSeconds = 0.35f;
    public const float DropThrowArcHeightMeters = 0.45f;
    /// <summary>
    /// Hover start when there is no dropper. 1.05m is above an L2 capsule (~0.9m)
    /// and reads as "out of the skull". Hand height ≈ 1.15 × collision half-height.
    /// </summary>
    public const float DropThrowStartHeightMeters = 0.28f;
    /// <summary>Weapon AL2NMover toss is longer so Pitch/Roll spin is visible.</summary>
    public const float WeaponThrowArcSeconds = 0.55f;
    public const float GroundGlowLocalY = 0.05f;
    /// <summary>
    /// Extra lift above GroundSnapHelper hit. CoinJunk OffsetZ=-4 UU is now
    /// world-scale (UU/52.5, no mesh K) so the old 8cm sink shrinks with it.
    /// Do not use 0.2 — that hovered the pile while motion still had K=1.8.
    /// </summary>
    public const float LandBurstLocalY = 0.04f;
    /// <summary>
    /// Hide DropMesh until e_u056_a CoinJunk (InitialDelay 0.7s). Adena must not
    /// use the weapon/potion throw arc — toss coins are the FX, not the static mesh.
    /// </summary>
    public const float CoinDropMeshRevealDelaySeconds = 0.7f;
}

public enum ItemDropVisualKind
{
    Generic,
    Weapon,
    Armor,
    EtcStackable,
    Adena
}
