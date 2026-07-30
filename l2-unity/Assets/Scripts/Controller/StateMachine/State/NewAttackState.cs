
using UnityEngine;



public class NewAttackState : AbstractAttackEvents
{

  
    public NewAttackState(PlayerStateMachine stateMachine) :
        base(stateMachine.GetObjectId() , 
        SpecialAnimationNames.GetSpecialsAttackAnimations() , 
        stateMachine)
    {

    }


    public override void Update()
    {

    }

    public override void HandleEvent(Event evt , object payload = null)
    {
        switch (evt)
        {
            case Event.READY_TO_ACT:
                Debug.Log("Attack Sate to Intention> начало новой atk пришел запрос от сервера");
                Entity targetEntity = _stateMachine.Player.GetTargetEntity();
                if (targetEntity == null || targetEntity.IsDead())
                {
                    // Stale Attack after Die/despawn — do not start another jatk.
                    if (PlayerEntity.Instance != null)
                    {
                        PlayerEntity.Instance.IsAttack = false;
                    }
                    PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                    PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                    break;
                }

                AttackTimingHelper.RotateFaceToMonster(_stateMachine.Player);

                int targetEntityId = targetEntity.IdentityInterlude != null ? targetEntity.IdentityInterlude.Id : 0;
                float attackDurationMs = AttackTimingHelper.ResolveServerLikeAttackDurationMs(_stateMachine.Player);
                float hitFraction = AttackTimingHelper.ResolveHitFractionByWeapon(_stateMachine.Player);
                float pAtkSpd = _stateMachine.Player.Stats != null ? _stateMachine.Player.Stats.BasePAtkSpeed : 0f;

                Debug.Log(
                    $"[ATK_TIMING_CMP] BeginAttack packet→serverCycle " +
                    $"pAtkSpd={pAtkSpd:F1} serverTimeAtkMs={attackDurationMs:F1} " +
                    $"(L2J 500000/pAtkSpd) serverHitMs={attackDurationMs * hitFraction:F1} " +
                    $"hitFraction={hitFraction:F2} targetId={targetEntityId}");

                SwordCollisionService.Instance.BeginAttack(
                    _stateMachine.Player.IdentityInterlude.Id,
                    targetEntityId,
                    _stateMachine.Player.transform,
                    targetEntity.transform,
                    attackDurationMs,
                    hitFraction);

                // Melee Hit/SoulShot: AttackShot on jatk*_1HS/_2HS/_dual/_pole (not wall-clock).
                // RegisterSwordCollision(_stateMachine.Player);

                PlayerEntity.Instance.RefreshRandomPAttack();
                Animation random = PlayerEntity.Instance.RandomName;
                AnimationManager.Instance.PlayAnimationTrigger(_stateMachine.GetObjectId() , random.ToString());

                break;
            case Event.WAIT_RETURN:
                // WhoDied while still ATTACKING: clear latch only. Pose returns at swing end
                // (PlayerStateJAtk.SwitchToIdle). Forcing atkwait here cuts the finishing blow.
                if (PlayerEntity.Instance != null)
                {
                    PlayerEntity.Instance.IsAttack = false;
                    PlayerEntity.Instance.LastAtkAnimation = null;
                }
                break;
            case Event.CANCEL:
                Debug.Log("Attack Sate to Intention> Отмена скорее всего запрос пришел из ActionFaild");
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                PlayerEntity.Instance.LastAtkAnimation = null;
                break;

        }
    }
}
