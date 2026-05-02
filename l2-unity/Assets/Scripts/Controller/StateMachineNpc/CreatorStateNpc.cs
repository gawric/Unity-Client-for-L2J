using System;
using UnityEngine;

public class CreatorStateNpc
{
    public static NpcBase GetState(NpcState monsterState, NpcStateMachine _stateMachine)
    {
        switch (monsterState)
        {
            case NpcState.IDLE:
                return new IdleNpcState(_stateMachine);
            case NpcState.RUNNING:
                return new RunningNpcState(_stateMachine);
            case NpcState.WALKING:
                return new WalkingNpcState(_stateMachine);

            default:
                throw new ArgumentException("Invalid state");
        }
    }
}