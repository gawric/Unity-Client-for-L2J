using UnityEngine;

/// <summary>
/// L2 InitDecoEffect: one Emitter actor, AttachToBone(r_bone1/2/3).
/// Height is StartLocationRange.Z on that actor — do not split _ca/_oh as skill attach.
/// </summary>
public static class NpcDecoAttachment
{
    static readonly string[] BoneHints =
    {
        "r_bone1",
        "r_bone2",
        "r_bone3"
    };

    static readonly IEffectAttachmentResolver Resolver = new DefaultEffectAttachmentResolver();

    public static Transform Resolve(Entity entity)
    {
        return ResolveBone(entity) ?? (entity != null ? entity.transform : null);
    }

    public static Quaternion UprightYaw(Transform source)
    {
        Vector3 forward = source != null ? source.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.forward;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    public static bool TryResolve(
        Entity entity,
        NpcDecoAttachPoint point,
        out Transform parent,
        out Vector3 worldPosition)
    {
        parent = null;
        worldPosition = Vector3.zero;
        if (entity == null)
            return false;

        switch (point)
        {
            case NpcDecoAttachPoint.Feet:
                return TryResolver(
                    entity,
                    EffectAttachmentPoint.CasterRoot,
                    out parent,
                    out worldPosition);
            case NpcDecoAttachPoint.OverHead:
                return TryResolver(
                    entity,
                    EffectAttachmentPoint.TargetOverHead,
                    out parent,
                    out worldPosition);
            default:
                parent = ResolveBone(entity) ?? entity.transform;
                if (parent == null)
                    return false;
                worldPosition = parent.position;
                return true;
        }
    }

    public static NpcDecoAttachPoint FromPieceName(string pieceName)
    {
        if (string.IsNullOrEmpty(pieceName))
            return NpcDecoAttachPoint.Bone;

        string n = pieceName.ToLowerInvariant();
        // Skill _ca is cast timing, not feet. ra_boss_halo_a_ca is still bone-attached.
        if (n.EndsWith("_feet") || n.Contains("_feet_") || n.Contains("_ground"))
            return NpcDecoAttachPoint.Feet;
        if (n.EndsWith("_oh") || n.Contains("_oh_") || n.Contains("_head") ||
            n.Contains("overhead") || n.Contains("_hat"))
            return NpcDecoAttachPoint.OverHead;
        return NpcDecoAttachPoint.Bone;
    }

    static Transform ResolveBone(Entity entity)
    {
        if (entity == null)
            return null;

        Gear gear = entity.Gear;
        if (gear == null)
            return null;

        for (int i = 0; i < BoneHints.Length; i++)
        {
            Transform bone = gear.FindRecursiveBone(BoneHints[i]);
            if (bone != null)
                return bone;
        }

        return null;
    }

    static bool TryResolver(
        Entity entity,
        EffectAttachmentPoint point,
        out Transform parent,
        out Vector3 worldPosition)
    {
        var context = new EffectResolveContext
        {
            CasterEntity = entity,
            TargetEntity = entity,
            CasterTransform = entity.transform,
            TargetTransform = entity.transform
        };

        if (Resolver.Resolve(point, context, out parent, out worldPosition))
        {
            if (parent == null)
                parent = entity.transform;
            return true;
        }

        parent = entity.transform;
        worldPosition = entity.transform.position;
        return parent != null;
    }
}
