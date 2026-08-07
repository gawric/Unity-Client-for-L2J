using UnityEngine;

public class NewAttackIntention : IntentionBase
{
    public NewAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        if (!IsSuccessAttack(arg0))
        {
            _stateMachine.ChangeIntention(Intention.INTENTION_IDLE); 
            return;
        }
        


        if (arg0.GetType() == typeof(Attack))
        {
            Debug.Log("NewAttackIntention > State " + PlayerStateMachine.Instance.State);
            Attack myModel = (Attack)arg0;
            int targetId = myModel.TargetId;

            Entity targetEntity = World.Instance.GetEntityNoLockSync(targetId);

            if (targetEntity != null)
            {
                // Bow: live follow until OnAnimationShoot. Melee: one-shot snapshot only.
                if (CombatFacingService.IsPlayerUsingBow(PlayerEntity.Instance))
                {
                    int localId = PlayerEntity.Instance.IdentityInterlude.Id;
                    CombatFacingService.Ensure().BeginFollow(
                        localId,
                        PlayerController.Instance.transform,
                        targetEntity.transform);
                }
                else
                {
                    CombatFacingService.Instance?.EndFollowLocal("melee-snapshot");
                    PlayerController.Instance.RotateToAttacker(targetEntity.transform.position);
                }
            }

            Hit playerHit = myModel.FirstHit;

            targetEntity.SetDamage(playerHit.Damage);
            PlayerEntity.Instance.IsAttack = true;
            if (TargetManager.Instance != null &&
                TargetManager.Instance.HasTarget() &&
                targetEntity is MonsterEntity)
            {
                TargetManager.Instance.SetAttackTarget();
            }
            PlayerEntity.Instance.SetSelfHit(playerHit);
            // Attack packet SS flag — recharge every swing (otherwise only first Hit shows SoulShot).
            PlayerEntity.Instance.IsSoulshotCharged = playerHit.hasSoulshot();
            Debug.Log(
                $"[ATK_HIT_CHAIN] 0.AttackPacket ss={playerHit.hasSoulshot()} miss={playerHit.isMiss()} " +
                $"crit={playerHit.isCrit()} dmg={playerHit.Damage}");

            if (_stateMachine.State != PlayerState.ATTACKING)
            {
                _stateMachine.ChangeState(PlayerState.ATTACKING);
            }
            _stateMachine.NotifyEvent(Event.READY_TO_ACT);

        }
    }





    public override void Exit() { }
    public override void Update()
    {

    }
}