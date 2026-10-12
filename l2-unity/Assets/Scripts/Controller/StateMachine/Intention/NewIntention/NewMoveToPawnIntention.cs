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

            // Packet Dist is the weapon-range offset only (sword 40 UU). Collision radii are not in it.
            Debug.Log("NewMoveToPawnIntention > dist " + distance + " stopDist " + model.Distance());

            if (distance > model.Distance())
            {
                // Packet Dist is the stand ring. The pawn capsule must not stop the player outside it.
                L2PawnRange.IgnorePawnCollision(PlayerEntity.Instance, model.TarEntity());
                PlayerStateMachine.Instance.IsMoveToPawn = true;
                bool running = PlayerEntity.Instance != null && PlayerEntity.Instance.Running;
                IncomingPacketActions.Player.MoveToPoint(new MovementTarget(model.TarEntity().transform, model.Distance(), running));
                StartAnimMoveTo();
            }
            else
            {
                L2PawnRange.ClearIgnoredPawn(PlayerEntity.Instance);
            }

        }
    }

    private void StartAnimMoveTo()
    {
        if (_stateMachine.State == PlayerState.IDLE ||
            _stateMachine.State == PlayerState.ATTACKING ||
            _stateMachine.State == PlayerState.PHYSICAL_SKILLS ||
            _stateMachine.State == PlayerState.MAGIC_SKILLS)
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