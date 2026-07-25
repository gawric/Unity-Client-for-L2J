using UnityEngine;

/// <summary>
/// Resolves world light position / aim from EffectResolveContext + LightEffectSetting.
/// Combat roles: Caster=attacker, Target=victim (dog when player hits).
/// Face offset mirrors former HitManager placement (attacker-facing capsule side).
/// Not related to ParticleGroup / ParticleSingle — called only from CompositePrefabEffect.
/// </summary>
public static class EffectLightPlacement
{
    public static bool TryResolve(
        EffectResolveContext context,
        LightEffectSetting settings,
        out Vector3 lightPoint,
        out Vector3 lightDir)
    {
        lightPoint = Vector3.zero;
        lightDir = Vector3.forward;

        if (settings == null)
        {
            return false;
        }

        Vector3 aim = ResolveAimDirection(context);
        lightDir = aim;

        switch (settings.attachSubject)
        {
            case LightAttachSubject.Caster:
                lightPoint = ResolveEntityCenter(context != null ? context.CasterEntity : null,
                    context != null ? context.CasterTransform : null,
                    fallback: Vector3.zero);
                if (lightPoint.sqrMagnitude < 0.0001f && context != null && context.HasHitPoint)
                {
                    lightPoint = context.HitPoint;
                }

                if (settings.useFaceOffset)
                {
                    // Caster: nudge toward target along aim.
                    lightPoint += aim * 0.2f;
                }

                return true;

            case LightAttachSubject.Target:
            {
                Entity target = context != null ? context.TargetEntity : null;
                if (target == null)
                {
                    target = TryResolvePlayerTarget();
                }

                if (target != null)
                {
                    lightPoint = ResolveFaceOrCenter(target, aim, settings);
                    return true;
                }

                // Impact FX often has HitPointProxy owner — no TargetEntity.
                if (context != null && context.HasHitPoint)
                {
                    lightPoint = ApplyFaceOffsetToPoint(context.HitPoint, aim, settings, 0.35f);
                    return true;
                }

                return false;
            }

            case LightAttachSubject.HitPoint:
            default:
                if (context == null || !context.HasHitPoint)
                {
                    return false;
                }

                lightPoint = ApplyFaceOffsetToPoint(context.HitPoint, aim, settings, 0.35f);
                return true;
        }
    }

    private static Vector3 ResolveAimDirection(EffectResolveContext context)
    {
        if (context != null && context.HasHitDirection && context.HitDirection.sqrMagnitude > 0.0001f)
        {
            return FlatNormalized(context.HitDirection);
        }

        if (context != null &&
            context.CasterTransform != null &&
            context.TargetTransform != null)
        {
            Vector3 toTarget = FlatNormalized(context.TargetTransform.position - context.CasterTransform.position);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                return toTarget;
            }
        }

        if (context != null && context.HasHitPoint && PlayerEntity.Instance != null)
        {
            Vector3 toHit = FlatNormalized(context.HitPoint - PlayerEntity.Instance.transform.position);
            if (toHit.sqrMagnitude > 0.0001f)
            {
                return toHit;
            }
        }

        return Vector3.forward;
    }

    private static Vector3 ResolveFaceOrCenter(Entity entity, Vector3 aim, LightEffectSetting settings)
    {
        Vector3 center = ResolveEntityCenter(entity, entity != null ? entity.transform : null, Vector3.zero);
        if (!settings.useFaceOffset)
        {
            return center;
        }

        float radius = ResolveCollisionRadius(entity);
        return center - aim * (radius * Mathf.Max(0.01f, settings.faceOffsetRadiusScale));
    }

    private static Vector3 ApplyFaceOffsetToPoint(
        Vector3 point,
        Vector3 aim,
        LightEffectSetting settings,
        float fallbackRadius)
    {
        if (!settings.useFaceOffset)
        {
            return point;
        }

        return point - aim * (fallbackRadius * Mathf.Max(0.01f, settings.faceOffsetRadiusScale));
    }

    private static Vector3 ResolveEntityCenter(Entity entity, Transform transform, Vector3 fallback)
    {
        if (entity != null)
        {
            CharacterController controller = ResolveCharacterController(entity);
            if (controller != null)
            {
                return controller.transform.TransformPoint(controller.center);
            }

            if (entity.transform != null)
            {
                return entity.transform.position;
            }
        }

        if (transform != null)
        {
            return transform.position;
        }

        return fallback;
    }

    private static float ResolveCollisionRadius(Entity target)
    {
        if (target == null)
        {
            return 0.35f;
        }

        CharacterController controller = ResolveCharacterController(target);
        if (controller != null)
        {
            float scaleX = Mathf.Abs(controller.transform.lossyScale.x);
            float fromCapsule = controller.radius * scaleX;
            if (fromCapsule > 0.01f)
            {
                return fromCapsule;
            }
        }

        if (target.Appearance != null && target.Appearance.CollisionRadius > 0.0001f)
        {
            float raw = target.Appearance.CollisionRadius;
            float fromAppearance = raw > 3f ? VectorUtils.ConvertL2jDistance(raw) : raw;
            if (fromAppearance > 0.01f)
            {
                return fromAppearance;
            }
        }

        return 0.35f;
    }

    private static CharacterController ResolveCharacterController(Entity target)
    {
        if (target == null)
        {
            return null;
        }

        if (target is MonsterEntity monster)
        {
            CharacterController fromMonster = monster.GetCharacterController();
            if (fromMonster != null)
            {
                return fromMonster;
            }
        }
        else if (target is NpcEntity npc)
        {
            CharacterController fromNpc = npc.GetCharacterController();
            if (fromNpc != null)
            {
                return fromNpc;
            }
        }

        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = target.GetComponentInChildren<CharacterController>(true);
        }

        return controller;
    }

    private static Entity TryResolvePlayerTarget()
    {
        if (PlayerEntity.Instance == null || PlayerEntity.Instance.TargetId <= 0 || World.Instance == null)
        {
            return null;
        }

        return World.Instance.GetEntityNoLockSync(PlayerEntity.Instance.TargetId);
    }

    private static Vector3 FlatNormalized(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }
}
