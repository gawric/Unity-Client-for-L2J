using UnityEngine;

/// <summary>
/// Follow target used by move-to-pawn. Kept after the combat log class was moved out.
/// </summary>
public static class EntityPawnLookup
{
    public static Entity PawnOf(Entity entity)
    {
        if (entity == null)
            return null;
        Entity target = entity.ActionSlot.Target;
        if (target != null && target != entity)
            return target;
        if (entity.AttackTarget == null)
            return null;
        Entity fromTransform = entity.AttackTarget.GetComponent<Entity>();
        if (fromTransform == null || fromTransform == entity)
            return null;
        return fromTransform;
    }

    public static Entity ResolvePawn(Entity mover, Vector3 dest)
    {
        Entity pawn = PawnOf(mover);
        if (pawn != null)
            return pawn;
        return FindNearDest(dest, mover, 0.45f);
    }

    public static Entity FindNearDest(Vector3 dest, Entity skip, float radius)
    {
        World world = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld
            : World.Instance;
        if (world == null)
            return null;
        Entity found = null;
        float best = radius;
        world.ForEachEntity(e =>
        {
            if (e == null || e == skip || e is UserEntity || e is PlayerEntity)
                return;
            if (e.transform == null)
                return;
            float d = VectorUtils.Distance2D(dest, e.transform.position);
            if (d < best)
            {
                best = d;
                found = e;
            }
        });
        return found;
    }
}
