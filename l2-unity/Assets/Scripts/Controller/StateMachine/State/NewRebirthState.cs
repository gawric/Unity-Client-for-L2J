public class NewRebirthState : StateBase
{
    public NewRebirthState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        
    }
    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.REBIRTH:
                AnimationManager.Instance.PlayOriginalAnimation(_stateMachine.GetObjectId() , AnimationNames.REBIRTH.ToString());
                _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                break;

        }
    }
}