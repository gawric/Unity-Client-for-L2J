using UnityEngine;

public class MoveToMonsterIntention : MonsterIntentionBase
{
    private float defaultIgnore = 0.12f;
    public MoveToMonsterIntention(MonsterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(Vector3))
        {
            Vector3 _targetPos = (Vector3)arg0;
  
            MonsterEntity monsterEntity = (MonsterEntity)_stateMachine.Entity;
            float distance = VectorUtils.Distance2D(monsterEntity.transform.position, _targetPos);

            if (distance > defaultIgnore)
            {
                // _stateMachine.MoveMonster.MoveToTargetPosition(new MovementTarget(_targetPos , 0.1f));
                MovementTarget movementTarget = new MovementTarget(_targetPos, 0.1f);
                MoveAllCharacters.Instance.AddMoveData(_stateMachine.Entity.IdentityInterlude.Id, new MovementData(monsterEntity, movementTarget));

                if (monsterEntity.Running)
                {
                    _stateMachine.ChangeState(MonsterState.RUNNING);
                    _stateMachine.NotifyEvent(Event.MOVE_TO);
                }
                else
                {
                    _stateMachine.ChangeState(MonsterState.WALKING);
                    _stateMachine.NotifyEvent(Event.MOVE_TO);
                }

               
            }  
        }

    }

    public override void Exit()
    {

    }
    public override void Update()
    {

    }

}
