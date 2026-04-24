using UnityEngine;

public class PlayerOverriddenMagicAtk : StateMachineBehaviour
{
    private const string TriggerCastMid = "CastMid";
    private const string TriggerCastEnd = "CastEnd";
    private const string TriggerMagicShot = "MagicShot";
    private const float ShotBlendCompensationSeconds = 0.06f;

    private MagicCastData _castData;
    private bool _isSwitchIdle;
    private float _stateEnterTime;
    private bool _forcedShotTriggered;

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

        _castData = PlayerEntity.Instance.GetMagicCastData();
        float targetSpeed = 1.0f;
        if (stateIndex == 0) targetSpeed = _castData.SpeedMid;
        else if (stateIndex == 1) targetSpeed = _castData.SpeedEnd;
        else if (stateIndex == 2) targetSpeed = _castData.SpeedShot;

        animator.speed = targetSpeed;

        // ПОЛУЧАЕМ ВРЕМЯ ИВЕНТА
        AnimationClip clip = AnimationDataCache.GetActiveClip(animator, layerIndex);
        if (clip != null)
        {
            _eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, clip, "OnAnimationShoot");
        }

        StopAnimationTrigger(animator, parameterName);
        _eventLogged = false; // Сбрасываем флаг при входе в стейт

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float globalSinceCastStart = (_castData != null) ? (Time.time - _castData.StartTime) : -1f;
        string clipName = (clip != null) ? clip.name : "null";
        if (_castData != null)
        {
           // Debug.Log(
           //     $"[CastDataSnapshot] state='{parameterName}' idx={stateIndex} " +
           //     $"startTime={_castData.StartTime:F3}s now={Time.time:F3}s globalSinceStart={globalSinceCastStart:F3}s " +
           //     $"hit={_castData.HitTime:F3}s flight={_castData.FlightTime:F3}s serverShoot={_castData.serverTimeToShoot:F3}s " +
           //     $"shotEvent={_castData.shotEventTime:F3}s speedMid={_castData.SpeedMid:F3} " +
           //     $"speedEnd={_castData.SpeedEnd:F3} speedShot={_castData.SpeedShot:F3}.");
        }

        //Debug.Log(
         //   $"[AnimStateEnter] state='{parameterName}' idx={stateIndex} final={isFinalShotState} " +
         //   $"globalSinceCastStart={globalSinceCastStart:F3}s speed={animator.speed:F3} " +
         //   $"eventTimeInClip={_eventTimeInClip:F3}s clip='{clipName}' normalized={stateInfo.normalizedTime:F3} " +
         //   $"layer={layerIndex} animatorInstanceId={animator.GetInstanceID()}");
#endif
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_castData == null) return;


        float localElapsed = Time.time - _stateEnterTime;
        float globalElapsed = Time.time - _castData.StartTime;
         //Debug.Log($"[AnimLog] {parameterName} | Local: {localElapsed:F3}s | Global: {globalElapsed:F3}s | Layer: {layerIndex} | Animator: {animator.GetInstanceID()}");

        if (!_forcedShotTriggered && stateIndex == 1 && !isFinalShotState)
        {
            float rawShootAt = Mathf.Max(0.01f, _castData.serverTimeToShoot);
            float compensatedShootAt = Mathf.Max(0.01f, rawShootAt - ShotBlendCompensationSeconds);
            if (globalElapsed >= compensatedShootAt)
            {
                _forcedShotTriggered = true;
                // Global animator slowdown (CastEnd speed) also slows state transitions.
                // Restore normal speed right before forcing MagicShot to avoid delayed transition.
                animator.speed = 1.0f;
                animator.ResetTrigger(TriggerCastMid);
                animator.ResetTrigger(TriggerCastEnd);
                animator.ResetTrigger(TriggerMagicShot);
                animator.SetTrigger(TriggerMagicShot);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
               // Debug.Log(
               //     $"[ForceShotSync] Triggered MagicShot from CastEnd at global={globalElapsed:F3}s " +
               //     $"targetRaw={rawShootAt:F3}s targetComp={compensatedShootAt:F3}s comp={ShotBlendCompensationSeconds:F3}s " +
               //     $"deltaRaw={globalElapsed - rawShootAt:F3}s deltaComp={globalElapsed - compensatedShootAt:F3}s " +
               //     $"animatorSpeedNow={animator.speed:F3} animatorInstanceId={animator.GetInstanceID()}.");
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
               // Debug.Log($"<color=cyan>[FIRE_SYNC]</color> ВЫСТРЕЛ! " +
                //          $"Global: {globalElapsed:F3}s (Цель: {_castData.HitTime - _castData.FlightTime:F3}s) | " +
               //           $"Разница: {globalElapsed - (_castData.HitTime - _castData.FlightTime):F4}s");
            }
        }


        if (isFinalShotState && stateInfo.normalizedTime >= 1.0f)
        {
            SwitchToIdle();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1.0f;

        float finalLocalTime = Time.time - _stateEnterTime;
        float globalSinceCastStart = (_castData != null) ? (Time.time - _castData.StartTime) : -1f;
       // Debug.Log(
       //     $"[AnimLog] EXIT {parameterName} | Total Local: {finalLocalTime:F3}s | " +
       //     $"Global: {globalSinceCastStart:F3}s | normalized: {stateInfo.normalizedTime:F3} | " +
       //     $"Layer: {layerIndex} | Animator: {animator.GetInstanceID()}");
    }

    private void SwitchToIdle()
    {
        if (_isSwitchIdle) return;
        _isSwitchIdle = true;

        PlayerEntity.Instance.IsAttack = false;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (!string.IsNullOrEmpty(parameterName) && animator.GetBool(parameterName))
        {
            AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName, "player");
        }
    }
}