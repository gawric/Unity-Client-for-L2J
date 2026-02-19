using static AttackingState;

public class WalkingState : StateBase
{
    public WalkingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.ARRIVED:
                if (TargetManager.Instance.HasAttackTarget())
                {
                 //   _stateMachine.ChangeIntention(Intention.INTENTION_ATTACK, AttackIntentionType.TargetReached);
                }
                else
                {
                    _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                }
                break;

        }
    }

    public override void Update()
    {
        //Arrived to destination
        if (!InputManager.Instance.Move && !PlayerController.Instance.RunningToDestination)
        {
            _stateMachine.NotifyEvent(Event.ARRIVED);
        }

        // If move input is pressed while running to target
        if (TargetManager.Instance.HasAttackTarget() && InputManager.Instance.Move)
        {
            // Cancel follow target
            TargetManager.Instance.ClearAttackTarget();
        }
    }
}