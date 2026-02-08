using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class DeadMosterState : MonsterBase
{
    public DeadMosterState(MonsterStateMachine stateMachine) : base(stateMachine) {

    }
    public override void Enter() {

    }
    public override void Exit() { }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.DEAD:
                //Protects against early death before you can strike, but the mob is already dead, or vice versa.If this happens, a FORCE_DEAD event occurs when the sword hits the monster.
                if (PlayerEntity.Instance.IsAttack || PlayerStateMachine.Instance.State == PlayerState.ATTACKING) return;
                Debug.Log("Попали и увидели что монстр уже должен быть мертвым и мы не в состоянии атаки! " + PlayerEntity.Instance.IsAttack  + " PlayerStateMachine " + PlayerStateMachine.Instance.State);
                UseDead((MonsterEntity)_stateMachine.Entity);
                break;

            case Event.FORCE_DEATH:
                UseDead((MonsterEntity)_stateMachine.Entity);
                break;
        }
    }

    private void UseDead(MonsterEntity entity)
    {
        Debug.Log("Попали и увидели что монстр уже должен быть мертвым пришел пакет на помереть 1");
        AnimationManager.Instance.PlayMonsterAnimation(entity.IdentityInterlude.Id, AnimationNames.DEAD.ToString());
        ResetAttackIfMonsterDead();
    }

    private void ResetAttackIfMonsterDead()
    {
        Transform[] swordBasePoints = PlayerEntity.Instance.GetSwordBasePoints();
        if (swordBasePoints.Length > 1) SwordCollisionService.Instance.UnregisterSword(swordBasePoints[0]);
        PlayerEntity.Instance.RemoveProceduralPose();
        Debug.LogWarning("SwordCollisionManager: reset dead monster");
    }

    private void CallBackAnimationFinish(string animName)
    {
        MonsterEntity entity = (MonsterEntity)_stateMachine.Entity;
        NetworkAnimationController nac = entity.GetAnimatorController();
        AnimationManager.Instance.PlayMonsterAnimation(_stateMachine.GetObjectId(), AnimationNames.DEAD.ToString());

    }
}
