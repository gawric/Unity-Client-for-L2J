
using UnityEngine;

public class IdleState : StateBase
{
    public IdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }


    public override void Update()
    {
        // Does the player want to move ?
        if (InputManager.Instance.Move || PlayerController.Instance.RunningToDestination && !TargetManager.Instance.HasAttackTarget())
        {
            //else dead player bug? code running point to dead
            //_stateMachine.ChangeIntention(Intention.INTENTION_MOVE_TO);
        }
        else if (PlayerController.Instance.RunningToDestination && TargetManager.Instance.HasAttackTarget())
        {
           // _stateMachine.ChangeIntention(Intention.INTENTION_FOLLOW);
        }
    }

    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.READY_TO_ACT:
                //if (TargetManager.Instance.HasAttackTarget() && !_stateMachine.WaitingForServerReply)
                //{
                    //Debug.Log("On Reaching Target");
                   // PathFinderController.Instance.ClearPath();
                   // PlayerController.Instance.ResetDestination();

                   // NetworkTransformShare.Instance.SharePosition();

                   // NetworkCharacterControllerShare.Instance.ForceShareMoveDirection();

                   // if (_stateMachine.State == PlayerState.ATTACKING) return;

                   // if (TargetManager.Instance.IsAttackTargetSet())
                   // {
                   //     _stateMachine.ChangeState(PlayerState.ATTACKING);
                    //    Debug.Log("Требуется реализация SendRequestAutoAttack -1 ");
                        //GameClient.Instance.ClientPacketHandler.SendRequestAutoAttack(-1);
                    //}
                   // else
                   // {
                   //     _stateMachine.ChangeState(PlayerState.ATTACKING);
                        //Debug.LogWarning("[StateMachine] Attacking a target which is not current target.");
                    //    Debug.Log("Требуется реализация SendRequestAutoAttack");
                        //GameClient.Instance.ClientPacketHandler.SendRequestAutoAttack(TargetManager.Instance.AttackTarget.Identity.Id);
                    //}

                    //_stateMachine.SetWaitingForServerReply(true);
                //}
                //else
                //{
                //    _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                //}
                break;

            case Event.WAIT_RETURN:
                {
                    PlayerAnimationController.Instance.SetBool("atkwait_1HS", true, true);
                    break;
                }

            case Event.DEAD:
                break;
       

        }
    }
}