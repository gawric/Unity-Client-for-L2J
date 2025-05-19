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
            
            //Debug.Log("StopMove Dist " + VectorUtils.Distance2D(PlayerController.Instance.transform.position, stop.StopPos));

            PlayerStateMachine.Instance.IsMoveToPawn = false;
            PlayerController.Instance.StopMove();
            //Debug.Log("IsMoveToPawn ��������� � ���� stopmove1");

            //����� �� �� �������� �������� �� ����� � ��������� � ��������� ���� ��� ����� ������������ RunningState � ��� ��� �� ��������� �� ����� � ��� ����� ��������� �������� ����
            //� ����� ����� ������� � ����� idle ��� �� ������� ����� wait ���� ���������� ��������
            if (PlayerStateMachine.Instance.State == PlayerState.RUNNING)
            {
                //Debug.Log("IsMoveToPawn ��������� � ���� stopmove2");

                _stateMachine.NotifyEvent(Event.ARRIVED);
                _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerController.Instance.StopMove();
            }
            else
            {
                //if (PlayerStateMachine.Instance.IsMoveToPawn == true | PlayerController.Instance.RunningToDestination == true)
                //{
                  //  PlayerStateMachine.Instance.IsMoveToPawn = false;
                  //  _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                  //  PlayerController.Instance.StopMove();
                //}

            }

        }
    }




    public override void Exit() { }
    public override void Update()
    {

    }
}