using UnityEngine;

public class RunningState : StateBase
{
    public RunningState(PlayerStateMachine stateMachine) : base(stateMachine) { }



    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.ARRIVED:
                if (IncomingPacketActions.Targets.HasAttackTarget())
                {
                    //_stateMachine.ChangeIntention(Intention.INTENTION_ATTACK, AttackIntentionType.TargetReached);
                }
                else
                {
                    //if(PlayerStateMachine.Instance.IsAutoAttack == true)
                    //{
                     //   _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                     //   _stateMachine.NotifyEvent(Event.WAIT_RETURN);
                   // }
                   // else
                   // {
                    //    _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                   // }
                }
                break;

        }
    }

    public override void Update()
    {
        //Arrived to destination
        if (!IncomingPacketActions.Input.Move && !IncomingPacketActions.Player.RunningToDestination)
        {
            ////Debug.Log("Input event Move" + InputManager.Instance.Move);
            //Debug.Log("Input event RunningToDestination" + PlayerController.Instance.RunningToDestination);

            // Debug.Log("Character position : x " + PlayerController.Instance.GetPlayerPosition().x + " y " + PlayerController.Instance.GetPlayerPosition().y + PlayerController.Instance.GetPlayerPosition().z);
            SendValidatePosition(IncomingPacketActions.Player.GetPlayerPosition());
            _stateMachine.NotifyEvent(Event.ARRIVED);
        }

        // If move input is pressed while running to target
        if (IncomingPacketActions.Targets.HasAttackTarget() && IncomingPacketActions.Input.Move)
        {
            // Cancel follow target
            IncomingPacketActions.Targets.ClearAttackTarget();
        }
    }

    private void SendValidatePosition(Vector3 playerPosition)
    {
        IncomingPacketActions.Game.Send(new ValidatePositionCommand(playerPosition.x, playerPosition.y, playerPosition.z));
    }
}