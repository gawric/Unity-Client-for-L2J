public class DeadState : StateBase
{
    public DeadState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        PlayerController.Instance.StopMove();
    }
    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.DEAD:
                AnimationManager.Instance.PlayAnimation(_stateMachine.Player.IdentityInterlude.Id , AnimationNames.RUN.ToString(), true);
                break;
        }
    }
}