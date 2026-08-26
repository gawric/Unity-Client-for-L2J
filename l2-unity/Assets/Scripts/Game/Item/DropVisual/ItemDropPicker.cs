using UnityEngine;

public sealed class ItemDropPicker
{
    public const float CastRadius = 0.02f;
    public const float KeepRadius = 0.06f;
    const float MaxDistance = 80f;
    static readonly Collider[] OverlapBuffer = new Collider[32];

    readonly ItemDropLayerService _layers;
    int _groundPickMask = -1;

    public ItemDropPicker(ItemDropLayerService layers)
    {
        _layers = layers;
    }

    public bool TryPick(
        Ray ray,
        float distance,
        ItemEntity sticky,
        out ItemEntity item,
        out float hitDistance)
    {
        item = null;
        hitDistance = 0f;
        LayerMask mask = _layers.Mask;
        if (mask == 0)
            return false;

        if (sticky != null && !sticky)
            sticky = null;

        float maxDist = Mathf.Min(distance, MaxDistance);
        bool hasGround = TryGetGroundPoint(ray, maxDist, out Vector3 groundPoint);

        if (sticky != null &&
            TryItemHoverDistance(sticky, ray, maxDist, hasGround, groundPoint, out float stickyDist) &&
            stickyDist <= KeepRadius)
        {
            item = sticky;
            hitDistance = AlongRay(ray, sticky.transform.position);
            return true;
        }

        ItemEntity best = null;
        float bestRadial = float.MaxValue;
        float bestAlong = float.MaxValue;
        Collect(
            Physics.OverlapCapsuleNonAlloc(
                ray.origin,
                ray.origin + ray.direction * maxDist,
                CastRadius,
                OverlapBuffer,
                mask,
                QueryTriggerInteraction.Collide),
            ray,
            maxDist,
            CastRadius,
            hasGround,
            groundPoint,
            ref best,
            ref bestRadial,
            ref bestAlong);

        if (hasGround)
        {
            Collect(
                Physics.OverlapSphereNonAlloc(
                    groundPoint,
                    CastRadius,
                    OverlapBuffer,
                    mask,
                    QueryTriggerInteraction.Collide),
                ray,
                maxDist,
                CastRadius,
                hasGround,
                groundPoint,
                ref best,
                ref bestRadial,
                ref bestAlong);
        }

        if (best == null)
            return false;

        item = best;
        hitDistance = bestAlong;
        return true;
    }

    bool TryGetGroundPoint(Ray ray, float distance, out Vector3 point)
    {
        point = default;
        int mask = GroundPickMask();
        if (mask == 0)
            return false;
        if (!Physics.Raycast(ray, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
            return false;
        point = hit.point;
        return true;
    }

    int GroundPickMask()
    {
        if (_groundPickMask >= 0)
            return _groundPickMask;

        _groundPickMask = 0;
        AddLayer(ref _groundPickMask, "Terrain");
        AddLayer(ref _groundPickMask, "StaticMesh");
        AddLayer(ref _groundPickMask, "Brush");
        AddLayer(ref _groundPickMask, "Default");
        AddLayer(ref _groundPickMask, "Unwalkable");
        AddLayer(ref _groundPickMask, "AllowWalk");
        AddLayer(ref _groundPickMask, "Obstacle");
        return _groundPickMask;
    }

    static void AddLayer(ref int mask, string name)
    {
        int layer = LayerMask.NameToLayer(name);
        if (layer >= 0)
            mask |= 1 << layer;
    }

    static void Collect(
        int count,
        Ray ray,
        float maxDist,
        float maxRadial,
        bool hasGround,
        Vector3 groundPoint,
        ref ItemEntity best,
        ref float bestRadial,
        ref float bestAlong)
    {
        for (int i = 0; i < count; i++)
        {
            Collider col = OverlapBuffer[i];
            if (col == null)
                continue;
            ItemEntity candidate = col.GetComponentInParent<ItemEntity>();
            if (candidate == null)
                continue;

            float along = AlongRay(ray, col.bounds.center);
            if (along < 0f || along > maxDist)
                continue;

            Vector3 onRay = ray.origin + ray.direction * along;
            float radial = Vector3.Distance(onRay, col.ClosestPoint(onRay));
            float groundDist = hasGround
                ? Vector3.Distance(groundPoint, col.ClosestPoint(groundPoint))
                : float.MaxValue;
            float score = Mathf.Min(radial, groundDist);
            if (score > maxRadial)
                continue;

            if (score < bestRadial - 0.001f ||
                (Mathf.Abs(score - bestRadial) <= 0.001f && along < bestAlong))
            {
                best = candidate;
                bestRadial = score;
                bestAlong = along;
            }
        }
    }

    static bool TryItemHoverDistance(
        ItemEntity entity,
        Ray ray,
        float maxDist,
        bool hasGround,
        Vector3 groundPoint,
        out float hoverDist)
    {
        hoverDist = float.MaxValue;
        Collider[] colliders = entity.GetComponentsInChildren<Collider>();
        bool any = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled)
                continue;
            float along = AlongRay(ray, col.bounds.center);
            if (along < 0f || along > maxDist)
                continue;
            Vector3 onRay = ray.origin + ray.direction * along;
            hoverDist = Mathf.Min(hoverDist, Vector3.Distance(onRay, col.ClosestPoint(onRay)));
            if (hasGround)
                hoverDist = Mathf.Min(hoverDist, Vector3.Distance(groundPoint, col.ClosestPoint(groundPoint)));
            any = true;
        }

        return any;
    }

    static float AlongRay(Ray ray, Vector3 worldPoint)
    {
        return Vector3.Dot(worldPoint - ray.origin, ray.direction);
    }
}
