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
            
            if (_stateMachine.State == PlayerState.DEAD)
                return;

            ModelMovePawn model = (ModelMovePawn)arg0;
            if (model.TarEntity() == null)
                return;

            Vector3 tarPosition = model.TarEntity().transform.position;
            Vector3 sourPosition = model.SourObj();
            float distance = VectorUtils.Distance2D(sourPosition, tarPosition);

            // APawn::ReachedDestination on a pawn goal replaces CollisionRadius with
            // MoveToPawnAdjust and then adds DestinationOffset. The packet distance is that adjust.
            // Debug.Log("NewMoveToPawnIntention > dist " + distance + " stopDist " + model.Distance());

            if (distance > model.Distance())
            {
                // Stop ring is the packet distance. ReachedDestination does not add collision radii for a pawn goal.
                L2PawnRange.IgnorePawnCollision(PlayerEntity.Instance, model.TarEntity());
                PlayerStateMachine.Instance.IsMoveToPawn = true;
                bool running = PlayerEntity.Instance != null && PlayerEntity.Instance.Running;
                IncomingPacketActions.Player.MoveToPoint(new MovementTarget(model.TarEntity().transform, model.Distance(), running));
                StartAnimMoveTo();
            }
            else
            {
                L2PawnRange.ClearIgnoredPawn(PlayerEntity.Instance);
                if (_stateMachine.State == PlayerState.SITTING ||
                    _stateMachine.State == PlayerState.SIT_WAIT ||
                    _stateMachine.State == PlayerState.STANDING)
                {
                    _stateMachine.ChangeState(PlayerState.IDLE);
                    _stateMachine.NotifyEvent(Event.ARRIVED);
                }
            }

        }
    }

    private void StartAnimMoveTo()
    {
        if (_stateMachine.State == PlayerState.IDLE ||
            _stateMachine.State == PlayerState.ATTACKING ||
            _stateMachine.State == PlayerState.PHYSICAL_SKILLS ||
            _stateMachine.State == PlayerState.MAGIC_SKILLS ||
            _stateMachine.State == PlayerState.SITTING ||
            _stateMachine.State == PlayerState.SIT_WAIT ||
            _stateMachine.State == PlayerState.STANDING)
        {
            if (PlayerEntity.Instance != null)
                PlayerEntity.Instance.IsAttack = false;

            _stateMachine.ChangeState(PlayerState.WALKING);
            _stateMachine.NotifyEvent(Event.MOVE_TO);
        }
        else if (_stateMachine.State == PlayerState.WALKING ||
                 _stateMachine.State == PlayerState.RUNNING)
        {
            _stateMachine.NotifyEvent(Event.MOVE_TO);
        }
    }


    public override void Exit() { }
    public override void Update()
    {

    }
}