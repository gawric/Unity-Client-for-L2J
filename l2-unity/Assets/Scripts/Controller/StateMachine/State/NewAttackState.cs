using UnityEngine;

public class NewAttackState : StateBase
{
    public NewAttackState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }


    public override void Update()
    {

    }

    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.READY_TO_ACT:
                Debug.Log("Attack Sate to Intention> начало новой атакие пришел запрос от сервера");
                PlayerEntity.Instance.RefreshRandomPAttack();
                AnimationManager.Instance.PlayAnimation(PlayerEntity.Instance.RandomName.ToString(), true);
                break;
            case Event.CANCEL:
                Debug.Log("Attack Sate to Intention> Отмена скорее всего запрос пришел из ActionFaild");
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                break;

        }
    }
}