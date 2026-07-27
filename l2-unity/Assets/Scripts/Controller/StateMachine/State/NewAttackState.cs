
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
                AttackTimingHelper.RotateFaceToMonster(_stateMachine.Player);
                Entity targetEntity = _stateMachine.Player.GetTargetEntity();

                int targetEntityId = targetEntity != null && targetEntity.IdentityInterlude != null ? targetEntity.IdentityInterlude.Id : 0;
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
                    targetEntity != null ? targetEntity.transform : _stateMachine.Player.Target,
                    attackDurationMs,
                    hitFraction);

                // Melee Hit/SoulShot: AttackShot on jatk*_1HS/_2HS/_dual/_pole (not wall-clock).
                // RegisterSwordCollision(_stateMachine.Player);

                PlayerEntity.Instance.RefreshRandomPAttack();
                Animation random = PlayerEntity.Instance.RandomName;
                AnimationManager.Instance.PlayAnimationTrigger(_stateMachine.GetObjectId() , random.ToString());

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