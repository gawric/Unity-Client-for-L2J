public class NewDeadIntention : IntentionBase
{
    public NewDeadIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        if (arg0.GetType() == typeof(DieDto))
        {
            DieDto myModel = (DieDto)arg0;
            IncomingPacketActions.Buffer.RemoveAllEffects();
            IncomingPacketActions.Dead.ShowWindow();

            _stateMachine.ChangeState(PlayerState.DEAD);
            _stateMachine.NotifyEvent(Event.DEAD);

        }
    }

    public override void Exit() { }
    public override void Update()
    {

    }
}