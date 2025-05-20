
using UnityEditorInternal;
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
                Debug.Log("Attack Sate to Intention> ������ ����� ������ ������ ������ �� �������");
                PlayerEntity.Instance.RefreshRandomPAttack();
                AnimationManager.Instance.PlayAnimation(PlayerEntity.Instance.RandomName.ToString(), true);
                break;
            case Event.CANCEL:
                Debug.Log("Attack Sate to Intention> ������ ������ ����� ������ ������ �� ActionFaild");
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                break;

        }
    }
}