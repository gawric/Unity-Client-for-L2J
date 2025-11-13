using UnityEngine;

public class MoveToIntention : IntentionBase
{
    public MoveToIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        //Debug.Log("State MoveToIntention 1 : " + _stateMachine.State);
    
        if (_stateMachine.State == PlayerState.SITTING 
            || _stateMachine.State == PlayerState.SIT_WAIT 
            || _stateMachine.State == PlayerState.STANDING 
            || _stateMachine.State == PlayerState.REBIRTH 
            || _stateMachine.State == PlayerState.DEAD)
        {
            if(_stateMachine.State != PlayerState.DEAD)
            {
                _stateMachine.ChangeIntention(Intention.INTENTION_STAND);
            }

            if (_stateMachine.State == PlayerState.REBIRTH)
            {
                _stateMachine.ChangeState(PlayerState.RUNNING);
            }

            return;
        }

     
        //move engine //0.1 default distance
        if (arg0 != null)
        {
            if(PlayerStateMachine.Instance.IsMoveToPawn) PlayerStateMachine.Instance.IsMoveToPawn = false;
            //Debug.Log("Runninin PathFindedController -2");
            //PathFinderController.Instance.MoveTo((Vector3)arg0 , 0.1f);
            PlayerController.Instance.MoveToPoint(new MovementTarget((Vector3)arg0 , 0.1f));
        }

 

        if (_stateMachine.State == PlayerState.IDLE)
        {
            //_stateMachine.ChangeState(PlayerState.RUNNING);
            _stateMachine.ChangeState(PlayerState.WALKING);
        }

        //Debug.Log("State MoveToIntention 2 : " + _stateMachine.State);

        //else attack mobs and click to move position(stop event attack and use running event)
        if (_stateMachine.State == PlayerState.ATTACKING)
        {
            _stateMachine.ChangeState(PlayerState.RUNNING);
        }

        //if (_stateMachine.IsInMovableState())
        //{
           // if (PlayerEntity.Instance.Running & !PlayerEntity.Instance.GetDead())
           // {
               // if (PlayerController.Instance.IsFirstRun())
               // {
               //     _stateMachine.ChangeState(PlayerState.WALKING);
               // }
               // else
               // {
                //    _stateMachine.ChangeState(PlayerState.RUNNING);
               // }
               
            //}
            //else
            //{
            //    _stateMachine.ChangeState(PlayerState.RUNNING);
           // }
        //}
    
    }

    public override void Exit() { }
    public override void Update()
    {
        //if (_stateMachine.WaitingForServerReply)
        //{
         //   PlayerController.Instance.StopMoving();
        //}
        //Debug.Log("M")
    }
}