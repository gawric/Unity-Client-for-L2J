using UnityEngine;

public class FollowMonsterIntention : MonsterIntentionBase
{
    public FollowMonsterIntention(MonsterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(MoveToPawn))
        {

                MoveToPawn moveToPawn = (MoveToPawn)arg0;


                Entity pEntity = World.Instance.GetEntityNoLockSync(moveToPawn.TarObjid);


                if (pEntity.GetType() == typeof(PlayerEntity))
                {
                    if (!pEntity.GetDead() & !_stateMachine.Entity.GetDead())
                    {
                        PlayerEntity pEntity1 = pEntity as PlayerEntity;
                        _stateMachine.SetTarget(pEntity1);
                        //_stateMachine.MoveMonster.MoveToPawn(new MovementTarget(pEntity.transform, moveToPawn.Distance));
                        _stateMachine.Entity.Running = true;
                        _stateMachine.ChangeState(MonsterState.RUNNING);
                    }
                }
        }
        Debug.Log("Debug EVENT =========== MoveMonsterIntention");
    }

    public override void Exit()
    {
        Debug.Log("Debug EVENT =========== MoveMonsterIntention  EXIT");
    }
    public override void Update()
    {
        Debug.Log("Debug EVENT MoveMonsterIntention ATTACK");
    }
}
