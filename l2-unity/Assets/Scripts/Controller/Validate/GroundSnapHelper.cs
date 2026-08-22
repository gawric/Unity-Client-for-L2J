using UnityEngine;

/// <summary>
/// Places a world position on terrain via GroundMask raycast.
/// Shared by ValidateLocationDto jumps and player teleport.
/// </summary>
public static class GroundSnapHelper
{
    /// <summary>How far above the given Y to start the ray.</summary>
    public const float DefaultStartAbove = 50f;

    /// <summary>Max distance below the ray origin.</summary>
    public const float DefaultMaxDistance = 300f;

    /// <summary>
    /// Absolute-height fallback when the local cast misses (e.g. spawn Y below mesh).
    /// </summary>
    public const float FallbackOriginY = 200f;

    /// <summary>Same high origin geodata uses when GroundMask misses village grass.</summary>
    public const float HighOriginY = 750f;

    public const float HighMaxDistance = 1000f;

    const float MaxLift = 1.25f;
    const float MaxDrop = 15f;

    public static LayerMask ResolveGroundMask(LayerMask? groundMask)
    {
        if (groundMask.HasValue)
        {
            return groundMask.Value;
        }

        World world = IncomingPacketActions.GameWorld;
        if (world != null)
        {
            return world.GroundMask;
        }

        return Physics.DefaultRaycastLayers;
    }

    public static bool TrySnapToGround(
        Vector3 position,
        out Vector3 snapped,
        LayerMask? groundMask = null,
        float startAbove = DefaultStartAbove,
        float maxDistance = DefaultMaxDistance)
    {
        snapped = position;
        LayerMask mask = WalkableMask(groundMask);

        if (TryPickNearServerY(position, position.y + startAbove, startAbove + maxDistance, mask, 0f, out snapped))
            return true;
        if (TryPickNearServerY(position, HighOriginY, HighMaxDistance, mask, 0f, out snapped))
            return true;
        if (TryPickNearServerY(position, HighOriginY, HighMaxDistance, mask, 0.4f, out snapped))
            return true;

        return false;
    }

    public static Vector3 SnapToGroundOrKeep(
        Vector3 position,
        LayerMask? groundMask = null,
        float startAbove = DefaultStartAbove,
        float maxDistance = DefaultMaxDistance)
    {
        return TrySnapToGround(position, out Vector3 snapped, groundMask, startAbove, maxDistance)
            ? snapped
            : position;
    }

    static LayerMask WalkableMask(LayerMask? groundMask)
    {
        LayerMask mask = ResolveGroundMask(groundMask);
        mask |= LayerMask.GetMask("Default", "Terrain", "StaticMesh", "Brush", "AllowWalk", "Deco", "Obstacle");
        mask &= ~LayerMask.GetMask("Entity", "EntityClick", "Player", "UI", "Ignore Raycast", "SkillEffect");
        return mask;
    }

    static bool TryPickNearServerY(
        Vector3 position,
        float originY,
        float maxDistance,
        LayerMask mask,
        float sphereRadius,
        out Vector3 snapped)
    {
        snapped = position;
        Vector3 origin = new Vector3(position.x, originY, position.z);
        RaycastHit[] hits = sphereRadius > 0.001f
            ? Physics.SphereCastAll(origin, sphereRadius, Vector3.down, maxDistance, mask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(origin, Vector3.down, maxDistance, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        RaycastHit best = default;
        float bestDelta = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
                continue;

            float dy = hit.point.y - position.y;
            if (dy > MaxLift || dy < -MaxDrop)
                continue;

            float abs = dy < 0f ? -dy : dy;
            if (abs < bestDelta)
            {
                bestDelta = abs;
                best = hit;
                found = true;
            }
        }

        if (!found)
            return false;

        snapped = new Vector3(position.x, best.point.y, position.z);
        return true;
    }
}
