using UnityEngine;

public class NewIdleState : StateBase
{
    private const float CombatSwingNormGate = 0.95f;

    /// <summary>
    /// Payload from JAtk/SpAtk/Magic SMB SwitchToIdle. Wall clock may end before animNorm hits
    /// CombatSwingNormGate — still apply atkwait/wait (do not treat as early interrupt).
    /// </summary>
    public const string WaitReturnFromCombatSmb = "smb_finished";

    public NewIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Update() { }

    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.ENTER_WORLD:
                HandleEnterWorld();
                break;
            case Event.CHANGE_EQUIP:
                HandleEquipChange();
                break;
            case Event.ARRIVED:
                HandleArrival();
                break;
            case Event.WAIT_RETURN:
                HandleWaitReturn(payload);
                break;
        }
    }

    private void HandleEnterWorld()
    {
        PlayAnimation(AnimationNames.WAIT);
    }

    private void HandleEquipChange()
    {
        var animation = _stateMachine.Player.isAutoAttack
            ? AnimationNames.ATK_WAIT
            : AnimationNames.WAIT;
        PlayAnimation(animation);

    }

    private void HandleArrival()
    {
        bool targetDead = IsCurrentTargetDead();
        bool useAtkWait = _stateMachine.Player.isAutoAttack || targetDead;
        var animation = useAtkWait ? AnimationNames.ATK_WAIT : AnimationNames.WAIT;
        PlayAnimation(animation);
    }

    private void HandleWaitReturn(object payload = null)
    {
        // Combat swing finished (or target died). Always leave attack latch and show wait pose.
        if (PlayerEntity.Instance != null)
        {
            PlayerEntity.Instance.IsAttack = false;
            PlayerEntity.Instance.LastAtkAnimation = null;
        }

        bool fromCombatSmb = IsWaitReturnFromCombatSmb(payload);
        bool targetDead = IsCurrentTargetDead();
        // Dead target still uses atkwait_ (combat idle). AutoAttackStop may already
        // have cleared isAutoAttack — targetDead keeps the correct pose.
        bool useAtkWait = (_stateMachine.Player != null && _stateMachine.Player.isAutoAttack) || targetDead;
        var animation = useAtkWait ? AnimationNames.ATK_WAIT : AnimationNames.WAIT;

        // Skip only early external WAIT_RETURN (DieDto / skill Complete) while swing still plays.
        // SMB SwitchToIdle uses wall clock — animNorm often < 0.95 when it fires; must CrossFade.
        if (!fromCombatSmb && TryGetActiveCombatSwing(out _, out _))
        {
            return;
        }

        // Defer one frame: CrossFade from inside JAtk OnStateUpdate is unreliable, and a
        // same-frame second CrossFade (Complete → CallBack) can leave the Animator stuck.
        PlayAnimationDeferred(animation, fromCombatSmb);
    }

    private static bool IsWaitReturnFromCombatSmb(object payload)
    {
        if (payload is string s &&
            string.Equals(s, WaitReturnFromCombatSmb, System.StringComparison.Ordinal))
        {
            return true;
        }

        return payload is bool b && b;
    }

    private static bool IsCurrentTargetDead()
    {
        if (PlayerEntity.Instance == null || IncomingPacketActions.GameWorld == null)
        {
            return false;
        }

        int targetId = PlayerEntity.Instance.TargetId;
        if (targetId == 0)
        {
            return false;
        }

        Entity target = IncomingPacketActions.GameWorld.GetEntityNoLockSync(targetId);
        return target == null || target.IsDead();
    }

    /// <summary>
    /// True while jatk/SpAtk (and similar) SMB swing is still playing below exit gate.
    /// Prevents early WAIT_RETURN / DieDto from CrossFading atkwait mid-dual.
    /// </summary>
    private bool TryGetActiveCombatSwing(out string swingState, out float swingNorm)
    {
        swingState = null;
        swingNorm = 0f;

        int objectId = _stateMachine.GetObjectId();
        string recent = IncomingPacketActions.Animations != null
            ? IncomingPacketActions.Animations.GetCurrentAnimationName(objectId)
            : null;
        if (!IsCombatSwingStateName(recent))
        {
            return false;
        }

        IAnimationController controller = null;
        if (IncomingPacketActions.Animations is AnimationManager concrete)
        {
            controller = concrete.GetPlayerController(objectId);
        }
        else if (PlayerAnimationController.Instance != null)
        {
            controller = PlayerAnimationController.Instance;
        }

        Animator animator = controller != null ? controller.GetAnimator() : null;
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(recent) && current.normalizedTime < CombatSwingNormGate)
        {
            swingState = recent;
            swingNorm = current.normalizedTime;
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsName(recent) && next.normalizedTime < CombatSwingNormGate)
            {
                swingState = recent;
                swingNorm = next.normalizedTime;
                return true;
            }
        }

        return false;
    }

    private static bool IsCombatSwingStateName(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        return stateName.StartsWith("jatk", System.StringComparison.OrdinalIgnoreCase)
            || stateName.StartsWith("SpAtk", System.StringComparison.OrdinalIgnoreCase);
    }

    private void PlayAnimation(Animation animation)
    {
        IncomingPacketActions.Animations.PlayAnimation(_stateMachine.GetObjectId() , animation.ToString(), true);
    }

    private void PlayAnimationDeferred(Animation animation, bool fromCombatSmb = false)
    {
        int objectId = _stateMachine.GetObjectId();
        string animName = animation.ToString();
        IncomingPacketActions.Queue(() =>
        {
            // Re-check only for early external WAIT_RETURN; SMB wall-finish must CrossFade.
            if (!fromCombatSmb && TryGetActiveCombatSwing(out _, out _))
            {
                return;
            }

            IncomingPacketActions.Animations.PlayAnimation(objectId, animName, true);
        });
    }
}
