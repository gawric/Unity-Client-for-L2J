using UnityEngine;

public class AttackMonsterIntention : MonsterIntentionBase
{
    public AttackMonsterIntention(MonsterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(Attack))
        {
           // PlayerEntity _target = (PlayerEntity)arg0;
            //_stateMachine.SetTarget(_target);
            //Vector3 tarPos = _target.transform.position;
            //Vector3 monsterPos = _stateMachine.MonsterObject.transform.position;
            //float dist = VectorUtils.Distance2D(tarPos, monsterPos);
            //10 metr distance tar and defens
            //if(dist == 0 | dist <= 10)
            //{
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

            //}
  
        }
 
    }

    public override void Exit() {
  
    }
    public override void Update()
    {
     
    }
}
