using UnityEngine;

public class PlayerStateSpAtk : StateMachineBehaviour
{
    private float _startTime;
    private float _clipLength;
    private float _eventHitTimeInClip;
    private float _serverHitTime;
    private bool _isSwitchIdle;
    private bool _hitExecuted;
    private float _calculatedPostSpeed; // Скорость, которую мы вычислим для выхода

    public string motionName;
    public string eventStartHitName; // "hit_start"
    public string eventEndHitName;   // "hit_end"

    [Header("Настройка выхода")]
    public float postHitSpeed = 2.0f; // Просто фиксированная скорость после удара
    private float _lastDebugTime; // Для интервала дебага
    private const float DEBUG_INTERVAL = 0.02f; // 20мс
   

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _hitExecuted = false;
        _startTime = Time.time;

        _clipLength = AnimationDataCache.GetOverrideLength(animator, motionName);

        // 1. Получаем границы окна удара
        float startHit = AnimationDataCache.GetEventTimeByName(animator, motionName, eventStartHitName);
        float endHit = AnimationDataCache.GetEventTimeByName(animator, motionName, eventEndHitName);

        // 2. Вычисляем "Золотую середину" — когда меч должен быть внутри монстра
        _eventHitTimeInClip = (startHit + endHit) / 2f;

        int serverTimeMs = animator.GetInteger("sptimeatk");

        // 3. Применяем компенсацию (учитываем пинг и переход)
        float compensation = 0.08f;
        _serverHitTime = (serverTimeMs / 1000f) - compensation;

        if (_serverHitTime < 0.1f) _serverHitTime = 0.1f;

        // 4. Скорость: Путь до середины удара / Время до хита от сервера
        float startSpeed = _eventHitTimeInClip / _serverHitTime;

        animator.speed = startSpeed;
        animator.Update(0);

        Debug.Log($"<color=cyan>[Sync]</color> Окно хита: {startHit:F2}-{endHit:F2} (Центр: {_eventHitTimeInClip:F2}). " +
                  $"Скорость: {startSpeed:F2}");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float elapsed = Time.time - _startTime;

        if (Time.time - _lastDebugTime >= DEBUG_INTERVAL)
        {
            _lastDebugTime = Time.time;
            Debug.Log($"<color=grey>[Monitor]</color> Time: {elapsed:F3}s | NormTime: {stateInfo.normalizedTime:F3} | AnimSpeed: {animator.speed:F2}");
        }

        if (elapsed < _serverHitTime)
        {
            // Фаза замаха (скорость уже задана в OnStateEnter)
        }
        else
        {
            if (!_hitExecuted)
            {
                _hitExecuted = true;

                // Устанавливаем ЖЕСТКУЮ скорость без вычислений времени
                // PlayerAnimationController.Instance.SetPAtkSpeed(postHitSpeed);
                animator.speed = postHitSpeed; // Меняем системную скорость напрямую
                float diff = (elapsed - _serverHitTime) * 1000f;
                Debug.Log($"<color=yellow>[HIT]</color> Diff: {diff:F1}ms | Switch Speed to: {postHitSpeed}");
            }

            // Выход в Idle, когда анимация в стейте дошла до конца (normalizedTime >= 1)
            // stateInfo.normalizedTime показывает прогресс текущей анимации от 0 до 1
            if (stateInfo.normalizedTime >= 0.95f)
            {
                SwitchToIdle(animator);
            }
        }
    }

    private void SwitchToIdle(Animator animator)
    {
        if (_isSwitchIdle) return;
        _isSwitchIdle = true;

        animator.speed = 1f; // Меняем системную скорость напрямую
        PlayerEntity.Instance.IsAttack = false;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1f; // Меняем системную скорость напрямую
    }
}
