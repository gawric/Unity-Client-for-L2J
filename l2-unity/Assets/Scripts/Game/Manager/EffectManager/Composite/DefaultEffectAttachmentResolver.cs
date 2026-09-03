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
                return ResolveWeapon(context.CasterEntity, context.CasterTransform, leftHand: false, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.LeftWeaponSocket:
                return ResolveWeapon(context.CasterEntity, context.CasterTransform, leftHand: true, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.TargetRoot:
                return ResolveRoot(context.TargetTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.TargetLowerBody:
                return ResolveLowerBody(context.TargetEntity, context.TargetTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.TargetCenter:
                return ResolveTargetCenter(context.TargetEntity, context.TargetTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.TargetOverHead:
                return ResolveTargetOverHead(context.TargetEntity, context.TargetTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.CasterCenter:
                return ResolveTargetCenter(context.CasterEntity, context.CasterTransform, out resolvedTransform, out worldPosition);
            case EffectAttachmentPoint.WorldHitPoint:
            {
                if (context.HasHitPoint)
                {
                    worldPosition = context.HitPoint;
                    return true;
                }

                Transform fallbackTarget = context.TargetTransform != null
                    ? context.TargetTransform
                    : context.CasterTransform;
                Entity fallbackEntity = context.TargetEntity != null
                    ? context.TargetEntity
                    : context.CasterEntity;
                Transform hitAnchor = HitAnchorResolver.ResolveHitAnchor(fallbackEntity, fallbackTarget);
                if (hitAnchor == null)
                {
                    return false;
                }

                worldPosition = hitAnchor.position;
                return true;
            }
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

    private bool ResolveTargetCenter(Entity entity, Transform fallbackRoot, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;

        if (fallbackRoot == null)
        {
            return false;
        }

        resolvedTransform = fallbackRoot;

        CharacterController controller = ResolveCharacterController(entity, fallbackRoot);

        if (controller != null)
        {
            worldPosition = controller.transform.TransformPoint(controller.center);
            return true;
        }

        Transform searchRoot = entity != null ? entity.transform : fallbackRoot;
        Renderer[] renderers = searchRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (bounds.size.sqrMagnitude > 0.00001f)
            {
                worldPosition = bounds.center;
                return true;
            }
        }

        worldPosition = fallbackRoot.position;
        return true;
    }

    private bool ResolveTargetOverHead(Entity entity, Transform fallbackRoot, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;

        if (fallbackRoot == null)
        {
            return false;
        }

        resolvedTransform = fallbackRoot;

        float collisionHeight = L2NameplateAnchor.DefaultCollisionHeightMeters;
        if (entity != null && entity.Appearance != null)
        {
            collisionHeight = entity.Appearance.CollisionHeight;
        }

        // Same call as NameplateEntryStore / NameplatesManager._headHeightOffset.
        worldPosition = L2NameplateAnchor.GetHeadWorldPos(
            fallbackRoot,
            collisionHeight,
            L2NameplateAnchor.DefaultHeadHeightOffsetMeters);
        Debug.Log(
            $"[HOME_SPAWN] ResolveTargetOverHead entity='{(entity != null ? entity.name : "null")}' " +
            $"nameplate={worldPosition:F3} dY={(worldPosition.y - fallbackRoot.position.y):F3}");
        return true;
    }

    private CharacterController ResolveCharacterController(Entity entity, Transform fallbackRoot)
    {
        CharacterController controller = null;
        if (entity != null)
        {
            controller = entity.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = entity.GetComponentInChildren<CharacterController>(true);
            }
        }

        if (controller == null && fallbackRoot != null)
        {
            controller = fallbackRoot.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = fallbackRoot.GetComponentInChildren<CharacterController>(true);
            }
        }

        return controller;
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

        // Prefer pelvis/hips for choosing follow transform when root is missing; world anchor matches follow pivot
        // when following entity root so Composite prefab positionOffset (tuned as root-local TransformPoint) stays valid.
        // For bone-centered hits without that offset, use TargetCenter / WorldHitPoint instead.
        Gear gear = entity != null ? entity.Gear : null;
        if (gear != null)
        {
            Transform pelvis = gear.FindRecursiveBone("Bip01_Pelvis") ?? gear.FindRecursiveBone("Bip01_Hips");
            if (pelvis != null)
            {
                resolvedTransform = fallbackRoot != null ? fallbackRoot : pelvis;
                worldPosition = resolvedTransform.position;
                return true;
            }

            Transform leftFoot = gear.FindRecursiveBone("Bip01_L_Foot");
            Transform rightFoot = gear.FindRecursiveBone("Bip01_R_Foot");
            if (leftFoot != null && rightFoot != null)
            {
                resolvedTransform = fallbackRoot;
                worldPosition = resolvedTransform != null
                    ? resolvedTransform.position
                    : (leftFoot.position + rightFoot.position) * 0.5f;
                return true;
            }
        }

        Animator animator = entity != null ? entity.Animator : null;
        if (animator != null && animator.isHuman)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                resolvedTransform = fallbackRoot != null ? fallbackRoot : hips;
                worldPosition = resolvedTransform.position;
                return true;
            }

            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            if (left != null && right != null)
            {
                resolvedTransform = fallbackRoot;
                worldPosition = resolvedTransform != null
                    ? resolvedTransform.position
                    : (left.position + right.position) * 0.5f;
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

    private bool ResolveWeapon(
        Entity entity,
        Transform fallbackRoot,
        bool leftHand,
        out Transform resolvedTransform,
        out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;

        if (!leftHand && entity is PlayerEntity playerEntity)
        {
            Transform weapon = playerEntity.GetWeaponTransform();
            if (weapon != null)
            {
                resolvedTransform = weapon;
                worldPosition = weapon.position;
                return true;
            }
        }

        Gear gear = entity != null ? entity.Gear : null;
        if (gear != null)
        {
            Transform[] found = leftHand
                ? gear.GetAllTransformByLeftHand(WeaponNamePattern)
                : gear.GetAllTransformByRightHand(WeaponNamePattern);
            Transform guessed = found != null ? found.FirstOrDefault() : null;
            if (guessed != null)
            {
                resolvedTransform = guessed;
                worldPosition = guessed.position;
                return true;
            }
        }

        if (!leftHand && entity != null)
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
