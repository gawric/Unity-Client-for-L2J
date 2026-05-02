using System.Linq;
using UnityEngine;

public interface IEffectAttachmentResolver
{
    bool Resolve(EffectAttachmentPoint point, EffectResolveContext context, out Transform resolvedTransform, out Vector3 worldPosition);
}

public class DefaultEffectAttachmentResolver : IEffectAttachmentResolver
{
    private static readonly string[] WeaponNamePattern = { Gear.weaponName }; // "weapon_"

    public bool Resolve(EffectAttachmentPoint point, EffectResolveContext context, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;

        if (!TryHydrateContext(context))
        {
            return false;
        }

        switch (point)
        {
            case EffectAttachmentPoint.CasterRoot:
                return ResolveRoot(context.CasterTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.CasterLowerBody:
                return ResolveLowerBody(context.CasterEntity, context.CasterTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.WeaponSocket:
                return ResolveWeapon(context.CasterEntity, context.CasterTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.TargetRoot:
                return ResolveRoot(context.TargetTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.TargetLowerBody:
                return ResolveLowerBody(context.TargetEntity, context.TargetTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.WorldHitPoint:
                if (context.HasHitPoint)
                {
                    worldPosition = context.HitPoint;
                    return true;
                }
                return false;
            case EffectAttachmentPoint.CasterPosition:
                if (context.CasterTransform != null)
                {
                    worldPosition = context.CasterTransform.position;
                    return true;
                }
                return false;
            case EffectAttachmentPoint.TargetPosition:
                if (context.TargetTransform != null)
                {
                    worldPosition = context.TargetTransform.position;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private bool TryHydrateContext(EffectResolveContext context)
    {
        if (context == null)
        {
            return false;
        }

        if (context.CasterEntity == null && context.CasterUserId > 0 && World.Instance != null)
        {
            context.CasterEntity = World.Instance.GetEntityNoLockSync(context.CasterUserId);
        }
        if (context.TargetEntity == null && context.TargetUserId > 0 && World.Instance != null)
        {
            context.TargetEntity = World.Instance.GetEntityNoLockSync(context.TargetUserId);
        }

        if (context.CasterTransform == null && context.CasterEntity != null)
        {
            context.CasterTransform = context.CasterEntity.transform;
        }
        if (context.TargetTransform == null && context.TargetEntity != null)
        {
            context.TargetTransform = context.TargetEntity.transform;
        }

        return context.CasterTransform != null || context.TargetTransform != null || context.HasHitPoint;
    }

    private bool ResolveRoot(Transform src, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        resolvedTransform = src;
        worldPosition = src != null ? src.position : Vector3.zero;
        return src != null;
    }

    private bool ResolveLowerBody(Entity entity, Transform fallbackRoot, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;

        // Prefer pelvis/hips first so "LowerBody" is near torso center, not near floor.
        Gear gear = entity != null ? entity.Gear : null;
        if (gear != null)
        {
            Transform pelvis = gear.FindRecursiveBone("Bip01_Pelvis") ?? gear.FindRecursiveBone("Bip01_Hips");
            if (pelvis != null)
            {
                // Keep lower-body spawn point, but follow root for stability across rigs.
                resolvedTransform = fallbackRoot != null ? fallbackRoot : pelvis;
                worldPosition = pelvis.position;
                return true;
            }

            Transform leftFoot = gear.FindRecursiveBone("Bip01_L_Foot");
            Transform rightFoot = gear.FindRecursiveBone("Bip01_R_Foot");
            if (leftFoot != null && rightFoot != null)
            {
                resolvedTransform = fallbackRoot;
                worldPosition = (leftFoot.position + rightFoot.position) * 0.5f;
                return true;
            }
        }

        Animator animator = entity != null ? entity.Animator : null;
        if (animator != null && animator.isHuman)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                // Keep lower-body spawn point, but follow root for stability across rigs.
                resolvedTransform = fallbackRoot != null ? fallbackRoot : hips;
                worldPosition = hips.position;
                return true;
            }

            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            if (left != null && right != null)
            {
                resolvedTransform = fallbackRoot;
                worldPosition = (left.position + right.position) * 0.5f;
                return true;
            }
        }

        if (fallbackRoot != null)
        {
            worldPosition = fallbackRoot.position;
            return true;
        }

        return false;
    }

    private bool ResolveWeapon(Entity entity, Transform fallbackRoot, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;

        if (entity is PlayerEntity playerEntity)
        {
            Transform weapon = playerEntity.GetWeaponTransform();
            if (weapon != null)
            {
                resolvedTransform = weapon;
                worldPosition = weapon.position;
                return true;
            }
        }

        // Generic path for both Player/Monster (and anything with Gear):
        // search under right-hand bone for "weapon_" objects using Gear's naming search.
        Gear gear = entity != null ? entity.Gear : null;
        if (gear != null)
        {
            Transform guessed = gear.GetAllTransformByRightHand(WeaponNamePattern).FirstOrDefault();
            if (guessed != null)
            {
                resolvedTransform = guessed;
                worldPosition = guessed.position;
                return true;
            }
        }

        if (entity != null)
        {
            Transform guessed = entity.transform.Find("weapon_");
            if (guessed != null)
            {
                resolvedTransform = guessed;
                worldPosition = guessed.position;
                return true;
            }
        }

        if (fallbackRoot != null)
        {
            resolvedTransform = fallbackRoot;
            worldPosition = fallbackRoot.position;
            return true;
        }

        return false;
    }
}
