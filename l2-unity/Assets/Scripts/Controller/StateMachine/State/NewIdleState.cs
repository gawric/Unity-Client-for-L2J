public class NewIdleState : StateBase
{
    public NewIdleState(PlayerStateMachine stateMachine) : base(stateMachine) {
    }


    public override void Update()
    {
        
    }

    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.READY_TO_ACT:
                break;
            case Event.ENTER_WORLD:
                AnimationManager.Instance.PlayAnimation(AnimationNames.WAIT.ToString() , true);
                break;
            case Event.ARRIVED:
                Arrived(_stateMachine, AnimationManager.Instance);
                break;
            case Event.WAIT_RETURN:
                AnimationManager.Instance.PlayAnimation(AnimationNames.ATK_WAIT.ToString(), true);
                break;

        }
    }

    private void Arrived(PlayerStateMachine stateMachine , IAnimationManager animationManager)
    {
        if (stateMachine.Player.isAutoAttack)
        {
            animationManager.PlayAnimation(AnimationNames.ATK_WAIT.ToString(), true);
        }
        else
        {
            animationManager.PlayAnimation(AnimationNames.WAIT.ToString(), true);
        }
    }
}