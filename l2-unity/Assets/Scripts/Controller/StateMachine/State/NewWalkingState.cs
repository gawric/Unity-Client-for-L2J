public class NewWalkingState : StateBase
{
    public NewWalkingState(PlayerStateMachine stateMachine) : base(stateMachine)
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
                    DebugLineDraw.RemoveDrawLineDebug(PlayerEntity.Instance.Identity.Id);
                    break;
                case Event.MOVE_TO:
                    IncomingPacketActions.Animations.PlayAnimation(_stateMachine.GetObjectId() , AnimationNames.WALK.ToString(), true);
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
}