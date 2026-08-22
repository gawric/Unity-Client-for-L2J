using UnityEngine;

public class PlayerOverriddenLongAtk : StateMachineBehaviour
{
    private const string TriggerCastMidLong = "CastMidLong";
    private const string TriggerCastEndLong = "CastEndLong";
    private const string TriggerMagicShotLong = "MagicShotLong";
    private const string ShotSourceLongCast = "LongCast";
    private const string LongExitLogTag = "[LONG_SMB_EXIT]";
    /// <summary>Same gate as <see cref="PlayerStateSpAtk.SwitchToIdleNormalizedTime"/> / magic SMB.</summary>
    private const float PhaseDoneNormalizedTime = 0.95f;

    private MagicCastData _castData;
    private int _objectId;
    private bool _isSwitchIdle;
    private bool _phaseFinished;
    private float _stateEnterTime;
    private bool _shotTriggered;
    private bool _eventLogged;
    private float _eventTimeInClip;
    private float _clipLengthAtEnter;
    private float _lastExitDiagLogTime;

    [System.Serializable]
    public enum CastPhase
    {
        MidPhase,
        EndLoopPhase, // Фаза удержания/ожидания таймера сервера
        FinalShotPhase
    }

    [Header("Phase Settings")]
    public CastPhase currentPhase; // Вместо stateIndex используем понятный enum
    public string parameterName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _phaseFinished = false;
        _shotTriggered = false;
        _eventLogged = false;
        _stateEnterTime = Time.time;
        _lastExitDiagLogTime = -1f;

        _objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        _castData = EntityActionSkill.ResolveCastData(_objectId);
        animator.speed = 1f;

        AnimationClip clip = AnimationDataCache.GetActiveClip(animator, layerIndex);
        _clipLengthAtEnter = clip != null ? clip.length : stateInfo.length;

        // Получаем время ивента "выстрела" только для финальной стадии
        if (currentPhase == CastPhase.FinalShotPhase && clip != null)
        {
            _eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, clip, "OnAnimationShoot");
        }

        StopAnimationTrigger(animator, parameterName);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogStateEnter();
#endif
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float globalElapsed = (_castData != null) ? (Time.time - _castData.StartTime) : -1f;

        // Loop phase needs long-cast timing; mid/final can still finish without it.
        if (currentPhase == CastPhase.EndLoopPhase)
        {
            if (_castData == null || !_castData.IsLongCast)
            {
                return;
            }

            if (!_shotTriggered && globalElapsed >= _castData.ShotTriggerGlobalOffset)
            {
                TriggerLongShot(animator, globalElapsed);
            }

            return;
        }

        if (currentPhase == CastPhase.FinalShotPhase &&
            _castData != null &&
            !_eventLogged)
        {
            float localElapsed = Time.time - _stateEnterTime;
            if (localElapsed >= _eventTimeInClip)
            {
                _eventLogged = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log(
                    $"[LONG_CAST_FIRE] global={globalElapsed:F3}s " +
                    $"delta={(globalElapsed - _castData.ShotTriggerGlobalOffset):F4}s");
#endif
            }
        }

        if (!_phaseFinished &&
            (currentPhase == CastPhase.MidPhase || currentPhase == CastPhase.FinalShotPhase))
        {
            TryFinishPhase(animator, stateInfo, globalElapsed);
        }
    }

    private void TryFinishPhase(Animator animator, AnimatorStateInfo stateInfo, float globalElapsed)
    {
        if (_phaseFinished)
        {
            return;
        }

        float localElapsed = Time.time - _stateEnterTime;
        float speed = Mathf.Max(0.0001f, animator.speed);
        float wallNeed = _clipLengthAtEnter / speed;
        bool wallDone = localElapsed >= wallNeed;
        bool normDone = stateInfo.normalizedTime >= PhaseDoneNormalizedTime;
        bool phaseDone = wallDone || normDone;

        bool shouldDiag =
            phaseDone ||
            _lastExitDiagLogTime < 0f ||
            (localElapsed - _lastExitDiagLogTime) >= 0.5f;
        if (shouldDiag)
        {
            _lastExitDiagLogTime = localElapsed;
            Debug.Log(
                $"{LongExitLogTag} CHECK phase={currentPhase} state={parameterName} " +
                $"local={localElapsed:F3}s global={globalElapsed:F3}s " +
                $"norm={stateInfo.normalizedTime:F3} (need>={PhaseDoneNormalizedTime:F2}) normDone={normDone} " +
                $"speed={animator.speed:F3} clipLen={_clipLengthAtEnter:F3} wallNeed={wallNeed:F3}s wallDone={wallDone} " +
                $"alreadyFinished={_phaseFinished} gateWillFire={phaseDone}");
        }

        if (!phaseDone)
        {
            return;
        }

        _phaseFinished = true;
        int objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        Debug.Log(
            $"{LongExitLogTag} PHASE_DONE phase={currentPhase} state={parameterName} " +
            $"local={localElapsed:F3}s norm={stateInfo.normalizedTime:F3} wallDone={wallDone} → NotifyMagicPhaseFinished");

        if (AnimationManager.Instance != null)
        {
            AnimationManager.Instance.NotifyMagicPhaseFinished(objectId, parameterName);
        }

        if (currentPhase == CastPhase.FinalShotPhase)
        {
            SwitchToIdle();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (currentPhase == CastPhase.FinalShotPhase)
        {
            animator.speed = 1f;
        }
    }

    private void TriggerLongShot(Animator animator, float globalElapsed)
    {
        _shotTriggered = true;
        int objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);

        if (MagicShotCoordinator.TryStartShot(objectId, _castData, ShotSourceLongCast, out string coordinatorMessage))
        {
            // Сбрасываем старые триггеры, чтобы избежать залипания
            animator.ResetTrigger(TriggerCastMidLong);
            animator.ResetTrigger(TriggerCastEndLong);

            // Толкаем триггер перехода в финальную анимацию выстрела
            animator.SetTrigger(TriggerMagicShotLong);

            Debug.Log($"[LONG_CAST_SHOT] {coordinatorMessage} global={globalElapsed:F3}s target={_castData.ShotTriggerGlobalOffset:F3}s");
        }

        // Unblocks AsyncPlayLongCastLoopPhase (runner already awaiting MagicShotLong finish).
        LongCastCoordinator.CompleteLoopPhase(objectId);
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
            $"{LongExitLogTag} SwitchToIdle FIRE state={parameterName} local={localElapsed:F3}s " +
            $"→ INTENTION_IDLE + WAIT_RETURN");

        if (!EntityActionSkill.IsLocalPlayer(_objectId))
        {
            EntityActionSkill.FinishRemoteCast(_objectId);
            return;
        }

        if (PlayerEntity.Instance != null)
        {
            PlayerEntity.Instance.IsAttack = false;
        }

        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN, NewIdleState.WaitReturnFromCombatSmb);
    }

    private void StopAnimationTrigger(Animator animator, string triggerName)
    {
        if (!string.IsNullOrEmpty(triggerName) && animator.GetBool(triggerName))
        {
            AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), triggerName, "player");
        }
    }

    private void LogStateEnter()
    {
        float globalSinceCastStart = (_castData != null) ? (Time.time - _castData.StartTime) : -1f;
        string exitGate = currentPhase == CastPhase.EndLoopPhase
            ? "loop→shot@ShotTriggerGlobalOffset"
            : $"wall|norm>={PhaseDoneNormalizedTime:F2}→NotifyPhase" +
              (currentPhase == CastPhase.FinalShotPhase ? "+SwitchToIdle" : "");
        Debug.Log(
            $"[LONG_CAST_ENTER] phase={currentPhase} state={parameterName} " +
            $"globalSinceStart={globalSinceCastStart:F3}s " +
            $"shotOffset={(_castData != null ? _castData.ShotTriggerGlobalOffset.ToString("F3") : "null")}s " +
            $"clipLen={_clipLengthAtEnter:F3} exitGate={exitGate}");
    }
}
