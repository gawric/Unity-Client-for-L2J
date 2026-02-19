using FMOD.Studio;
using System.IO;
using UnityEngine;

public class NewMoveToPawnIntention : IntentionBase
{
    public NewMoveToPawnIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        
        if (arg0 != null)
        {
            
            ModelMovePawn model = (ModelMovePawn)arg0;
            Vector3 tarPosition = model.TarEntity().transform.position;
            Vector3 sourPosition = model.SourObj();
            float distance = VectorUtils.Distance2D(sourPosition, tarPosition);

            Debug.Log("NewMoveToPawnIntention > dist " + distance + " collision metr " + model.GetRange() + " collision dstance " + model.Distance());

            if (distance > model.GetRange())
            {
                if (distance > model.Distance())
                {
                    PlayerStateMachine.Instance.IsMoveToPawn = true;
                    PlayerController.Instance.MoveToPoint(new MovementTarget(model.TarEntity(), model.Distance() ));
                    StartAnimMoveTo();
                }

            }

        }
    }

    private void StartAnimMoveTo()
    {
        if (_stateMachine.State == PlayerState.IDLE)
        {
            _stateMachine.ChangeState(PlayerState.WALKING);
            _stateMachine.NotifyEvent(Event.MOVE_TO);
        }
    }


    public override void Exit() { }
    public override void Update()
    {

    }
}