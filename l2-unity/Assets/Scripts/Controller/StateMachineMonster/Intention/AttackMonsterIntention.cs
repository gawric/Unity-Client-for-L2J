using UnityEngine;

public class AttackMonsterIntention : MonsterIntentionBase
{
    public AttackMonsterIntention(MonsterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(Attack))
        {
  
                Attack attack = (Attack)arg0;

                Entity entity = World.Instance.GetEntityNoLockSync(attack.TargetId);
                Entity monster = _stateMachine.Entity;

                if (_stateMachine.State ==  MonsterState.WALKING | _stateMachine.State == MonsterState.RUNNING)
                {
                    Debug.Log("AttackMonsterIntention: Enter: " + _stateMachine.State);
                }



                if (entity == null) return;

                if(entity.GetType() == typeof(PlayerEntity))
                {
                    _stateMachine.SetTarget((PlayerEntity)entity);

                    MoveAllCharacters.Instance.AddRotate(monster.IdentityInterlude.Id, new RotateData(entity, monster));

                    _stateMachine.ChangeState(MonsterState.ATTACKING);
                    _stateMachine.NotifyEvent(Event.READY_TO_ACT);
                }

        }
 
    }

    public override void Exit() {
  
    }
    public override void Update()
    {
     
    }
}
