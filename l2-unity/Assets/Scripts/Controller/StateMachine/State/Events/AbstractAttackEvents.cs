using UnityEngine;

public abstract class AbstractAttackEvents : StateBase
{
    private const int WOODEN_ARROW = 17;
    protected AnimationEventsBase _events;
    private Animation[] _specialsBows;
    public AbstractAttackEvents(int objectId , Animation[] specialsBows, PlayerStateMachine stateMachine = null ) : base(stateMachine)
    {
        _specialsBows = specialsBows;
        _events = AnimationManager.Instance.GetAnimationEvents(objectId);
        _events.OnAnimationFinished += CallBackAnimationFinish;
        _events.OnAnimationStartShoot += CallBackStartShoot;
        _events.OnAnimationFinishedHit += CallBackFinishedHit;
        _events.OnAnimationStartLoadArrow += CallBackLoadArrow;
        _events.OnAnimationStartHit += CallBackStartHit;

        ProjectileManager.Instance.OnHitMonster += OnHitBodyMonster;
        ProjectileManager.Instance.OnHitCollider += OnHitColliderMonster;
        SwordCollisionService.Instance.OnHitCollider += OnHitColliderMonster;
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
            Transform target = PlayerEntity.Instance.Target;
            SwordCollisionService.Instance.RegisterSword(swordBase, swordTip, target, 0);
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
                
                HitManager.Instance.HandleHitCollider(attacker, monster.GetStateMachine(), hitPointCollider, hitDirection);
            }

            IfMonsterDead(PlayerEntity.Instance.GetTargetEntity());
        }

    }
}
