using UnityEngine;

public class MoveToNpcIntention : NpcIntentionBase
{
    public MoveToNpcIntention(NpcStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(CharMoveToLocation))
        {
                CharMoveToLocation packet = (CharMoveToLocation)arg0;
                NpcEntity npcEntity = (NpcEntity)_stateMachine.Entity;

                npcEntity.OnStartL2jMoving((npcEntity.Running) ? false : true);
          
                int id = _stateMachine.Entity.IdentityInterlude.Id;
                DebugLineDraw.ShowDrawLineDebugNpc(PlayerEntity.Instance.IdentityInterlude.Id, packet.OldPosition, packet.NewPosition, Color.red);

                //Debug.Log("object position 3 " + npcEntity.transform.position + " go name " + npcEntity.name);
                MovementTarget movementTarget = new MovementTarget(packet.NewPosition, 0.1f);
                MoveAllCharacters.Instance.AddMoveData(id, new MovementData(npcEntity, movementTarget));

            if (npcEntity.Running)
            {
                _stateMachine.ChangeState(NpcState.RUNNING);
            }
            else
            {
                _stateMachine.ChangeState(NpcState.WALKING);
            }
           // }
        }
        //Debug.Log("Debug EVENT =========== MoveToMonsterIntention");
    }

    public override void Exit()
    {
        //Debug.Log("Debug EVENT =========== MoveToMonsterIntention  EXIT");
    }
    public override void Update()
    {
        //Debug.Log("Debug EVENT MoveToMonsterIntention ATTACK");
    }

}
