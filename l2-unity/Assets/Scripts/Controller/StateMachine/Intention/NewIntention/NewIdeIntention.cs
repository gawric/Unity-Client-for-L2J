using UnityEngine;

public class NewIdleIntention : IntentionBase
{
    public NewIdleIntention(PlayerStateMachine stateMachine) : base(stateMachine) { 
    }

    public override void Enter(object arg0)
    {
        _stateMachine.ChangeState(PlayerState.IDLE);
    }


    public override void Exit() { }
    public override void Update()
    {

    }
}
