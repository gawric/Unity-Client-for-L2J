using UnityEngine;

public class NewRunningState : StateBase
{
    public NewRunningState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }


    public override void Update()
    {
 
    }


    public override void HandleEvent(Event evt , object payload = null)
    {
        switch (evt)
        {
            case Event.ARRIVED:
                ArrivedToDestination();
                break;

            case Event.MOVE_TO:
                IncomingPacketActions.Animations.PlayAnimation(_stateMachine.GetObjectId() , AnimationNames.RUN.ToString(), true);
                break;

            case Event.CHANGE_EQUIP:
                IncomingPacketActions.Animations.PlayAnimation(_stateMachine.GetObjectId() , AnimationNames.RUN.ToString(), true);
                break;
        }
    }


    private void ArrivedToDestination()
    {
       _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
       _stateMachine.NotifyEvent(Event.ARRIVED);
    }
  
}
