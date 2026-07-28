using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class NewMoveToIntention : IntentionBase
{
    public NewMoveToIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        //move engine //0.1 default distance
        if (arg0 != null)
        {
            var moveToLocation = (CharMoveToLocation)arg0;
            Vector3 newPosition = moveToLocation.NewPosition;
            Vector3 oldPosition = moveToLocation.OldPosition;

           // Vector3 oldVector = new Vector3(oldPosition.x, PlayerController.Instance.transform.position.y, oldPosition.z);
            //Vector3 newVector = new Vector3(newPosition.x, PlayerController.Instance.transform.position.y, newPosition.z);


            //float distTest = VectorUtils.Distance2D(PlayerController.Instance.transform.position, oldVector);
            //PlayerController.Instance.transform.position = oldVector;
            //float distTest2 = VectorUtils.Distance2D(PlayerController.Instance.transform.position, oldVector);

            DebugLineDraw.ShowDrawLineDebug(PlayerEntity.Instance.IdentityInterlude.Id, oldPosition, newPosition, Color.red);

            float dist = VectorUtils.Distance2D(PlayerController.Instance.transform.position, newPosition);

            //Debug.Log("NewMoveToIntention> to dict " + distTest + " original " + distTest2);

            if (dist <= 0.12f) return;

            if (PlayerStateMachine.Instance.IsMoveToPawn) PlayerStateMachine.Instance.IsMoveToPawn = false;

            
            //PlayerController.Instance.MoveToPoint((Vector3)arg0, 0.1f);
            PlayerController.Instance.MoveToPoint(new MovementTarget(newPosition, 0.1f , PlayerEntity.Instance.Running));
            StartAnimMoveTo();
        }
    }

    private void StartAnimMoveTo()
    {
        PlayerState before = _stateMachine.State;
        bool runningFlag = PlayerEntity.Instance != null && PlayerEntity.Instance.Running;

        if (_stateMachine.State == PlayerState.IDLE |
            _stateMachine.State == PlayerState.ATTACKING |
            _stateMachine.State == PlayerState.PHYSICAL_SKILLS)
        {
            // Leaving combat/skills for locomotion — clear attack latch so WAIT_RETURN/ATK_WAIT don't stick.
            if (PlayerEntity.Instance != null)
            {
                PlayerEntity.Instance.IsAttack = false;
            }

            Debug.Log(
                $"[SS_CHARGE_SM] MoveTo.StartAnimMoveTo beforeState={before} " +
                $"player.Running={runningFlag} → ChangeState(WALKING) + MOVE_TO");

            _stateMachine.ChangeState(PlayerState.WALKING);
            _stateMachine.NotifyEvent(Event.MOVE_TO);
        }
        else if (_stateMachine.State == PlayerState.RUNNING ||
                 _stateMachine.State == PlayerState.WALKING)
        {
            // Already locomoting — still re-fire MOVE_TO so RUN/WALK anim restarts
            // if ATK_WAIT / attack end left the animator on wait while state stayed RUNNING.
            Debug.Log(
                $"[SS_CHARGE_SM] MoveTo.StartAnimMoveTo beforeState={before} " +
                $"player.Running={runningFlag} → keep state, re-Notify MOVE_TO (refresh anim)");

            _stateMachine.NotifyEvent(Event.MOVE_TO);
        }
        else
        {
            Debug.Log(
                $"[SS_CHARGE_SM] MoveTo.StartAnimMoveTo beforeState={before} " +
                $"player.Running={runningFlag} → NO anim refresh");
        }
    }


    public override void Exit() { }
    public override void Update()
    {

    }
}