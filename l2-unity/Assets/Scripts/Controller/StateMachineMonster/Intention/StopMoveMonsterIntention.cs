using UnityEngine;

public class StopMoveMonsterIntention : MonsterIntentionBase
{
    public StopMoveMonsterIntention(MonsterStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(StopMove))
        {
            StopMove stopMovePacket = (StopMove)arg0;

            Debug.Log("Move Monstr посылаем запрос на остановку!   ");
            //_stateMachine.MoveMonster.CancelMove();
            MoveAllCharacters.Instance.CancelMove(_stateMachine.Entity.IdentityInterlude.Id);
            _stateMachine.ChangeState(MonsterState.IDLE);
            _stateMachine.NotifyEvent(Event.ARRIVED);
        }

    }

    public override void Exit()
    {

    }
    public override void Update()
    {

    }

}
