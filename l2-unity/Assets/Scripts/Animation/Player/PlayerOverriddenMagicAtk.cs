using UnityEngine;

public class PlayerOverriddenMagicAtk : StateMachineBehaviour
{
    private MagicCastData _castData;
    private bool _isSwitchIdle;
    private float _stateEnterTime;

    [Header("Settings")]
    public string parameterName; 
    public bool isFinalShotState;
    public int stateIndex;
    private bool _eventLogged = false;
    float _eventTimeInClip;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
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
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_castData == null) return;


        float localElapsed = Time.time - _stateEnterTime;
        float globalElapsed = Time.time - _castData.StartTime;
         Debug.Log($"[AnimLog] {parameterName} | Local: {localElapsed:F3}s | Global: {globalElapsed:F3}s");

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
        Debug.Log($"[AnimLog] EXIT {parameterName} | Total Local: {finalLocalTime:F3}s");
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