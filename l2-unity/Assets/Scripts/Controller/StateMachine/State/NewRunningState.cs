using UnityEngine;

public class NewRunningState : StateBase
{
    public NewRunningState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }


    public override void Update()
    {
 
    }


    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.ARRIVED:
                ArrivedToDestination();
                DebugLineDraw.RemoveDrawLineDebug(PlayerEntity.Instance.IdentityInterlude.Id);
                break;

            case Event.MOVE_TO:
                AnimationManager.Instance.PlayAnimation(AnimationNames.RUN.ToString(), true);
                break;

        }
    }


    private void ArrivedToDestination()
    {
        //if (PlayerStateMachine.Instance.IsAutoAttack == true)
        //{
         //   _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
         //   _stateMachine.NotifyEvent(Event.WAIT_RETURN);
        //}
        //else
        //{
            _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
            _stateMachine.NotifyEvent(Event.ARRIVED);
       //}

    }
    private void SendValidatePosition(Vector3 playerPosition)
    {
        ValidatePosition sendPaket = CreatorPacketsUser.CreateValidatePosition(playerPosition.x, playerPosition.y, playerPosition.z);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
    }
}
