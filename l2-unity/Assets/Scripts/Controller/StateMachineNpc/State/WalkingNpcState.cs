using UnityEngine;

public class WalkingNpcState : NpcBase
{
    public WalkingNpcState(NpcStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {

    }
    public override void Exit()
    {

    }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        NpcEntity npcEntity = (NpcEntity)_stateMachine.Entity;

        switch (evt)
        {
            case Event.ARRIVED:
                Debug.Log("Walking NPC ARRIVED TO POINT!");
                npcEntity.OnStopL2jMoving();
                //_stateMachine.MoveMonster.SetAccessTeleport(true);
                break;
            case Event.CANCEL:
                Debug.Log("CANCEL Walking Npc ARRIVED TO POINT!");
                npcEntity.OnStopL2jMoving();
                //_stateMachine.MoveMonster.CancelMove();
                //_stateMachine.MoveMonster.SetAccessTeleport(true);
                break;

        }
    }
}
