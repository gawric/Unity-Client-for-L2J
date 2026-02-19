using System;
using UnityEngine;

public class CreatorStatePlayer 
{
    public static StateBase GetState(PlayerState currentState, PlayerStateMachine _stateMachine)
    {
        switch (currentState)
        {
            case PlayerState.IDLE:
                return new NewIdleState(_stateMachine);
            case PlayerState.RUNNING:
                return new NewRunningState(_stateMachine);
            case PlayerState.ATTACKING:
                return new NewAttackState(_stateMachine);
            case PlayerState.PHYSICAL_SKILLS:
                return new NewPhysicalSkillsState(_stateMachine);
            case PlayerState.MAGIC_SKILLS:
                return new NewMagicSkillsState(_stateMachine);
            case PlayerState.DEAD:
                return new NewDeadState(_stateMachine);
            case PlayerState.REBIRTH:
                return new NewRebirthState(_stateMachine);
            case PlayerState.WALKING:
                return new NewWalkingState(_stateMachine);
            default:
                throw new ArgumentException("Invalid State");
        }
    }

    //new1 PlayerState.IDLE => new NewIdleState(this),
    //new1 PlayerState.RUNNING => new NewRunningState(this),
    //new1 PlayerState.ATTACKING => new NewAttackState(this),
    //new1 PlayerState.DEAD => new NewDeadState(this),
    //new1 PlayerState.REBIRTH => new NewRebirthState(this),
    //PlayerState.WALKING => new NewWalkingState(this),
}
