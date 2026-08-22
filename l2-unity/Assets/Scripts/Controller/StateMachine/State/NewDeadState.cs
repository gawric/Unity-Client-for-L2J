public class NewDeadState : StateBase
{
    public NewDeadState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        if(IncomingPacketActions.Player.RunningToDestination) IncomingPacketActions.Player.StopMove();
    }
    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.DEAD:
                IncomingPacketActions.Animations.PlayOriginalAnimation(_stateMachine.GetObjectId() , AnimationNames.DEAD.ToString());
                break;
 
        }
    }
}