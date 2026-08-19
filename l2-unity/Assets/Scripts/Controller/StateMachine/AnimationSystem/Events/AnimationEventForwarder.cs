using UnityEngine;

/// <summary>
/// Unity Animation Events fire on the Animator GameObject.
/// PlayerAnimationController already sits on that GO; UserEntity/NPC keep
/// <see cref="AnimationEventsBase"/> on the root — this component forwards clip notifies.
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    [SerializeField] AnimationEventsBase _target;

    public void Bind(AnimationEventsBase target)
    {
        _target = target;
    }

    /// <summary>
    /// Attach on the Animator GO when the event receiver lives on a parent.
    /// No-op if the Animator already has <see cref="AnimationEventsBase"/>.
    /// </summary>
    public static void BindAnimator(IAnimationController controller)
    {
        if (controller == null)
            return;

        Animator animator = controller.GetAnimator();
        AnimationEventsBase target = controller as AnimationEventsBase;
        if (animator == null || target == null)
            return;

        BindAnimator(animator, target);
    }

    public static void BindAnimator(Animator animator, AnimationEventsBase target)
    {
        if (animator == null || target == null)
            return;

        if (animator.gameObject == target.gameObject)
            return;

        if (animator.GetComponent<AnimationEventsBase>() != null)
            return;

        AnimationEventForwarder forwarder = animator.GetComponent<AnimationEventForwarder>();
        if (forwarder == null)
            forwarder = animator.gameObject.AddComponent<AnimationEventForwarder>();
        forwarder.Bind(target);
    }

    AnimationEventsBase Resolve()
    {
        if (_target == null)
            _target = GetComponentInParent<AnimationEventsBase>();
        return _target;
    }

    public void AttackShot(string animationName)
    {
        Resolve()?.AttackShot(animationName);
    }

    public void OnAnimationHit(string animationName)
    {
        Resolve()?.OnAnimationHit(animationName);
    }

    public void OnAnimationAttackHitEnd(string animationName)
    {
        Resolve()?.OnAnimationAttackHitEnd(animationName);
    }

    public void OnAnimationComplete(string animationName)
    {
        Resolve()?.OnAnimationComplete(animationName);
    }

    public void OnAnimationShoot(string animationName)
    {
        Resolve()?.OnAnimationShoot(animationName);
    }

    public void OnAnimationLoadArrow(string animationName)
    {
        Resolve()?.OnAnimationLoadArrow(animationName);
    }
}
