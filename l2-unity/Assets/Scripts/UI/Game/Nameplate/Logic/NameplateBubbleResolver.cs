using UnityEngine;

/// <summary>
/// Maps hover / target / melee-attack state to L2 TargetRenderType bubbles.
/// </summary>
public sealed class NameplateBubbleResolver
{
    public L2TargetRenderType ResolveBubbleType(Transform target)
    {
        if (target == null)
        {
            return L2TargetRenderType.None;
        }

        if (TargetManager.Instance != null &&
            TargetManager.Instance.HasTarget() &&
            TargetManager.Instance.Target.Data != null &&
            TargetManager.Instance.Target.Data.ObjectTransform == target)
        {
            if (TargetManager.Instance.IsAttackTargetSet())
            {
                return L2TargetRenderType.Attack;
            }

            return L2TargetRenderType.Target;
        }

        if (ClickManager.Instance != null &&
            ClickManager.Instance.HoverObjectData != null &&
            ClickManager.Instance.HoverObjectData.ObjectTransform == target)
        {
            return L2TargetRenderType.Normal;
        }

        return L2TargetRenderType.None;
    }

    /// <summary>
    /// Local player: suppress grey hover bubble; keep Target/Attack (e.g. StatusWindow self-select).
    /// </summary>
    public L2TargetRenderType ResolveForPaint(Transform target, bool isLocalPlayer)
    {
        L2TargetRenderType type = ResolveBubbleType(target);
        if (isLocalPlayer && type == L2TargetRenderType.Normal)
        {
            return L2TargetRenderType.None;
        }

        return type;
    }

    public bool IsHoverOrTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (ClickManager.Instance != null &&
            ClickManager.Instance.HoverObjectData != null &&
            ClickManager.Instance.HoverObjectData.ObjectTransform == target)
        {
            return true;
        }

        if (TargetManager.Instance != null &&
            TargetManager.Instance.HasTarget() &&
            TargetManager.Instance.Target.Data.ObjectTransform == target)
        {
            return true;
        }

        return false;
    }
}
