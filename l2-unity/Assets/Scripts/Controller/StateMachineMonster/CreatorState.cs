using System;
using UnityEngine;

public class CreatorState 
{
    public static MonsterBase GetState(MonsterState monsterState , MonsterStateMachine _stateMachine)
    {
        switch (monsterState)
       {
        case MonsterState.IDLE:
               return  new IdleMosterState(_stateMachine);
        case MonsterState.ATTACKING:
                return  new AttackinMonsterState(_stateMachine);
        case MonsterState.DEAD:
                return  new DeadMosterState(_stateMachine);
        case MonsterState.RUNNING:
                return  new RunningMonsterState(_stateMachine);
        case MonsterState.WALKING:
                return  new WalkingMonsterState(_stateMachine);

        default:
                throw new ArgumentException("Invalid state");
        }
    }
}
