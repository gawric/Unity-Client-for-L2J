

using UnityEngine;

public class StandIntention : IntentionBase
{
    public StandIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        Debug.Log("");
        //if (!_stateMachine.WaitingForServerReply)
        //{
          //  _stateMachine.SetWaitingForServerReply(true);
          //  Debug.Log("Здесь должно сработать Stand Intention");
           // GameClient.Instance.ClientPacketHandler.RequestActionUse((int)ActionType.Sit);
        //}
        //else
        //{
            PlayerController.Instance.StopMove();
        //}
    }

    public override void Exit() { }
    public override void Update()
    {
        //if (_stateMachine.WaitingForServerReply)
        //{
        //    PlayerController.Instance.StopMoving();
        //}
    }
}