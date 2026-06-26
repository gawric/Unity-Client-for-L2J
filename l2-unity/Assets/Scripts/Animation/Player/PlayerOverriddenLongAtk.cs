using UnityEngine;

public class PlayerOverriddenLongAtk : StateMachineBehaviour
{
    private const string TriggerCastMidLong = "CastMidLong";
    private const string TriggerCastEndLong = "CastEndLong";
    private const string TriggerMagicShotLong = "MagicShotLong";
    private const string ShotSourceLongCast = "LongCast";

    private MagicCastData _castData;
    private bool _isSwitchIdle;
    private float _stateEnterTime;
    private bool _shotTriggered;
    private bool _eventLogged;
    private float _eventTimeInClip;

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
        _shotTriggered = false;
        _eventLogged = false;
        _stateEnterTime = Time.time;

        _castData = PlayerEntity.Instance != null ? PlayerEntity.Instance.GetMagicCastData() : null;
        animator.speed = 1f;

        // Получаем время ивента "выстрела" только для финальной стадии
        if (currentPhase == CastPhase.FinalShotPhase)
        {
            AnimationClip clip = AnimationDataCache.GetActiveClip(animator, layerIndex);
            if (clip != null)
            {
                _eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, clip, "OnAnimationShoot");
            }
        }

        StopAnimationTrigger(animator, parameterName);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogStateEnter();
#endif
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_castData == null || !_castData.IsLongCast) return;

        float globalElapsed = Time.time - _castData.StartTime;

        // ЛОГИКА ДЛЯ ХОЛД-ФАЗЫ (Ожидание серверного таймера)
        if (currentPhase == CastPhase.EndLoopPhase && !_shotTriggered)
        {
            // Если серверное время еще не вышло — анимация должна циклиться (накапливать энергию)
            // Убедитесь, что у самой анимации в инспекторе включен Loop Time!

            if (globalElapsed >= _castData.ShotTriggerGlobalOffset)
            {
                TriggerLongShot(animator, globalElapsed);
            }
        }

        // ЛОГИКА ДЛЯ ФИНАЛЬНОГО ВЫСТРЕЛА
        if (currentPhase == CastPhase.FinalShotPhase)
        {
            if (!_eventLogged)
            {
                float localElapsed = Time.time - _stateEnterTime;
                if (localElapsed >= _eventTimeInClip)
                {
                    _eventLogged = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[LONG_CAST_FIRE] global={globalElapsed:F3}s delta={(globalElapsed - _castData.ShotTriggerGlobalOffset):F4}s");
#endif
                }
            }

            // Анимация выстрела полностью завершилась -> уходим в Idle
            if (stateInfo.normalizedTime >= 0.98f)
            {
                SwitchToIdle();
            }
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

        LongCastCoordinator.CompleteLoopPhase(objectId);
    }

    private void SwitchToIdle()
    {
        if (_isSwitchIdle) return;
        _isSwitchIdle = true;

        PlayerEntity.Instance.IsAttack = false;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
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
        Debug.Log($"[LONG_CAST_ENTER] phase={currentPhase} globalSinceStart={globalSinceCastStart:F3}s " +
                  $"shotOffset={(_castData != null ? _castData.ShotTriggerGlobalOffset.ToString("F3") : "null")}s");
    }
}
