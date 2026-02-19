using System;
using UnityEngine;

public class CreatorIntentionNpc
{
    public static NpcIntentionBase GetIntention(NpcIntention currentIntention, NpcStateMachine _stateMachine)
    {
        switch (currentIntention)
        {
            case NpcIntention.INTENTION_IDLE:
                return new IdleNpcIntention(_stateMachine);
            case NpcIntention.INTENTION_MOVE_TO:
                return new MoveToNpcIntention(_stateMachine);
            case NpcIntention.STARTED_TALKING:
                return new StartTalkingIntention(_stateMachine);

            default:
                throw new ArgumentException("Invalid Intention");
        }
    }
}