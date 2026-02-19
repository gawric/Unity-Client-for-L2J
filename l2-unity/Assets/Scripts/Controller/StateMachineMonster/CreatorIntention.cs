using System;
using UnityEngine;

public class CreatorIntention 
{
    public static MonsterIntentionBase GetIntention(MonsterIntention currentIntention, MonsterStateMachine _stateMachine)
    {
        switch (currentIntention)
        {
            case MonsterIntention.INTENTION_IDLE:
                return new IdleMonsterIntention(_stateMachine);
            case MonsterIntention.INTENTION_FOLLOW:
                return new FollowMonsterIntention(_stateMachine);
            case MonsterIntention.INTENTION_ATTACK:
                return new AttackMonsterIntention(_stateMachine);
            case MonsterIntention.INTENTION_MOVE_TO:
                return new MoveToMonsterIntention(_stateMachine);
            case MonsterIntention.INTENTION_STOP_MOVE:
                return new StopMoveMonsterIntention(_stateMachine);
            default:
                throw new ArgumentException("Invalid Intention");
        }
    }
}
