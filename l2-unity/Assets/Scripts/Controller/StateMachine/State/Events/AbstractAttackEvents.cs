using UnityEngine;

public abstract class AbstractAttackEvents : StateBase
{
    private const int WOODEN_ARROW = 17;
    private const string PROJECTILE_CHAIN_LOG = "[PROJECTILE_CHAIN]";
    protected AnimationEventsBase _events;
    private Animation[] _specialsBows;
    private bool _isSubscribed;
  
    public AbstractAttackEvents(int objectId , Animation[] specialsBows, PlayerStateMachine stateMachine = null ) : base(stateMachine)
    {
        _specialsBows = specialsBows;
        _events = AnimationManager.Instance.GetAnimationEvents(objectId);
    }

    public override void Enter()
    {
        base.Enter();
        if (_isSubscribed) return;
        _isSubscribed = true;

        if (_events != null)
        {
            _events.OnAnimationFinished += CallBackAnimationFinish;
            _events.OnAnimationStartShoot += CallBackStartShoot;
            _events.OnAnimationFinishedHit += CallBackFinishedHit;
            _events.OnAnimationStartLoadArrow += CallBackLoadArrow;
            _events.OnAnimationStartHit += CallBackStartHit;
        }

        if (ProjectileManager.Instance != null)
        {
            SkillExecutor.Instance.OnAllAnimationFinished += OnAllAnimationFinishedFromExecutor;
        }


        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.OnHitMonster += OnHitBodyMonster;
            ProjectileManager.Instance.OnHitCollider += OnHitColliderMonster;
            ProjectileManager.Instance.OnHitEffectProjectile += OnHitEffectProjectile;
        }
        if (SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.OnHitCollider += OnHitColliderMonster;
        }


    }

    public override void Exit()
    {
        base.Exit();
        if (!_isSubscribed) return;
        _isSubscribed = false;

        if (_events != null)
        {
            _events.OnAnimationFinished -= CallBackAnimationFinish;
            _events.OnAnimationStartShoot -= CallBackStartShoot;
            _events.OnAnimationFinishedHit -= CallBackFinishedHit;
            _events.OnAnimationStartLoadArrow -= CallBackLoadArrow;
            _events.OnAnimationStartHit -= CallBackStartHit;
        }

        SkillExecutor.Instance.OnAllAnimationFinished -= OnAllAnimationFinishedFromExecutor;

        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.OnHitMonster -= OnHitBodyMonster;
            ProjectileManager.Instance.OnHitCollider -= OnHitColliderMonster;
            ProjectileManager.Instance.OnHitEffectProjectile -= OnHitEffectProjectile;
        }
        if (SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.OnHitCollider -= OnHitColliderMonster;
        }


    
    }


    private void OnAllAnimationFinishedFromExecutor(AnimationEventsBase actions)
    {
        if (actions == null || _events == null || actions != _events)
        {
            return;
        }

        if (_stateMachine != null && _stateMachine.Player != null)
        {
            int objectId = _stateMachine.Player.IdentityInterlude.Id;
            AnimationManager.Instance.ResetPlayerAnimatorSpeed(objectId);
        }

        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }


    private void CallBackAnimationFinish(string animName)
    {
        foreach (Animation special in _specialsBows)
        {
            
            if (animName == special.ToString())
            {
                if(special.Type == TypesAnimation.MagicAttack && special.Phase != MagicPhase.End)
                {
                    return;
                }

                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                break;
            }
        }
    }

    private void CallBackFinishedHit(string animName)
    {
        foreach (Animation special in _specialsBows)
        {
            if (animName == special.ToString())
            {

                if (special.Type == TypesAnimation.MeleeAttack)
                {
                    Transform[] swordBasePoints = _stateMachine.Player.GetSwordBasePoints();
                    if (swordBasePoints.Length > 1) SwordCollisionService.Instance.UnregisterSword(swordBasePoints[0]);
                    PlayerEntity.Instance.RemoveProceduralPose();
                    break;
                }
            }
        }
    }

    private void IfMonsterDead(Entity target)
    {
        if (target == null) return;


        if (target != null & target is MonsterEntity)
        {
            MonsterEntity monsterEntity = (MonsterEntity)target;
            //Debug.Log("Попали и увидели что монстр уже должен быть мертвым hp  " + monsterEntity.Hp() + " RemainingHP " + monsterEntity.CalculateRemainingHp());

            if (monsterEntity.IsDead() || monsterEntity.CalculateRemainingHp() <= 0)
            {
                monsterEntity.SetDead(true);
                MonsterStateMachine stateMachine = monsterEntity.GetStateMachine();
                stateMachine.ChangeState(MonsterState.DEAD);
                stateMachine.NotifyEvent(Event.FORCE_DEATH);
                //Debug.Log("Попали и увидели что монстр уже должен быть мертвым hp запускаем анимацию смерти " + monsterEntity.IsDead());
            }


        }
    }

    private void CallBackStartShoot(string animName)
    {
        foreach (Animation special in _specialsBows)
        {
            if (animName == special.ToString())
            {

                if (special.Type == TypesAnimation.BowAttack)
                {
                    GameObject go = PlayerEntity.Instance.GetGoEtcItem();
                    Transform target = PlayerEntity.Instance.Target;

                    if (PlayerEntity.Instance == null ||
                        go == null ||
                        target == null)
                    {
                        Debug.LogError("NewAttackState->CallBackStartShoot: Критическая ошибка не все компоненты загрузились что-бы отправить стрелу в полет");
                        return;
                    }

                    Vector3 startPos = PlayerEntity.Instance.GetPositionRightHand();

                    float baseAttackTime = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);
                    float targetDistance = PlayerEntity.Instance.TargetDistance();
                    float[] timeAndFlye = CalcBaseParam.CalculateAttackAndFlightTimes(targetDistance, baseAttackTime);
                    var timeAtk = TimeUtils.ConvertMsToSec(timeAndFlye[1]);

                    ProjectileData settings = new ProjectileData(go, target, startPos, target);
                    settings.lifetime = timeAtk;

                    ProjectileManager.Instance.LaunchProjectile(go, startPos, target, settings);
                    break;
                }

            }
        }
    }

    private void CallBackLoadArrow(string animName)
    {
        PlayerEntity.Instance.EquipArrow(WOODEN_ARROW);
    }

    private void CallBackStartHit(string animName)
    {
        //Animation[] specials = SpecialAnimationNames.GetSpecialsAttackAnimations();
        foreach (Animation special in _specialsBows)
        {
            if (special.Type == TypesAnimation.MeleeAttack)
            {
                RegisterSwordCollision(_stateMachine.Player);
            }
        }

    }

    protected void RegisterSwordCollision(PlayerEntity entity)
    {
        if (entity == null) return;

        Transform[] swordBasePoints = entity.GetSwordBasePoints();

        if (swordBasePoints != null && swordBasePoints.Length > 1)
        {
            Transform swordBase = swordBasePoints[0];
            Transform swordTip = swordBasePoints[1];
            Entity targetEntity = entity.GetTargetEntity();
            Transform target = targetEntity != null ? targetEntity.transform : PlayerEntity.Instance.Target;
            int attackerEntityId = entity.IdentityInterlude != null ? entity.IdentityInterlude.Id : 0;
            int targetEntityId = targetEntity != null && targetEntity.IdentityInterlude != null ? targetEntity.IdentityInterlude.Id : 0;
            SwordCollisionService.Instance.RegisterSwordByEntityId(attackerEntityId, targetEntityId, swordBase, swordTip, target, 0);
        }
    }

    private void OnHitBodyMonster(GameObject prefab, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        HitManager.Instance.HandleHitBody(prefab, target, hitPointCollider, hitDirection);
    }

    private void OnHitColliderMonster(Transform attacker, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        Entity entity = PlayerEntity.Instance.GetTargetEntity();
       
        if (entity is MonsterEntity)
        {
            MonsterEntity monster = (MonsterEntity)entity;
         
            if (!_stateMachine.Player.HitIsMissed())
            {
                
                HitManager.Instance.HandleHitCollider(PlayerEntity.Instance , attacker, monster.GetStateMachine(), hitPointCollider, hitDirection);
            }

            IfMonsterDead(PlayerEntity.Instance.GetTargetEntity());
        }

    }

    private void OnHitEffectProjectile(GameObject prefab, Transform target, Vector3 hitPointCollider, Vector3 hitDirection, int attackerEntityId)
    {
        string prefabName = prefab != null ? prefab.name : "null";
        string targetName = target != null ? target.name : "null";
        Debug.Log($"{PROJECTILE_CHAIN_LOG} OnHitEffectProjectile prefab={prefabName} target={targetName} attackerId={attackerEntityId} point={hitPointCollider} dir={hitDirection}");
    }
}
