using UnityEngine;

public class IdleIntention : IntentionBase
{
    public IdleIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        //if (_stateMachine.IsInMovableState())
       // {
          //  _stateMachine.ChangeState(PlayerState.IDLE);
        //}
       // else
        //{
         //   PlayerController.Instance.StopMove();
       // }
        // else if (!_stateMachine.WaitingForServerReply)
        //{
        //   _stateMachine.SetWaitingForServerReply(true);
        //   GameClient.Instance.ClientPacketHandler.UpdateMoveDirection(Vector3.zero);
        //}

    }
    

    public override void Exit() { }
    public override void Update()
    {

    }
}