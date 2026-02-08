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
        if (!_stateMachine.Player.IsAttack)
        {
            PlayAnimation(AnimationNames.ATK_WAIT);
            Debug.Log("HandleArrival: Handle_Wait_Return ");
        }

        PlayerEntity.Instance.LastAtkAnimation = null;
    }


    private void PlayAnimation(Animation animation)
    {
        AnimationManager.Instance.PlayAnimation(_stateMachine.GetObjectId() , animation.ToString(), true);
    }
}
