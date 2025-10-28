public class RunningNpcState : NpcBase
{
    private NpcEntity npcEntity;
    public RunningNpcState(NpcStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {
        npcEntity = (NpcEntity)_stateMachine.Entity;
        npcEntity.StartRunAnim(true);
    }
    public override void Exit()
    {
    }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.ARRIVED:
                //Debug.Log("NPC  Arrived");
                //if (!_stateMachine.MoveMonster.IsFollow())
                //{
                //if (!_stateMachine.MoveMonster.IsSpecialDetectedIsMoveObject())
                //{
                npcEntity = (NpcEntity)_stateMachine.Entity;
                npcEntity.OnStopL2jMoving();
                //_stateMachine.MoveMonster.SetAccessTeleport(true);
                // }

                //}

                break;
            case Event.CANCEL:
               // Debug.Log(" NPC CANCEL RUNNING");
                //monsterEntity = (MonsterEntity)_stateMachine.Entity;
                //if (!_stateMachine.MoveMonster.IsFollow())
                //{
                    //if (!_stateMachine.MoveMonster.IsSpecialDetectedIsMoveObject())
                    //{
                     //   monsterEntity.OnStopL2jMoving();
                      //  _stateMachine.MoveMonster.CancelMove();
                      //  _stateMachine.MoveMonster.SetAccessTeleport(true);
                    //}

               // }

                break;

        }
    }
}
