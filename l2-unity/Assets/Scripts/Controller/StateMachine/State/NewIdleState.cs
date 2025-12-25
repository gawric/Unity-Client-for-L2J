using UnityEditorInternal;
using UnityEngine;

public class NewIdleState : StateBase
{
    public NewIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Update() { }

    public override void HandleEvent(Event evt)
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
        PlayAnimation(animation);
    }

    private void HandleWaitReturn()
    {
        PlayAnimation(AnimationNames.ATK_WAIT);
        PlayerEntity.Instance.LastAtkAnimation = null;
    }

    private void StopPreviousEquipAnimation()
    {
        string lastAnimName = _stateMachine.Player.GetLastAnimName();
        Debug.Log("StopPreviousEquipAnimation>>> stop " + lastAnimName);
        if (!string.IsNullOrEmpty(lastAnimName))
        {
            string paramName = AnimationNames.WAIT.Concat(lastAnimName);
            Debug.Log("StopPreviousEquipAnimation>>> start " + paramName);
            AnimationManager.Instance.StopCurrentAnimation(paramName);
        }
    }

    private void PlayAnimation(Animation animation)
    {
        AnimationManager.Instance.PlayAnimation(animation.ToString(), true);
    }
}
