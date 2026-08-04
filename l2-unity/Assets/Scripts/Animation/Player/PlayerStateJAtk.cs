using UnityEngine;

/// <summary>
/// Melee attack anim — linear rate over the FULL L2 attack cycle (timeAtk).
/// Server: timeAtk = calculateTimeBetweenAttacks; Hit at timeAtk/2 (BeginAttack HitFraction ~0.5).
/// Do not stretch the clip into timeAtk/2 — that makes the swing ~2x faster than SoulShot.
/// </summary>
public class PlayerStateJAtk : StateMachineBehaviour
{
    private const string JATK_TIMING_LOG = "[JATK_TIMING]";
    private const string JATK_EARLY_LOG = "[JATK_EARLY]";

    private float _startTime = -1f;
    private float _endTime = -1f;
    private float _linearPatkSpd = 1f;
    private float _lastAnimNormalized = 0f;
    private float _fullCycleMs = 0f;
    private float _clipLengthSec = 0f;
    private int _milestoneMask = 0;
    private int _attackEpoch = 0;
    private bool _isSwordHitLogged = false;
    private bool _isColliderSubscribed = false;
    private bool _isSwitchIdle = false;
    private bool _dieTargetLatched = false;
    private bool _exitViaSwitchToIdle = false;
    private string _lastSwitchDenyReason = "";

    public string parameterName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(layerIndex);
        if (clipInfos == null || clipInfos.Length == 0)
        {
            clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);
        }

        _isSwitchIdle = false;
        _exitViaSwitchToIdle = false;
        _dieTargetLatched = false;
        _lastSwitchDenyReason = "";
        _isSwordHitLogged = false;
        _milestoneMask = 0;
        _lastAnimNormalized = 0f;

        _fullCycleMs = GetFullAttackCycleMs(parameterName);
        _startTime = Time.time;
        _endTime = TimeUtils.ConvertMsToSec(_fullCycleMs);

        float timeAnimation = (clipInfos != null && clipInfos.Length > 0 && clipInfos[0].clip != null)
            ? clipInfos[0].clip.length
            : 1f;
        _clipLengthSec = timeAnimation;

        // Linear rate: play whole clip over full server attack cycle (not timeAtk/2).
        _linearPatkSpd = timeAnimation * 1000f / Mathf.Max(1f, _fullCycleMs);
        PlayerAnimationController.Instance.SetPAtkSpeed(_linearPatkSpd);

        TrySubscribeSwordCollider();

        int entityId = ResolvePlayerEntityId();
        if (entityId > 0 && SwordCollisionService.Instance != null)
        {
            _attackEpoch = SwordCollisionService.Instance.GetAttackEpoch(entityId);
            SwordCollisionService.Instance.UpdateAttackProgress(entityId, 0f);
        }
        else
        {
            _attackEpoch = 0;
        }

        float hitFraction = PlayerEntity.Instance != null
            ? AttackTimingHelper.ResolveHitFractionByWeapon(PlayerEntity.Instance)
            : 0.5f;
        float hitAtMs = _fullCycleMs * hitFraction;

        float attackShotSec = -1f;
        float onAnimHitSec = -1f;
        string clipName = "null";
        if (clipInfos != null && clipInfos.Length > 0 && clipInfos[0].clip != null)
        {
            AnimationClip clip = clipInfos[0].clip;
            clipName = clip.name;
            attackShotSec = AnimationDataCache.GetEventTimeByName(animator, clip, "AttackShot");
            onAnimHitSec = AnimationDataCache.GetEventTimeByName(animator, clip, "OnAnimationHit");
        }

        float clipLenMs = timeAnimation * 1000f;
        float attackShotNorm = timeAnimation > 0f && attackShotSec >= 0f ? attackShotSec / timeAnimation : -1f;
        float attackShotWallMs = attackShotNorm >= 0f ? _fullCycleMs * attackShotNorm : -1f;
        float onAnimHitNorm = timeAnimation > 0f && onAnimHitSec >= 0f ? onAnimHitSec / timeAnimation : -1f;

        float pAtkSpd = PlayerEntity.Instance != null && PlayerEntity.Instance.Stats != null
            ? PlayerEntity.Instance.Stats.BasePAtkSpeed
            : 0f;

        // Client wall duration of the swing = server timeAtk (clip is rate-scaled to fit).
        // If clipLenMs >> serverTimeAtkMs, anim feels long only if rate is wrong; compare these.
        Debug.Log(
            $"[ATK_TIMING_CMP] Enter anim={parameterName} clip={clipName} " +
            $"pAtkSpd={pAtkSpd:F1} serverTimeAtkMs={_fullCycleMs:F1} (500000/pAtkSpd) " +
            $"serverHitMs={hitAtMs:F1} (timeAtk*{hitFraction:F2}) " +
            $"clipLenMs={clipLenMs:F1} clipLenSec={timeAnimation:F3} " +
            $"patkspd={_linearPatkSpd:F3} (=clipMs/serverMs) " +
            $"clientWallCycleMs={_fullCycleMs:F1} " +
            $"deltaClipVsServerMs={clipLenMs - _fullCycleMs:F1} " +
            $"AttackShotSec={attackShotSec:F3} AttackShotNorm={attackShotNorm:F3} AttackShotWallMs~={attackShotWallMs:F1} " +
            $"OnAnimationHitSec={onAnimHitSec:F3} OnAnimationHitNorm={onAnimHitNorm:F3} " +
            $"epoch={_attackEpoch}");

        Debug.Log(
            $"[ATK_HIT_CHAIN] 1b.JAtkEnter frame={Time.frameCount} t={Time.time:F3} anim={parameterName} " +
            $"fullCycleMs={_fullCycleMs:F1} hitFraction={hitFraction:F3} hitAtMs~={hitAtMs:F1} " +
            $"clipLenSec={timeAnimation:F3} AttackShotNorm={attackShotNorm:F3} " +
            $"patkspd={_linearPatkSpd:F3} player={PlayerEntity.Instance?.RandomName}");
        Debug.Log(
            $"{JATK_TIMING_LOG} Enter LINEAR anim={parameterName} serverTimeAtkMs={_fullCycleMs:F1} " +
            $"clipLenMs={clipLenMs:F1} patkspd={_linearPatkSpd:F3} AttackShotWallMs~={attackShotWallMs:F1} " +
            $"player={PlayerEntity.Instance?.RandomName}");

        if (IsDual(parameterName))
        {
            Debug.Log(
                $"{JATK_EARLY_LOG} Enter DUAL anim={parameterName} clip={clipName} " +
                $"clipLenSec={timeAnimation:F3} serverCycleSec={_endTime:F3} " +
                $"patkspd={_linearPatkSpd:F3} " +
                $"note=wall_clock_ends_at_serverCycle_even_if_dual_clip_has_2_hits");
        }

        StopAnimationTrigger(animator, parameterName);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float timeOut = Time.time - _startTime;
        _lastAnimNormalized = stateInfo.normalizedTime % 1f;
        LogAnimMilestones(timeOut);
        LogDieTargetLatch(timeOut);

        if (timeOut >= _endTime)
        {
            SwitchToIdle(stateInfo);
        }

        int entityId = ResolvePlayerEntityId();
        if (entityId > 0 && SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.UpdateAttackProgress(entityId, TimeUtils.ConvertSecToMs(timeOut));
        }

        // Keep constant rate — do not remould mid-swing.
        PlayerAnimationController.Instance.SetPAtkSpeed(_linearPatkSpd);
    }

    private void LogDieTargetLatch(float elapsedSec)
    {
        if (_dieTargetLatched)
        {
            return;
        }

        if (!IsDieTarget())
        {
            return;
        }

        _dieTargetLatched = true;
        float elapsedMs = TimeUtils.ConvertSecToMs(elapsedSec);
        bool isAttack = PlayerEntity.Instance != null && PlayerEntity.Instance.IsAttack;
        Debug.Log(
            $"{JATK_EARLY_LOG} DIE_TARGET_LATCH anim={parameterName} " +
            $"wallElapsedMs={elapsedMs:F1}/{_fullCycleMs:F1} animNorm={_lastAnimNormalized:F3} " +
            $"isAttack={isAttack} " +
            $"note=SwitchToIdle_still_waits_for_wall_cycle_unless_external_CrossFade");
    }

    private void LogAnimMilestones(float elapsedSec)
    {
        float elapsedMs = TimeUtils.ConvertSecToMs(elapsedSec);
        float wallNorm = _fullCycleMs > 0f ? elapsedMs / _fullCycleMs : 0f;
        TryLogMilestone(0, 0.25f, elapsedMs, wallNorm);
        TryLogMilestone(1, 0.40f, elapsedMs, wallNorm);
        TryLogMilestone(2, 0.50f, elapsedMs, wallNorm);
        TryLogMilestone(3, 0.75f, elapsedMs, wallNorm);
    }

    private void TryLogMilestone(int bit, float animThreshold, float elapsedMs, float wallNorm)
    {
        int flag = 1 << bit;
        if ((_milestoneMask & flag) != 0) return;
        if (_lastAnimNormalized < animThreshold) return;

        _milestoneMask |= flag;
        Debug.Log(
            $"[ATK_HIT_CHAIN] AnimMilestone anim={parameterName} animNorm>={animThreshold:F2} " +
            $"animNorm={_lastAnimNormalized:F3} wallElapsedMs={elapsedMs:F1} wallNorm={wallNorm:F3} " +
            $"frame={Time.frameCount}");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        TryUnsubscribeSwordCollider();

        float elapsedMs = TimeUtils.ConvertSecToMs(Time.time - _startTime);
        float expectedMs = TimeUtils.ConvertSecToMs(_endTime);
        bool earlyWallExit = elapsedMs + 50f < expectedMs;
        float animNormRaw = stateInfo.normalizedTime;
        int entityId = ResolvePlayerEntityId();
        if (entityId > 0 && SwordCollisionService.Instance != null)
        {
            SwordCollisionService.Instance.EndAttack(entityId, _attackEpoch);
        }

        bool dieTarget = IsDieTarget();
        bool isAttack = PlayerEntity.Instance != null && PlayerEntity.Instance.IsAttack;
        string exitCause = _exitViaSwitchToIdle
            ? "SwitchToIdle"
            : (earlyWallExit ? "EXTERNAL_interrupt_before_wall_cycle" : "EXTERNAL_or_blend_after_wall");

        Debug.Log(
            $"[ATK_TIMING_CMP] Exit anim={parameterName} epoch={_attackEpoch} " +
            $"wallElapsedMs={elapsedMs:F1} expectedServerCycleMs={expectedMs:F1} " +
            $"deltaMs={elapsedMs - expectedMs:F1} " +
            $"earlyExit={earlyWallExit} swordHit={_isSwordHitLogged} " +
            $"animNorm={_lastAnimNormalized:F3}");
        Debug.Log(
            $"{JATK_TIMING_LOG} Exit anim={parameterName} elapsedMs={elapsedMs:F1} " +
            $"expectedMs={expectedMs:F1} swordHit={_isSwordHitLogged} epoch={_attackEpoch}");

        Debug.Log(
            $"{JATK_EARLY_LOG} Exit anim={parameterName} cause={exitCause} " +
            $"viaSwitchToIdle={_exitViaSwitchToIdle} earlyWallExit={earlyWallExit} " +
            $"wallElapsedMs={elapsedMs:F1}/{expectedMs:F1} " +
            $"animNormMod={_lastAnimNormalized:F3} animNormRaw={animNormRaw:F3} " +
            $"clipLenSec={_clipLengthSec:F3} patkspd={_linearPatkSpd:F3} " +
            $"dieTarget={dieTarget} dieLatched={_dieTargetLatched} isAttack={isAttack} " +
            $"swordHit={_isSwordHitLogged} lastDeny='{_lastSwitchDenyReason}' " +
            $"frame={Time.frameCount}");

        // Do not clear IsAttack here — next Attack packet may already own the combo.
    }

    private void SwitchToIdle(AnimatorStateInfo stateInfo)
    {
        // Only called after wall-clock attack cycle ends. Do not also require anim norm —
        // pole/slow clips can still be <0.9 when server cycle is done; that skipped WAIT_RETURN.
        bool dieTarget = IsDieTarget();
        bool isAttack = PlayerEntity.Instance != null && PlayerEntity.Instance.IsAttack;
        bool canSwitch = !_isSwitchIdle && (dieTarget || !isAttack);
        if (!canSwitch)
        {
            _lastSwitchDenyReason =
                $"already={_isSwitchIdle} dieTarget={dieTarget} isAttack={isAttack} " +
                $"animNorm={_lastAnimNormalized:F3}";
            if (IsDual(parameterName))
            {
                Debug.Log(
                    $"{JATK_EARLY_LOG} SwitchToIdle DENY anim={parameterName} {_lastSwitchDenyReason}");
            }
            return;
        }

        float elapsedMs = TimeUtils.ConvertSecToMs(Time.time - _startTime);
        Debug.Log(
            $"{JATK_EARLY_LOG} SwitchToIdle FIRE anim={parameterName} " +
            $"reason={(dieTarget ? "dieTarget" : "!isAttack")} " +
            $"wallElapsedMs={elapsedMs:F1}/{_fullCycleMs:F1} animNorm={_lastAnimNormalized:F3} " +
            $"clipLenSec={_clipLengthSec:F3} → IDLE+WAIT_RETURN");

        if (PlayerEntity.Instance != null)
        {
            PlayerEntity.Instance.IsAttack = false;
        }

        _isSwitchIdle = true;
        _exitViaSwitchToIdle = true;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN, NewIdleState.WaitReturnFromCombatSmb);
    }

    private bool IsDieTarget()
    {
        if (PlayerEntity.Instance == null || World.Instance == null)
        {
            return false;
        }

        int targetId = PlayerEntity.Instance.TargetId;
        if (targetId == 0)
        {
            return false;
        }

        // Despawned after Die → null; treat as dead so we still leave jatk.
        Entity entity = World.Instance.GetEntityNoLockSync(targetId);
        return entity == null || entity.IsDead();
    }

    private void StopAnimationTrigger(Animator animator, string animParameterName)
    {
        if (animator.GetBool(animParameterName))
        {
            AnimationManager.Instance.StopCurrentAnimation(
                animator.GetInteger(AnimatorUtils.OBJECT_ID),
                animParameterName,
                "player");
        }
    }

    private static bool IsDual(string animName) =>
        !string.IsNullOrEmpty(animName) &&
        animName.IndexOf("dual", System.StringComparison.OrdinalIgnoreCase) >= 0;

    private bool IsBow(string animName) =>
        !string.IsNullOrEmpty(animName) && animName.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0;

    private void TrySubscribeSwordCollider()
    {
        if (_isColliderSubscribed || SwordCollisionService.Instance == null)
        {
            return;
        }

        SwordCollisionService.Instance.OnHitCollider += OnSwordColliderHit;
        _isColliderSubscribed = true;
    }

    private void TryUnsubscribeSwordCollider()
    {
        if (!_isColliderSubscribed || SwordCollisionService.Instance == null)
        {
            return;
        }

        SwordCollisionService.Instance.OnHitCollider -= OnSwordColliderHit;
        _isColliderSubscribed = false;
    }

    private void OnSwordColliderHit(Transform attacker, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        if (_startTime < 0f || _isSwordHitLogged || PlayerEntity.Instance == null)
        {
            return;
        }

        _isSwordHitLogged = true;
        float elapsedMs = TimeUtils.ConvertSecToMs(Time.time - _startTime);
        float expectedMs = TimeUtils.ConvertSecToMs(_endTime);
        float wallNorm = _endTime > 0f ? (Time.time - _startTime) / _endTime : 0f;
        bool sameRoot = attacker != null &&
                        PlayerEntity.Instance.transform != null &&
                        attacker.root == PlayerEntity.Instance.transform.root;

        string attackerName = attacker != null ? attacker.name : "null";
        string targetName = target != null ? target.name : "null";
        Debug.Log(
            $"[ATK_HIT_CHAIN] 5.JAtkSawHit anim={parameterName} frame={Time.frameCount} t={Time.time:F3} " +
            $"wallElapsedMs={elapsedMs:F1}/{expectedMs:F1} wallNorm={wallNorm:F3} " +
            $"animNorm={_lastAnimNormalized:F3} sameRoot={sameRoot} " +
            $"attacker={attackerName} target={targetName} hitPoint={hitPointCollider}");
        Debug.Log(
            $"{JATK_TIMING_LOG} SwordHit anim={parameterName} elapsedMs={elapsedMs:F1} " +
            $"expectedMs={expectedMs:F1} wallNorm={wallNorm:F3} animNorm={_lastAnimNormalized:F3} " +
            $"sameRoot={sameRoot} attacker={attackerName} target={targetName} hitPoint={hitPointCollider}");

        if (IsDual(parameterName))
        {
            Debug.Log(
                $"{JATK_EARLY_LOG} FirstSwordHit DUAL anim={parameterName} " +
                $"wallElapsedMs={elapsedMs:F1}/{expectedMs:F1} animNorm={_lastAnimNormalized:F3} " +
                $"target={targetName}");
        }
    }

    /// <summary>
    /// Full attack cycle ms (same as AttackTimingHelper / L2J calculateTimeBetweenAttacks).
    /// Bow: draw portion after subtracting flight time.
    /// </summary>
    private float GetFullAttackCycleMs(string animName)
    {
        if (PlayerEntity.Instance == null || PlayerEntity.Instance.Stats == null)
        {
            return 1000f;
        }

        float baseTimeAtkMs = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);

        if (IsBow(animName))
        {
            float targetDistance = PlayerEntity.Instance.TargetDistance();
            float[] timeAndFlye = CalcBaseParam.CalculateAttackAndFlightTimes(targetDistance, baseTimeAtkMs);
            return timeAndFlye[0];
        }

        // 1HS / default: full cycle. Melee Hit/SoulShot from AttackShot anim event.
        return baseTimeAtkMs;
    }

    private static int ResolvePlayerEntityId()
    {
        if (PlayerEntity.Instance == null || PlayerEntity.Instance.IdentityInterlude == null)
        {
            return 0;
        }

        return PlayerEntity.Instance.IdentityInterlude.Id;
    }
}
