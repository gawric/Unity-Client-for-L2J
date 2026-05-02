using System;
using UnityEngine;

public class CreateIntentionPlayer 
{
    public static IntentionBase GetIntention(Intention currentState, PlayerStateMachine _stateMachine)
    {
        
        switch (currentState)
        {
            case Intention.INTENTION_IDLE:
                return new NewIdleIntention(_stateMachine);
            case Intention.INTENTION_MOVE_TO:
                return new NewMoveToIntention(_stateMachine);
            case Intention.INTENTION_MOVE_TO_PAWN:
                return new NewMoveToPawnIntention(_stateMachine);
            case Intention.INTENTION_STOP_MOVE:
                return new NewStopMoveIntention(_stateMachine);
            case Intention.INTENTION_ATTACK:
                return new NewAttackIntention(_stateMachine);
            case Intention.INTENTION_DEAD:
                return new NewDeadIntention(_stateMachine);
            case Intention.INTENTION_MAGIC_ATTACK:
                return new NewMagicAttackIntention(_stateMachine);
            case Intention.INTENTION_PHYSICAL_SKILLS_ATTACK:
                return new NewPhysicalSkillsIntention(_stateMachine);
            default:
                Debug.LogError("CreateIntentionPlayer> Invalid Intention Request " + currentState);
                throw new ArgumentException("Invalid Intention");
        }
    }

    //new 1 Intention.INTENTION_IDLE => new NewIdleIntention(this),
    //new 1 Intention.INTENTION_MOVE_TO => new NewMoveToIntention(this),
    //new 1 Intention.INTENTION_MOVE_TO_PAWN => new NewMoveToPawnIntention(this),
    //new 1 Intention.INTENTION_STOP_MOVE => new NewStopMoveIntention(this),
    //new 1 Intention.INTENTION_ATTACK => new NewAttackIntention(this),
    //new 1 Intention.INTENTION_DEAD => new NewDeadIntention(this),

}
