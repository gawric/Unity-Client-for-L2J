using UnityEngine;

public class NewIdleState : StateBase
{
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
                HandleWaitReturn();
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

    private void HandleWaitReturn()
    {
        // Combat swing finished (or target died). Always leave attack latch and show wait pose.
        if (PlayerEntity.Instance != null)
        {
            PlayerEntity.Instance.IsAttack = false;
            PlayerEntity.Instance.LastAtkAnimation = null;
        }

        bool targetDead = IsCurrentTargetDead();
        // Dead target still uses atkwait_ (combat idle). AutoAttackStop may already
        // have cleared isAutoAttack — targetDead keeps the correct pose.
        bool useAtkWait = (_stateMachine.Player != null && _stateMachine.Player.isAutoAttack) || targetDead;
        var animation = useAtkWait ? AnimationNames.ATK_WAIT : AnimationNames.WAIT;

        // Defer one frame: CrossFade from inside JAtk OnStateUpdate is unreliable, and a
        // same-frame second CrossFade (Complete → CallBack) can leave the Animator stuck.
        PlayAnimationDeferred(animation);
    }

    private static bool IsCurrentTargetDead()
    {
        if (PlayerEntity.Instance == null || World.Instance == null)
        {
            return false;
        }

        int targetId = PlayerEntity.Instance.TargetId;
        if (targetId == 0)
        {
            return false;
        }

        Entity target = World.Instance.GetEntityNoLockSync(targetId);
        return target == null || target.IsDead();
    }


    private void PlayAnimation(Animation animation)
    {
        AnimationManager.Instance.PlayAnimation(_stateMachine.GetObjectId() , animation.ToString(), true);
    }

    private void PlayAnimationDeferred(Animation animation)
    {
        int objectId = _stateMachine.GetObjectId();
        string animName = animation.ToString();
        if (EventProcessor.Instance != null)
        {
            EventProcessor.Instance.QueueEvent(() =>
            {
                AnimationManager.Instance.PlayAnimation(objectId, animName, true);
            });
            return;
        }

        PlayAnimation(animation);
    }
}
