using UnityEngine;

public static class CompositeEffectUtilities
{
    public static float ResolveSpawnDelay(CompositePartSpawnTiming timing, float manualDelaySeconds, MagicCastData castData)
    {
        float baseDelay = Mathf.Max(0f, manualDelaySeconds);
        if (castData == null)
        {
            return baseDelay;
        }

        switch (timing)
        {
            case CompositePartSpawnTiming.OnServerShootTime:
                return Mathf.Max(baseDelay, Mathf.Max(0f, castData.serverTimeToShoot));
            case CompositePartSpawnTiming.OnFlightTimeElapsed:
                return Mathf.Max(baseDelay, Mathf.Max(0f, castData.FlightTime));
            case CompositePartSpawnTiming.OnHitTime:
                return Mathf.Max(baseDelay, Mathf.Max(0f, castData.HitTime));
            default:
                return baseDelay;
        }
    }

    public static Vector3 ResolveSpawnPosition(Transform resolvedTransform, Vector3 resolvedWorldPosition, Vector3 positionOffset)
    {
        if (resolvedTransform != null)
        {
            return resolvedTransform.TransformPoint(positionOffset);
        }

        return resolvedWorldPosition + positionOffset;
    }

    public static Quaternion ResolveSpawnRotation(bool inheritRotation, Transform resolvedTransform)
    {
        return inheritRotation && resolvedTransform != null
            ? resolvedTransform.rotation
            : Quaternion.identity;
    }

    public static EffectResolveContext BuildContext(Transform owner, MagicCastData castData)
    {
        EffectResolveContext context = new EffectResolveContext();
        context.CastData = castData;

        if (owner != null)
        {
            context.CasterTransform = owner;
        }

        Entity casterEntity = owner != null ? owner.GetComponentInParent<Entity>() : null;
        if (casterEntity != null)
        {
            context.CasterEntity = casterEntity;
            context.CasterUserId = casterEntity.IdentityInterlude != null ? casterEntity.IdentityInterlude.Id : 0;

            if (casterEntity.TargetId > 0 && World.Instance != null)
            {
                Entity targetEntity = World.Instance.GetEntityNoLockSync(casterEntity.TargetId);
                if (targetEntity != null)
                {
                    context.TargetEntity = targetEntity;
                    context.TargetUserId = targetEntity.IdentityInterlude != null
                        ? targetEntity.IdentityInterlude.Id
                        : casterEntity.TargetId;
                    context.TargetTransform = targetEntity.transform;
                }
            }
        }

        return context;
    }
}

