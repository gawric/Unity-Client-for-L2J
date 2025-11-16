public class NewDeadIntention : IntentionBase
{
    public NewDeadIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        if (arg0.GetType() == typeof(Die))
        {
            Die myModel = (Die)arg0;
            BufferPanel.Instance.RemoveAllEffects();
            DeadWindow.Instance.ShowWindow();

            _stateMachine.ChangeState(PlayerState.DEAD);
            _stateMachine.NotifyEvent(Event.DEAD);

        }
    }

    public override void Exit() { }
    public override void Update()
    {

    }
}