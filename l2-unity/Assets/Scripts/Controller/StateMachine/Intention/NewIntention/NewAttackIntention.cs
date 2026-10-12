using UnityEngine;

public class NewAttackIntention : IntentionBase
{
    public NewAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter(object arg0)
    {
        L2PawnRange.ClearIgnoredPawn(PlayerEntity.Instance);
        if (!IsSuccessAttack(arg0))
        {
            _stateMachine.ChangeIntention(Intention.INTENTION_IDLE); 
            return;
        }
        


        if (arg0.GetType() == typeof(AttackDto))
        {
            Debug.Log("NewAttackIntention > State " + PlayerStateMachine.Instance.State);
            AttackDto myModel = (AttackDto)arg0;
            int targetId = myModel.TargetId;

            Entity targetEntity = IncomingPacketActions.GameWorld.GetEntityNoLockSync(targetId);

            if (targetEntity != null)
            {
                SelectPacketTarget(targetEntity);
                ReplayAttackTrace.Write(
                    "TARGET_SELECTED",
                    PlayerEntity.Instance,
                    targetEntity,
                    "playerTarget=" +
                    (PlayerEntity.Instance != null && PlayerEntity.Instance.Target != null));

                // Bow: live follow until OnAnimationShoot. Melee: one-shot snapshot only.
                if (CombatFacingService.IsPlayerUsingBow(PlayerEntity.Instance))
                {
                    int localId = PlayerEntity.Instance.Identity.Id;
                    CombatFacingService.Ensure().BeginFollow(
                        localId,
                        IncomingPacketActions.Player.transform,
                        targetEntity.transform);
                }
                else
                {
                    CombatFacingService.Instance?.EndFollowLocal("melee-snapshot");
                    IncomingPacketActions.Player.RotateToAttacker(targetEntity.transform.position);
                }
            }

            Hit playerHit = myModel.FirstHit;
            if (targetEntity == null || playerHit == null)
            {
                _stateMachine.ChangeIntention(Intention.INTENTION_IDLE);
                return;
            }

            // Attack carries visual damage only. The official client changes health from
            // StatusUpdate and enters death only from Die; it doesn't predict death here.
            PlayerEntity.Instance.IsAttack = true;
            if (IncomingPacketActions.Targets != null)
                IncomingPacketActions.Targets.SetAttackTarget();
            PlayerEntity.Instance.SetSelfHit(playerHit);
            // AttackDto packet SS flag — recharge every swing (otherwise only first Hit shows SoulShot).
            PlayerEntity.Instance.IsSoulshotCharged = playerHit.hasSoulshot();
            Debug.Log(
                $"[ATK_HIT_CHAIN] 0.AttackPacket ss={playerHit.hasSoulshot()} miss={playerHit.isMiss()} " +
                $"crit={playerHit.isCrit()} dmg={playerHit.Damage}");

            if (_stateMachine.State != PlayerState.ATTACKING)
            {
                _stateMachine.ChangeState(PlayerState.ATTACKING);
            }
            ReplayAttackTrace.Write(
                "READY_TO_ACT",
                PlayerEntity.Instance,
                targetEntity,
                "damage=" + playerHit.Damage);
            _stateMachine.NotifyEvent(Event.READY_TO_ACT, targetEntity);

        }
    }

    static void SelectPacketTarget(Entity targetEntity)
    {
        if (IncomingPacketActions.Targets != null)
        {
            IncomingPacketActions.Targets.SetTarget(new ObjectData(targetEntity.gameObject), "#ffffff");
            return;
        }

        if (PlayerEntity.Instance == null)
            return;
        PlayerEntity.Instance.TargetId = targetEntity.Identity != null ? targetEntity.Identity.Id : 0;
        PlayerEntity.Instance.Target = targetEntity.transform;
    }





    public override void Exit() { }
    public override void Update()
    {

    }
}