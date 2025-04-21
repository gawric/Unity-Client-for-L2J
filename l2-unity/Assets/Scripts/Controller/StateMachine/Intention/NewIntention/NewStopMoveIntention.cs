using System.IO;
using UnityEditorInternal;
using UnityEngine;

public class NewStopMoveIntention : IntentionBase
{
    public NewStopMoveIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        
        if (arg0 != null)
        {
            StopMove stop = (StopMove)arg0;
            
            Debug.Log("StopMove Dist " + VectorUtils.Distance2D(PlayerController.Instance.transform.position, stop.StopPos));
            //����� �� �� �������� �������� �� ����� � ��������� � ��������� ���� ��� ����� ������������ RunningState � ��� ��� �� ��������� �� ����� � ��� ����� ��������� �������� ����
            //� ����� ����� ������� � ����� idle ��� �� ������� ����� wait ���� ���������� ��������
            if (PlayerStateMachine.Instance.State == PlayerState.RUNNING)
            {
                _stateMachine.NotifyEvent(Event.ARRIVED);
                _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerController.Instance.StopMove();
            }
            else
            {
                if (PlayerStateMachine.Instance.IsMoveToPawn == true | PlayerController.Instance.RunningToDestination == true)
                {
                    _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                    PlayerController.Instance.StopMove();
                }

            }

        }
    }




    public override void Exit() { }
    public override void Update()
    {

    }
}