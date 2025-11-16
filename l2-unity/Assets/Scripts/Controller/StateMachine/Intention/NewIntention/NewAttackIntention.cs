using UnityEngine;

public class NewAttackIntention : IntentionBase
{
    public NewAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        if (!IsSuccessAttack(arg0))
        {
            _stateMachine.ChangeIntention(Intention.INTENTION_IDLE); 
            return;
        }
        


        if (arg0.GetType() == typeof(Attack))
        {
            Debug.Log("NewAttackIntention > State " + PlayerStateMachine.Instance.State);
            Attack myModel = (Attack)arg0;

            Entity entity = World.Instance.GetEntityNoLockSync(myModel.TargetId);
            PlayerController.Instance.RotateToAttacker(entity.transform.position);

            PlayerEntity.Instance.IsAttack = true;

            _stateMachine.ChangeState(PlayerState.ATTACKING);
            _stateMachine.NotifyEvent(Event.READY_TO_ACT);

        }
    }



    public override void Exit() { }
    public override void Update()
    {

    }
}