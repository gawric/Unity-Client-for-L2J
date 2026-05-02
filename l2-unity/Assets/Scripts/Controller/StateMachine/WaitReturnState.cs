using static AttackingState;

public class WaitReturnState : StateBase
{
    public WaitReturnState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void HandleEvent(Event evt, object payload = null)
    {
     
    }

    public override void Update()
    {
   
    }
}