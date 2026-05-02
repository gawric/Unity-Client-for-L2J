
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
               
                SwordCollisionService.Instance.BeginAttack(
                    _stateMachine.Player.IdentityInterlude.Id,
                    targetEntityId,
                    _stateMachine.Player.transform,
                    targetEntity != null ? targetEntity.transform : _stateMachine.Player.Target,
                    attackDurationMs,
                    hitFraction);

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