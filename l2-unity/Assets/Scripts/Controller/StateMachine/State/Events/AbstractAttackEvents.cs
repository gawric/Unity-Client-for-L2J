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
            // Melee Hit/SoulShot: AttackShot anim event (not wall-clock).
            // Bow Soulshot/stick: HitManager subscribes to ProjectileManager.OnHitMonster (not Attack lifecycle).
            _events.OnAnimationAttackShot += CallBackAttackShot;
        }

        if (ProjectileManager.Instance != null)
        {
            SkillExecutor.Instance.OnAllAnimationFinished += OnAllAnimationFinishedFromExecutor;
            // EffectOnly magic chain only — bow ArrowStick is HitManager ← OnHitMonster (persistent).
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
            _events.OnAnimationAttackShot -= CallBackAttackShot;
        }

        SkillExecutor.Instance.OnAllAnimationFinished -= OnAllAnimationFinishedFromExecutor;

        if (ProjectileManager.Instance != null)
        {
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
            if (animName != special.ToString())
            {
                continue;
            }

            if (special.Type == TypesAnimation.MagicAttack && special.Phase != MagicPhase.End)
            {
                return;
            }

            // Melee jatk / SpAtk return is owned by SMB SwitchToIdle (not Complete).
            // A second WAIT_RETURN CrossFades mid-transition and can freeze the Animator.
            if (special.Type == TypesAnimation.MeleeAttack ||
                (!string.IsNullOrEmpty(animName) &&
                 animName.StartsWith("SpAtk", System.StringComparison.Ordinal)))
            {
                return;
            }

            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
            PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
            break;
        }
    }

    private void CallBackFinishedHit(string animName)
    {
        if (!IsMeleeAttackShotAnim(animName))
        {
            return;
        }

        Transform[] swordBasePoints = _stateMachine.Player.GetSwordBasePoints();
        if (swordBasePoints != null && swordBasePoints.Length > 1)
        {
            SwordCollisionService.Instance.UnregisterSword(swordBasePoints[0]);
        }

        PlayerEntity.Instance.RemoveProceduralPose();
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
                    // SpAtk01_bow / jatk*_bow: same ArrowStick path. Ensure wooden arrow exists
                    // if LoadArrow notify was skipped (state exited early before equip).
                    if (PlayerEntity.Instance.GetGoEtcItem() == null)
                    {
                        PlayerEntity.Instance.EquipArrow(WOODEN_ARROW);
                    }

                    GameObject go = PlayerEntity.Instance.GetGoEtcItem();
                    Transform target = PlayerEntity.Instance.Target;

                    if (PlayerEntity.Instance == null ||
                        go == null ||
                        target == null)
                    {
                        Debug.LogError(
                            $"NewAttackState->CallBackStartShoot: missing arrow/target anim={animName} " +
                            $"go={(go != null)} target={(target != null)}");
                        return;
                    }

                    Vector3 startPos = PlayerEntity.Instance.GetPositionRightHand();
                    Vector3 aimPos = VectorUtils.GetCollision(startPos, target);
                    float dist3d = Vector3.Distance(startPos, aimPos);
                    // ANProjectile NArrow / s_u003_d: dirMul=3000, fly=sqrt(2·Dist/3000), path (t/T)².
                    float flyAccel = ProjectileFlightTimeCalculator.CalculateL2ArrowFlightTimeSeconds(dist3d);
                    float avgSpeed = dist3d / Mathf.Max(flyAccel, 0.05f);

                    ProjectileData settings = new ProjectileData(go, target, startPos, target);
                    settings.impactType = ProjectileImpactType.ArrowStick;
                    settings.speed = avgSpeed;
                    settings.flytime = flyAccel;
                    settings.lifetime = flyAccel;

                    bool isSpAtkBow = animName.IndexOf("SpAtk", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    Debug.Log(
                        $"[BOW_ARROW] SHOOT anim={animName} spAtkBow={isSpAtkBow} " +
                        $"dist3d={dist3d:F3} flyAccel={flyAccel:F3}s avgSpeed={avgSpeed:F3} " +
                        $"uuAccel=3000 path=(t/T)^2 (compare vs decompile ANSkillProjectileTick.log)");

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

    /// <summary>
    /// L2-like AttackShot: fire melee Hit/SoulShot when clip notify fires.
    /// Accepts jatk* and SpAtk* melee (_1HS / _2HS / _dual / _pole).
    /// Bow: StartShoot + ArrowStickTimeHit (end of flytime / Hit Time), not this path.
    /// </summary>
    private void CallBackAttackShot(string animName)
    {
        if (!IsMeleeAttackShotAnim(animName))
        {
            Debug.Log(
                $"[HIT_FX] 2.CallBackAttackShot SKIP not-melee-attack-shot frame={Time.frameCount} anim={animName}");
            return;
        }

        PlayerEntity player = _stateMachine != null ? _stateMachine.Player : null;
        if (player == null || SwordCollisionService.Instance == null)
        {
            Debug.LogWarning(
                $"[HIT_FX] 2.CallBackAttackShot SKIP playerNull={player == null} " +
                $"swordSvcNull={SwordCollisionService.Instance == null} anim={animName}");
            return;
        }

        Transform[] swordBasePoints = player.GetSwordBasePoints();
        if (swordBasePoints == null || swordBasePoints.Length <= 1)
        {
            Debug.LogWarning($"[HIT_FX] 2.CallBackAttackShot SKIP no sword points anim={animName}");
            return;
        }

        Entity targetEntity = player.GetTargetEntity();
        Transform target = targetEntity != null ? targetEntity.transform : player.Target;
        int attackerEntityId = player.IdentityInterlude != null ? player.IdentityInterlude.Id : 0;
        int targetEntityId = targetEntity != null && targetEntity.IdentityInterlude != null
            ? targetEntity.IdentityInterlude.Id
            : 0;

        if (target == null)
        {
            Debug.LogWarning(
                $"[HIT_FX] 2.CallBackAttackShot SKIP target=null anim={animName} " +
                $"attackerId={attackerEntityId} targetEntityId={targetEntityId}");
            return;
        }

        Debug.Log(
            $"[HIT_FX] 2.CallBackAttackShot OK frame={Time.frameCount} t={Time.time:F3} " +
            $"anim={animName} attackerId={attackerEntityId} targetId={targetEntityId} " +
            $"target={target.name} → EmitHitFromAttackShot");

        if (SwordCollisionService.Instance != null &&
            attackerEntityId > 0 &&
            player.Stats != null)
        {
            float serverCycleMs = AttackTimingHelper.ResolveServerLikeAttackDurationMs(player);
            float serverHitMs = AttackTimingHelper.ResolveServerLikeHitMs(player);
            Debug.Log(
                $"[ATK_TIMING_CMP] AttackShot fire anim={animName} " +
                $"serverTimeAtkMs={serverCycleMs:F1} serverHitMs={serverHitMs:F1} " +
                $"(compare with Enter AttackShotWallMs and Exit wallElapsedMs)");
        }

        SwordCollisionService.Instance.EmitHitFromAttackShot(
            attackerEntityId,
            targetEntityId,
            swordBasePoints[0],
            swordBasePoints[1],
            target);
    }

    /// <summary>jatk01_1HS / SpAtk01_1HS / … — melee AttackShot path, not bow.</summary>
    private static bool IsMeleeAttackShotAnim(string animName)
    {
        if (string.IsNullOrEmpty(animName))
        {
            return false;
        }

        if (animName.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        bool isJatk = animName.IndexOf("jatk", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isSpAtk = animName.IndexOf("SpAtk", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return isJatk || isSpAtk;
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
        else
        {
            Debug.LogWarning($"[ATK_HIT_CHAIN] RegisterSword SKIP — no sword points on player");
        }
    }

    private void OnHitColliderMonster(Transform attacker, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        // Melee only (SwordCollisionService). Bow uses HitManager ← OnHitMonster.
        string attackerName = attacker != null ? attacker.name : "null";
        string targetName = target != null ? target.name : "null";
        Entity entity = PlayerEntity.Instance != null ? PlayerEntity.Instance.GetTargetEntity() : null;

        Debug.Log(
            $"[HIT_FX] 5.OnHitColliderMonster frame={Time.frameCount} t={Time.time:F3} " +
            $"attacker={attackerName} target={targetName} " +
            $"playerTarget={(entity != null ? entity.name : "null")} " +
            $"isMonster={entity is MonsterEntity} point={hitPointCollider}");

        if (entity is MonsterEntity)
        {
            MonsterEntity monster = (MonsterEntity)entity;
            bool missed = _stateMachine != null &&
                          _stateMachine.Player != null &&
                          _stateMachine.Player.HitIsMissed();

            if (missed)
            {
                Debug.Log(
                    $"[HIT_FX] 5.OnHitColliderMonster SKIP HitIsMissed=true " +
                    $"monster={monster.name} — EffectManager NOT called");
            }
            else if (HitManager.Instance == null)
            {
                Debug.LogWarning("[HIT_FX] 5.OnHitColliderMonster SKIP HitManager.Instance=null");
            }
            else
            {
                Debug.Log(
                    $"[HIT_FX] 5.OnHitColliderMonster → HitManager.HandleHitCollider " +
                    $"monster={monster.name}");
                HitManager.Instance.HandleHitCollider(
                    PlayerEntity.Instance,
                    attacker,
                    monster.GetStateMachine(),
                    hitPointCollider,
                    hitDirection);
            }

            IfMonsterDead(PlayerEntity.Instance.GetTargetEntity());
        }
        else
        {
            Debug.Log(
                $"[HIT_FX] 5.OnHitColliderMonster SKIP not MonsterEntity " +
                $"type={(entity != null ? entity.GetType().Name : "null")}");
        }
    }

    private void OnHitEffectProjectile(GameObject prefab, Transform target, Vector3 hitPointCollider, Vector3 hitDirection, int attackerEntityId)
    {
        string prefabName = prefab != null ? prefab.name : "null";
        string targetName = target != null ? target.name : "null";
        Debug.Log($"{PROJECTILE_CHAIN_LOG} OnHitEffectProjectile prefab={prefabName} target={targetName} attackerId={attackerEntityId} point={hitPointCollider} dir={hitDirection}");
    }
}
