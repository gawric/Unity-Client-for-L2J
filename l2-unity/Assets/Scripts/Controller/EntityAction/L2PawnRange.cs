using System;
using UnityEngine;

/// <summary>
/// L2 engine.dll MoveToPawn / AdjustPawnLocation stand distance.
/// Dist is 2D to pawn Location. AdjustPawnLocation skips |delta| ≤ 200 UU.
/// </summary>
public sealed class L2PawnRange
{
    public const float AdjustSkipUu = 200f;

    public float AdjustSkipMeters()
    {
        return VectorUtils.ConvertL2UuToMeters(AdjustSkipUu);
    }

    public bool IsBow(Entity entity)
    {
        UserEntity user = entity as UserEntity;
        if (user != null)
            return string.Equals(user.WeaponAnim, "bow", StringComparison.OrdinalIgnoreCase);

        return entity != null && entity.Gear != null &&
               !string.IsNullOrEmpty(entity.Gear.WeaponAnim) &&
               string.Equals(entity.Gear.WeaponAnim, "bow", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Point on the Dist circle along packet origin → target Location.
    /// Same 2D ring L2 uses in APawn::ReachedDestination when the goal is a pawn.
    /// </summary>
    public Vector3 StopPointOnDistRing(Vector3 origin, Vector3 pawnLocation, float distMeters)
    {
        Vector3 fromPawn = VectorUtils.To2D(origin) - VectorUtils.To2D(pawnLocation);
        fromPawn.y = 0f;
        float mag = fromPawn.magnitude;
        if (mag < 0.001f || distMeters <= 0.01f)
            return origin;

        Vector3 dir = fromPawn / mag;
        return new Vector3(pawnLocation.x, origin.y, pawnLocation.z) + dir * distMeters;
    }

    public bool ShouldSkipCharInfoPositionSnap(Entity entity, Vector3 newPos)
    {
        if (!(entity is UserEntity))
            return false;

        float d = VectorUtils.Distance2D(entity.transform.position, newPos);
        if (d <= 0.15f)
            return true;

        if (HasReachedPawnDist(entity) && d < AdjustSkipMeters())
            return true;

        return IsBow(entity) && d < AdjustSkipMeters();
    }

    public static bool HasReachedPawnDist(Entity entity)
    {
        if (entity == null)
            return false;
        Entity pawn = EntityActionCombatLog.PawnOf(entity);
        float dist = entity.ActionSlot.PawnDist;
        if (pawn == null || dist <= 0.01f)
            return false;
        return VectorUtils.Distance2D(entity.transform.position, pawn.transform.position) <= dist + 0.12f;
    }

    public static void IgnorePawnCollision(Entity mover, Entity pawn)
    {
        ClearIgnoredPawn(mover);
        if (mover == null || pawn == null)
            return;
        IgnoreBetween(mover, pawn, true);
        mover.ActionSlot.CollisionPawn = pawn;
    }

    public static void ClearIgnoredPawn(Entity mover)
    {
        if (mover == null || mover.ActionSlot == null)
            return;
        Entity pawn = mover.ActionSlot.CollisionPawn;
        if (pawn == null)
            return;
        IgnoreBetween(mover, pawn, false);
        mover.ActionSlot.CollisionPawn = null;
    }

    public static bool TrySnapUserToPacket(Entity entity, Vector3 packetPos, string reason)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return false;

        float d = VectorUtils.Distance2D(user.transform.position, packetPos);
        if (d <= VectorUtils.ConvertL2UuToMeters(AdjustSkipUu))
            return false;

        Vector3 from = user.transform.position;
        Vector3 grounded = GroundSnapHelper.SnapToGroundOrKeep(packetPos);
        if (user.Identity != null)
            user.Identity.Position = grounded;
        EntitySpawnShared.ApplyGroundedTransform(user.gameObject, grounded, user.transform.rotation);
        EntityActionCombatLog.LogGap(user, "SNAP " + reason, EntityActionCombatLog.PawnOf(user),
            " snapD=" + d.ToString("F2") +
            " from=" + EntityActionCombatLog.Vec(from) +
            " to=" + EntityActionCombatLog.Vec(grounded));
        return true;
    }

    static void IgnoreBetween(Entity a, Entity b, bool ignore)
    {
        CharacterController ca = a != null ? a.GetComponent<CharacterController>() : null;
        CharacterController cb = b != null ? b.GetComponent<CharacterController>() : null;
        if (ca == null || cb == null)
            return;
        Physics.IgnoreCollision(ca, cb, ignore);
    }
}
