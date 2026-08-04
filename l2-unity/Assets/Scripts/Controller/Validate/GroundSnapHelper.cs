using UnityEngine;

/// <summary>
/// Places a world position on terrain via GroundMask raycast.
/// Shared by ValidateLocation jumps and player teleport.
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

    public static LayerMask ResolveGroundMask(LayerMask? groundMask)
    {
        if (groundMask.HasValue)
        {
            return groundMask.Value;
        }

        if (World.Instance != null)
        {
            return World.Instance.GroundMask;
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
        LayerMask mask = ResolveGroundMask(groundMask);

        if (TryHitY(position.x, position.y + startAbove, position.z, startAbove + maxDistance, mask, out float groundY))
        {
            snapped = new Vector3(position.x, groundY, position.z);
            return true;
        }

        // Server/Unity Y may sit under the mesh; cast from a high world origin on the same XZ.
        if (TryHitY(position.x, FallbackOriginY, position.z, FallbackOriginY + maxDistance, mask, out groundY))
        {
            snapped = new Vector3(position.x, groundY, position.z);
            return true;
        }

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

    static bool TryHitY(float x, float originY, float z, float maxDistance, LayerMask mask, out float groundY)
    {
        groundY = 0f;
        Vector3 origin = new Vector3(x, originY, z);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, mask))
        {
            return false;
        }

        groundY = hit.point.y;
        return true;
    }
}
