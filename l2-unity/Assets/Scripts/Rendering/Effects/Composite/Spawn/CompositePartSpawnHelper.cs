using UnityEngine;

public static class CompositePartSpawnHelper
{
    public const float HitDirectionYawOffsetDegrees = -90f;

    public static bool TryResolveAttachment(
        CompositePrefabPart part,
        EffectResolveContext context,
        IEffectAttachmentResolver resolver,
        out Transform resolvedTransform,
        out Vector3 worldPosition)
    {
        resolvedTransform = null;
        worldPosition = Vector3.zero;
        if (part == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(part.attachmentBoneName) &&
            context != null &&
            context.CasterEntity != null &&
            context.CasterEntity.Gear != null)
        {
            Transform bone = context.CasterEntity.Gear.FindRecursiveBone(part.attachmentBoneName);
            if (bone != null)
            {
                resolvedTransform = bone;
                worldPosition = bone.position;
                return true;
            }
        }

        if (resolver != null &&
            resolver.Resolve(part.attachmentPoint, context, out resolvedTransform, out worldPosition))
        {
            return true;
        }

        Debug.LogWarning($"CompositePrefabEffect: could not resolve point {part.attachmentPoint} for part {part.name}.");
        return false;
    }

    public static BaseEffect SpawnInstance(
        CompositePrefabPart part,
        Transform resolvedTransform,
        Vector3 worldPosition,
        Transform owner,
        EffectResolveContext context,
        bool useLegacyLifetimeHacks)
    {
        Vector3 adjustedOffset = GetAdjustedOffset(part, resolvedTransform, owner);
        Vector3 spawnPosition = CompositeEffectUtilities.ResolveSpawnPosition(
            resolvedTransform,
            worldPosition,
            adjustedOffset);
        Quaternion rotation = ResolveSpawnRotation(part, resolvedTransform, context);

        BaseEffect instance = Object.Instantiate(part.prefab, spawnPosition, rotation);
        instance.gameObject.SetActive(true);

        AttachIfFollow(part, resolvedTransform, instance.transform, adjustedOffset, worldPosition);
        ApplyScale(part, instance.transform);
        if (useLegacyLifetimeHacks)
        {
            ApplyShaderLifetimeOverride(part, instance.transform);
        }

        return instance;
    }

    public static Quaternion ResolveSpawnRotation(
        CompositePrefabPart part,
        Transform resolvedTransform,
        EffectResolveContext context)
    {
        if (context != null &&
            context.HasHitDirection &&
            context.HitDirection.sqrMagnitude > 0.0001f)
        {
            return Quaternion.LookRotation(context.HitDirection.normalized, Vector3.up) *
                   Quaternion.Euler(0f, HitDirectionYawOffsetDegrees, 0f);
        }

        return CompositeEffectUtilities.ResolveSpawnRotation(part != null && part.inheritRotation, resolvedTransform);
    }

    public static Transform ResolveSetupOwner(Transform resolvedTransform, Transform owner, Transform instanceTransform)
    {
        return resolvedTransform != null ? resolvedTransform : (owner != null ? owner : instanceTransform);
    }

    public static Vector3 GetAdjustedOffset(CompositePrefabPart part, Transform resolvedTransform, Transform owner)
    {
        if (part == null || !part.normalizeOffsetByOwnerHeight)
        {
            return part != null ? part.positionOffset : Vector3.zero;
        }

        float height = ResolveAttachmentHeight(resolvedTransform, owner);
        float reference = Mathf.Max(0.01f, part.referenceHeight);
        float multiplier = Mathf.Max(0.01f, height / reference);
        return part.positionOffset * multiplier;
    }

    public static void ApplyShaderTargetPosition(
        CompositePrefabPart part,
        Transform instanceTransform,
        EffectResolveContext context,
        IEffectAttachmentResolver resolver)
    {
        if (part == null || instanceTransform == null || !part.passShaderTargetPosition || resolver == null)
        {
            return;
        }

        if (!resolver.Resolve(part.shaderTargetAttachmentPoint, context, out Transform targetTransform, out Vector3 targetWorldPosition))
        {
            return;
        }

        Vector3 adjustedTargetWorldPosition = CompositeEffectUtilities.ResolveSpawnPosition(
            targetTransform,
            targetWorldPosition,
            part.shaderTargetPositionOffset);

        EffectPart[] effectParts = instanceTransform.GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < effectParts.Length; i++)
        {
            if (effectParts[i] != null)
            {
                effectParts[i].SetShaderTargetWorldPosOverride(true, adjustedTargetWorldPosition, targetTransform);
            }
        }
    }

    public static void ApplyShaderLifetimeOverride(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || !part.disableShaderLifetime || instanceTransform == null)
        {
            return;
        }

        EffectShaderLifetimeHelper.Apply(instanceTransform, false, 0.5f, 0.5f);
    }

    public static void ApplyHideTimeOverride(CompositePrefabPart part, EffectSettings partSettings)
    {
        if (part == null || partSettings == null || !part.overrideHideTime)
        {
            return;
        }

        float hide = part.customHideTime > 1e-4f
            ? part.customHideTime
            : (part.disableShaderLifetime ? 0.5f : 0f);
        partSettings.hideTime = Mathf.Max(0f, Mathf.Min(hide, partSettings.defaultLifeTime));
    }

    public static void ApplyScale(float scale, Transform instanceTransform)
    {
        if (instanceTransform == null || Mathf.Approximately(scale, 1f))
        {
            return;
        }

        instanceTransform.localScale *= scale;
    }

    public static void ApplyScale(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null)
        {
            return;
        }

        ApplyScale(part.scale, instanceTransform);
    }

    public static void ApplyLoopOverrides(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || instanceTransform == null)
        {
            return;
        }

        ParticleGroup[] groups = instanceTransform.GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            ParticleGroup group = groups[i];
            if (group != null)
            {
                group.SetRuntimeContinuousLoopOverride(part.overrideContinuousLoop, part.continuousLoop);
            }
        }

        ParticleSingle[] singles = instanceTransform.GetComponentsInChildren<ParticleSingle>(true);
        for (int i = 0; i < singles.Length; i++)
        {
            ParticleSingle single = singles[i];
            if (single != null)
            {
                single.SetRuntimeContinuousLoopOverride(part.overrideContinuousLoop, part.continuousLoop);
            }
        }
    }

    public static void ApplyHomeFlightOverrides(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || instanceTransform == null ||
            part.homeProjectile == null || !part.homeProjectile.IsEnabled)
        {
            return;
        }

        ParticleGroup[] groups = instanceTransform.GetComponentsInChildren<ParticleGroup>(true);
        bool hasAnchor = false;
        int anchorCount = 0;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].IsHomeFlightAnchor)
            {
                hasAnchor = true;
                anchorCount++;
            }
        }

        if (!hasAnchor && groups.Length > 0 && groups[0] != null)
        {
            groups[0].ApplyRuntimeHomeFlightProfile(ParticleGroupHomeFlightProfile.DefaultAnchor);
            hasAnchor = true;
        }

        if (!hasAnchor || !part.homeProjectile.mirrorDualFlight || anchorCount != 1)
        {
            return;
        }

        groups = instanceTransform.GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            ParticleGroup source = groups[i];
            if (source == null || !source.IsHomeFlightAnchor || source.name.EndsWith("_Mirror"))
            {
                continue;
            }

            GameObject mirrorObject = Object.Instantiate(source.gameObject, source.transform.parent);
            mirrorObject.name = source.gameObject.name + "_Mirror";
            Transform mirrorTransform = mirrorObject.transform;
            mirrorTransform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            mirrorTransform.localScale = source.transform.localScale;

            ParticleGroup mirrorGroup = mirrorObject.GetComponent<ParticleGroup>();
            if (mirrorGroup != null)
            {
                mirrorGroup.ApplyRuntimeHomeFlightProfile(ParticleGroupHomeFlightProfile.MirroredAnchor);
            }

            break;
        }
    }

    public static void AttachIfFollow(
        bool follow,
        Transform resolvedTransform,
        Transform instanceTransform,
        Vector3 resolvedWorldPosition)
    {
        if (!follow || instanceTransform == null || resolvedTransform == null)
        {
            return;
        }

        instanceTransform.SetParent(resolvedTransform, true);
        Vector3 localAnchor = resolvedTransform.InverseTransformPoint(resolvedWorldPosition);
        instanceTransform.localPosition = localAnchor;
    }

    public static void AttachIfFollow(
        CompositePrefabPart part,
        Transform resolvedTransform,
        Transform instanceTransform,
        Vector3 adjustedOffset,
        Vector3 resolvedWorldPosition)
    {
        if (part == null || instanceTransform == null || !part.followResolvedTransform || resolvedTransform == null)
        {
            return;
        }

        instanceTransform.SetParent(resolvedTransform, true);
        Vector3 localAnchor = resolvedTransform.InverseTransformPoint(resolvedWorldPosition);
        instanceTransform.localPosition = localAnchor + adjustedOffset;
    }

    public static float ResolveAttachmentHeight(Transform resolvedTransform, Transform owner)
    {
        Transform basis = resolvedTransform != null ? resolvedTransform : owner;
        if (basis == null)
        {
            return 1.8f;
        }

        CharacterController controller = basis.GetComponentInParent<CharacterController>();
        if (controller != null && controller.height > 0f)
        {
            return controller.height;
        }

        Renderer[] renderers = basis.GetComponentsInParent<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.y > 0.01f)
            {
                return bounds.size.y;
            }
        }

        return 1.8f;
    }
}
