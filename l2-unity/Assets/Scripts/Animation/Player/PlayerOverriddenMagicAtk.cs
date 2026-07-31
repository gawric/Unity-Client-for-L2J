using UnityEngine;

public class PlayerOverriddenMagicAtk : StateMachineBehaviour
{
    private const string TriggerCastMid = "CastMid";
    private const string TriggerCastEnd = "CastEnd";
    private const string TriggerMagicShot = "MagicShot";
    private const float ShotBlendCompensationSeconds = 0.06f;
    /// <summary>
    /// Принудительный MagicShot по wall-clock во время CastEnd (включено только при EnableForceSync = true и HitTime пороге ниже).
    /// Сейчас выключено — только SkillAnimationRunner.
    /// </summary>
    private const bool EnableForceSync = false;

    /// <summary>
    /// ForceSync только когда server HitTime >= этого порога (сек). Короче 2 с — только runner, без принудительного MagicShot.
    /// </summary>
    private const float ForceSyncMinServerHitTimeSeconds = 2f;
    private const string ShotSourceForceSync = "ForceSync";
    private const string MagicSpeedTraceTag = "[MAGIC_SPEED_TRACE]";

    private const string MagicExitLogTag = "[MAGIC_SMB_EXIT]";

    private MagicCastData _castData;
    private bool _isSwitchIdle;
    private float _stateEnterTime;
    private bool _forcedShotTriggered;
    private float _lastExitDiagLogTime;
    private float _clipLengthAtEnter;

    [Header("Settings")]
    public string parameterName; 
    public bool isFinalShotState;
    public int stateIndex;
    private bool _eventLogged = false;
    float _eventTimeInClip;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _forcedShotTriggered = false;
        _stateEnterTime = Time.time;
        _lastExitDiagLogTime = -1f;

        _castData = PlayerEntity.Instance.GetMagicCastData();
        float targetSpeed = 1.0f;
        if (_castData != null)
        {
            if (stateIndex == 0) targetSpeed = _castData.SpeedMid;
            else if (stateIndex == 1) targetSpeed = _castData.SpeedEnd;
            else if (stateIndex == 2) targetSpeed = _castData.SpeedShot;
        }

        animator.speed = targetSpeed;

        // ПОЛУЧАЕМ ВРЕМЯ ИВЕНТА
        AnimationClip clip = AnimationDataCache.GetActiveClip(animator, layerIndex);
        _clipLengthAtEnter = clip != null ? clip.length : stateInfo.length;
        if (clip != null)
        {
            _eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, clip, "OnAnimationShoot");
        }

        StopAnimationTrigger(animator, parameterName);
        _eventLogged = false; // Сбрасываем флаг при входе в стейт

        Debug.Log(
            $"[MAGIC_ENTER] state={parameterName} idx={stateIndex} final={isFinalShotState} " +
            $"now={Time.time:F3} castDataNull={(_castData == null)} " +
            $"castDataHash={(_castData != null ? _castData.GetHashCode().ToString() : "null")} " +
            $"start={(_castData != null ? _castData.StartTime.ToString("F3") : "null")} " +
            $"hit={(_castData != null ? _castData.HitTime.ToString("F3") : "null")} " +
            $"flight={(_castData != null ? _castData.FlightTime.ToString("F3") : "null")} " +
            $"shootAt={(_castData != null ? _castData.serverTimeToShoot.ToString("F3") : "null")} " +
            $"globalAtEnter={(_castData != null ? (Time.time - _castData.StartTime).ToString("F3") : "null")} " +
            $"animSpeed={animator.speed:F3} clipLen={_clipLengthAtEnter:F3} " +
            $"exitGate={(isFinalShotState ? "norm>=1→SwitchToIdle" : "no-SMB-exit")} " +
            $"animId={animator.GetInstanceID()}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float globalSinceCastStart = (_castData != null) ? (Time.time - _castData.StartTime) : -1f;
        string clipName = (clip != null) ? clip.name : "null";
        if (_castData != null)
        {
            Debug.Log(
                $"[CastDataSnapshot] state='{parameterName}' idx={stateIndex} " +
                $"startTime={_castData.StartTime:F3}s now={Time.time:F3}s globalSinceStart={globalSinceCastStart:F3}s " +
                $"hit={_castData.HitTime:F3}s flight={_castData.FlightTime:F3}s serverShoot={_castData.serverTimeToShoot:F3}s " +
                $"shotEvent={_castData.shotEventTime:F3}s speedMid={_castData.SpeedMid:F3} " +
                $"speedEnd={_castData.SpeedEnd:F3} speedShot={_castData.SpeedShot:F3}.");

            Debug.Log(
                $"{MagicSpeedTraceTag} ENTER state={parameterName} idx={stateIndex} final={isFinalShotState} " +
                $"appliedSpeed={targetSpeed:F3} speedMid={_castData.SpeedMid:F3} speedEnd={_castData.SpeedEnd:F3} speedShot={_castData.SpeedShot:F3} " +
                $"shootAt={_castData.serverTimeToShoot:F3}s shotEvent={_castData.shotEventTime:F3}s");

            if (stateIndex == 1)
            {
                if (!EnableForceSync)
                {
                    Debug.Log(
                        $"{MagicSpeedTraceTag} ForceSync OFF globally (runner-only); serverHit={_castData.HitTime:F3}s.");
                }
                else if (!IsHitTimeInsideForceSyncBand(_castData))
                {
                    Debug.Log(
                        $"{MagicSpeedTraceTag} ForceSync skipped serverHit={_castData.HitTime:F3}s " +
                        $"(need HitTime>={ForceSyncMinServerHitTimeSeconds:F0}s); runner-only.");
                }
            }
        }

        Debug.Log(
            $"[AnimStateEnter] state='{parameterName}' idx={stateIndex} final={isFinalShotState} " +
            $"globalSinceCastStart={globalSinceCastStart:F3}s speed={animator.speed:F3} " +
            $"eventTimeInClip={_eventTimeInClip:F3}s clip='{clipName}' normalized={stateInfo.normalizedTime:F3} " +
            $"layer={layerIndex} animatorInstanceId={animator.GetInstanceID()}");
#endif
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_castData == null)
        {
            // Without castData the whole update (including SwitchToIdle) is skipped.
            if (isFinalShotState && (Time.time - _lastExitDiagLogTime) >= 0.5f)
            {
                _lastExitDiagLogTime = Time.time;
                Debug.Log(
                    $"{MagicExitLogTag} SKIP_UPDATE state={parameterName} reason=castData_null " +
                    $"final={isFinalShotState} idx={stateIndex} norm={stateInfo.normalizedTime:F3} " +
                    $"speed={animator.speed:F3} local={(Time.time - _stateEnterTime):F3}s");
            }
            return;
        }


        float localElapsed = Time.time - _stateEnterTime;
        float globalElapsed = Time.time - _castData.StartTime;

        if (!_forcedShotTriggered && stateIndex == 1 && !isFinalShotState && ShouldUseForceSync(_castData))
        {
            float rawShootAt = Mathf.Max(0.01f, _castData.serverTimeToShoot);
            float shotEventLeadTime = Mathf.Max(0f, _castData.shotEventTime);
            float compensatedShootAt = Mathf.Max(0.01f, rawShootAt - shotEventLeadTime - ShotBlendCompensationSeconds);
            Debug.Log(
                $"[MAGIC_CHECK] now={Time.time:F3} state={parameterName} idx={stateIndex} " +
                $"global={globalElapsed:F3} rawShootAt={rawShootAt:F3} shotEventLead={shotEventLeadTime:F3} compShootAt={compensatedShootAt:F3} " +
                $"ready={(globalElapsed >= compensatedShootAt)} forced={_forcedShotTriggered}");
            if (globalElapsed >= compensatedShootAt)
            {
                _forcedShotTriggered = true;
                // Global animator slowdown (CastEnd speed) also slows state transitions.
                // Restore normal speed right before forcing MagicShot to avoid delayed transition.
                float speedBeforeForce = animator.speed;
                animator.speed = 1.0f;
                Debug.Log(
                    $"{MagicSpeedTraceTag} FORCE_SYNC state={parameterName} global={globalElapsed:F3}s " +
                    $"speedBeforeForce={speedBeforeForce:F3} speedAfterForce={animator.speed:F3} rawShootAt={rawShootAt:F3}s compShootAt={compensatedShootAt:F3}s");
                int objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);
                if (MagicShotCoordinator.TryStartShot(objectId, _castData, ShotSourceForceSync, out string coordinatorMessage))
                {
                    animator.ResetTrigger(TriggerCastMid);
                    animator.ResetTrigger(TriggerCastEnd);
                    animator.ResetTrigger(TriggerMagicShot);
                    animator.SetTrigger(TriggerMagicShot);
                    Debug.Log(
                        $"[MAGIC_TRIGGER] now={Time.time:F3} global={globalElapsed:F3} " +
                        $"targetRaw={rawShootAt:F3} shotEventLead={shotEventLeadTime:F3} targetComp={compensatedShootAt:F3} " +
                        $"deltaRaw={(globalElapsed - rawShootAt):F3} deltaComp={(globalElapsed - compensatedShootAt):F3} " +
                        $"castDataHash={_castData.GetHashCode()} animId={animator.GetInstanceID()} objectId={objectId}");
                    Debug.Log($"{coordinatorMessage} action=run");
                }
                else
                {
                    Debug.Log($"{coordinatorMessage} action=skip");
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log(
                    $"[ForceShotSync] Triggered MagicShot from CastEnd at global={globalElapsed:F3}s " +
                    $"targetRaw={rawShootAt:F3}s shotEventLead={shotEventLeadTime:F3}s targetComp={compensatedShootAt:F3}s comp={ShotBlendCompensationSeconds:F3}s " +
                    $"deltaRaw={globalElapsed - rawShootAt:F3}s deltaComp={globalElapsed - compensatedShootAt:F3}s " +
                    $"animatorSpeedNow={animator.speed:F3} animatorInstanceId={animator.GetInstanceID()}.");
#endif
            }
        }

        // Проверяем, наступил ли момент выстрела (только для финального стейта)
        if (isFinalShotState && !_eventLogged)
        {
            // Вычисляем текущее время ВНУТРИ клипа с учетом скорости
            float localElapsed1 = Time.time - _stateEnterTime;
            float currentClipTime = localElapsed1 * animator.speed;

            // Если мы прошли точку ивента (например, 0.541с)
            if (currentClipTime >= _eventTimeInClip)
            {
                _eventLogged = true;
                Debug.Log($"<color=cyan>[FIRE_SYNC]</color> ВЫСТРЕЛ! " +
                          $"Global: {globalElapsed:F3}s (Цель: {_castData.HitTime - _castData.FlightTime:F3}s) | " +
                          $"Разница: {globalElapsed - (_castData.HitTime - _castData.FlightTime):F4}s");
                Debug.Log(
                    $"{MagicSpeedTraceTag} FIRE_SYNC state={parameterName} local={localElapsed1:F3}s clipTime={currentClipTime:F3}s " +
                    $"eventTime={_eventTimeInClip:F3}s animSpeed={animator.speed:F3}");
            }
        }


        if (isFinalShotState)
        {
            if (_isSwitchIdle)
            {
                return;
            }

            bool normDone = stateInfo.normalizedTime >= 1.0f;
            float speed = Mathf.Max(0.0001f, animator.speed);
            float wallNeed = _clipLengthAtEnter / speed;
            bool wallDone = localElapsed >= wallNeed;
            bool wouldExit = normDone; // current gate only — wallDone is diag only

            // Throttled while stuck; always log the moment the gate would fire.
            bool shouldDiag =
                wouldExit ||
                _lastExitDiagLogTime < 0f ||
                (localElapsed - _lastExitDiagLogTime) >= 0.5f;
            if (shouldDiag)
            {
                _lastExitDiagLogTime = localElapsed;
                Debug.Log(
                    $"{MagicExitLogTag} CHECK state={parameterName} idx={stateIndex} " +
                    $"local={localElapsed:F3}s global={globalElapsed:F3}s " +
                    $"norm={stateInfo.normalizedTime:F3} (need>=1) normDone={normDone} " +
                    $"speed={animator.speed:F3} clipLen={_clipLengthAtEnter:F3} wallNeed={wallNeed:F3}s wallDone={wallDone} " +
                    $"alreadySwitched={_isSwitchIdle} gateWillCallSwitch={wouldExit && !_isSwitchIdle}");
            }

            if (normDone)
            {
                SwitchToIdle();
            }
        }
        else if (_lastExitDiagLogTime < 0f || (localElapsed - _lastExitDiagLogTime) >= 1.0f)
        {
            // CastMid/CastEnd: confirm SMB is alive but exit gate is intentionally off.
            _lastExitDiagLogTime = localElapsed;
            Debug.Log(
                $"{MagicExitLogTag} NO_EXIT_GATE state={parameterName} idx={stateIndex} final=False " +
                $"local={localElapsed:F3}s norm={stateInfo.normalizedTime:F3} speed={animator.speed:F3}");
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Only reset animator.speed when leaving MagicShot (stateIndex 2). Intermediate exits
        // (CastMid / CastEnd) must not set speed=1: Unity can call the next state's OnStateEnter
        // before the previous state's OnStateExit, so Exit would overwrite SpeedShot and
        // OnAnimationShoot runs at animSpeed=1 (FIRE_SYNC vs CastTimingSetup mismatch).
        if (stateIndex == 2)
        {
            animator.speed = 1.0f;
        }

        float finalLocalTime = Time.time - _stateEnterTime;
        float globalSinceCastStart = (_castData != null) ? (Time.time - _castData.StartTime) : -1f;
        Debug.Log(
            $"[MAGIC_EXIT] state={parameterName} idx={stateIndex} now={Time.time:F3} " +
            $"globalAtExit={(_castData != null ? (Time.time - _castData.StartTime).ToString("F3") : "null")} " +
            $"normalized={stateInfo.normalizedTime:F3} animSpeed={animator.speed:F3} animId={animator.GetInstanceID()}");
        Debug.Log(
            $"[AnimLog] EXIT {parameterName} | Total Local: {finalLocalTime:F3}s | " +
            $"Global: {globalSinceCastStart:F3}s | normalized: {stateInfo.normalizedTime:F3} | " +
            $"Layer: {layerIndex} | Animator: {animator.GetInstanceID()}");

    }

    private void SwitchToIdle()
    {
        if (_isSwitchIdle)
        {
            return;
        }

        _isSwitchIdle = true;
        float localElapsed = Time.time - _stateEnterTime;
        Debug.Log(
            $"{MagicExitLogTag} SwitchToIdle FIRE state={parameterName} local={localElapsed:F3}s " +
            $"→ INTENTION_IDLE + WAIT_RETURN");

        if (PlayerEntity.Instance != null)
        {
            PlayerEntity.Instance.IsAttack = false;
        }

        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }

    private static bool ShouldUseForceSync(MagicCastData castData)
    {
        return castData != null && EnableForceSync && IsHitTimeInsideForceSyncBand(castData);
    }

    private static bool IsHitTimeInsideForceSyncBand(MagicCastData castData)
    {
        return castData != null && castData.HitTime >= ForceSyncMinServerHitTimeSeconds;
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (!string.IsNullOrEmpty(parameterName) && animator.GetBool(parameterName))
        {
            AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName, "player");
        }
    }
}