using UnityEngine;

public class RunningState : StateBase
{
    public RunningState(PlayerStateMachine stateMachine) : base(stateMachine) { }



    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.ARRIVED:
                if (TargetManager.Instance.HasAttackTarget())
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
        if (!InputManager.Instance.Move && !PlayerController.Instance.RunningToDestination)
        {
            ////Debug.Log("Input event Move" + InputManager.Instance.Move);
            //Debug.Log("Input event RunningToDestination" + PlayerController.Instance.RunningToDestination);

            // Debug.Log("Character position : x " + PlayerController.Instance.GetPlayerPosition().x + " y " + PlayerController.Instance.GetPlayerPosition().y + PlayerController.Instance.GetPlayerPosition().z);
            SendValidatePosition(PlayerController.Instance.GetPlayerPosition());
            _stateMachine.NotifyEvent(Event.ARRIVED);
        }

        // If move input is pressed while running to target
        if (TargetManager.Instance.HasAttackTarget() && InputManager.Instance.Move)
        {
            // Cancel follow target
            TargetManager.Instance.ClearAttackTarget();
        }
    }

    private void SendValidatePosition(Vector3 playerPosition)
    {
        ValidatePosition sendPaket = CreatorPacketsUser.CreateValidatePosition(playerPosition.x, playerPosition.y, playerPosition.z);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
    }
}