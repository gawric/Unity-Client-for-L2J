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
        var animation = _stateMachine.Player.isAutoAttack
            ? AnimationNames.ATK_WAIT
            : AnimationNames.WAIT;
        Debug.Log("HandleArrival: NEW_IDLE_STATE " + animation.ToString());
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

        var animation = _stateMachine.Player != null && _stateMachine.Player.isAutoAttack
            ? AnimationNames.ATK_WAIT
            : AnimationNames.WAIT;
        PlayAnimation(animation);
        Debug.Log($"[SS_CHARGE_SM] HandleWaitReturn → {animation} IsAttack=false");
    }


    private void PlayAnimation(Animation animation)
    {
        AnimationManager.Instance.PlayAnimation(_stateMachine.GetObjectId() , animation.ToString(), true);
    }
}
