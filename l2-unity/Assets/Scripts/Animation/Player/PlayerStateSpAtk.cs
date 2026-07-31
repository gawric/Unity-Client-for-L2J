using UnityEngine;

public class PlayerStateSpAtk : StateMachineBehaviour
{
    public const float HitTimeCompensationSeconds = 0.08f;
    public const float MinServerHitSeconds = 0.1f;
    public const float SwitchToIdleNormalizedTime = 0.95f;

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

    /// <summary>
    /// Wall-clock duration until SwitchToIdle (norm >= 0.95), same formula as runtime SpAtk scaling.
    /// Used by melee skill FX that match effect lifetime to the swing.
    /// </summary>
    public static bool TryEstimateWallDuration(
        Animator animator,
        string spatkAnimName,
        int serverHitTimeMs,
        out float wallDurationSeconds,
        out string debugMotionName)
    {
        wallDurationSeconds = -1f;
        debugMotionName = null;
        if (animator == null || serverHitTimeMs <= 0)
        {
            return false;
        }

        if (!TryResolveBehaviour(animator, spatkAnimName, out PlayerStateSpAtk behaviour) || behaviour == null)
        {
            return false;
        }

        debugMotionName = behaviour.motionName;
        float clipLength = AnimationDataCache.GetOverrideLength(animator, behaviour.motionName);
        float startHit = AnimationDataCache.GetEventTimeByName(animator, behaviour.motionName, behaviour.eventStartHitName);
        float endHit = AnimationDataCache.GetEventTimeByName(animator, behaviour.motionName, behaviour.eventEndHitName);
        float hitWindowCenter = (startHit + endHit) * 0.5f;

        float serverHitTime = (serverHitTimeMs / 1000f) - HitTimeCompensationSeconds;
        if (serverHitTime < MinServerHitSeconds)
        {
            serverHitTime = MinServerHitSeconds;
        }

        float postSpeed = Mathf.Max(0.01f, behaviour.postHitSpeed);
        float afterHit = Mathf.Max(0f, (SwitchToIdleNormalizedTime * clipLength - hitWindowCenter) / postSpeed);
        wallDurationSeconds = serverHitTime + afterHit;
        return wallDurationSeconds > 0f;
    }

    private static bool TryResolveBehaviour(Animator animator, string spatkAnimName, out PlayerStateSpAtk behaviour)
    {
        behaviour = null;
        PlayerStateSpAtk[] behaviours = animator.GetBehaviours<PlayerStateSpAtk>();
        if (behaviours == null || behaviours.Length == 0)
        {
            return false;
        }

        if (behaviours.Length == 1)
        {
            behaviour = behaviours[0];
            return true;
        }

        bool want02 = !string.IsNullOrEmpty(spatkAnimName) &&
                      spatkAnimName.IndexOf("SpAtk02", System.StringComparison.OrdinalIgnoreCase) >= 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            PlayerStateSpAtk candidate = behaviours[i];
            if (candidate == null || string.IsNullOrEmpty(candidate.motionName))
            {
                continue;
            }

            string motion = candidate.motionName;
            bool is02 = motion.IndexOf("spatk002", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        motion.IndexOf("spatk02", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        motion.IndexOf("SpAtk02", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (is02 == want02)
            {
                behaviour = candidate;
                return true;
            }
        }

        behaviour = behaviours[0];
        return behaviour != null;
    }

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
        _serverHitTime = (serverTimeMs / 1000f) - HitTimeCompensationSeconds;

        if (_serverHitTime < MinServerHitSeconds) _serverHitTime = MinServerHitSeconds;

        // 4. Скорость: Путь до середины удара / Время до хита от сервера
        float startSpeed = _eventHitTimeInClip / _serverHitTime;

        animator.speed = startSpeed;
        animator.Update(0);

        Debug.Log(
            $"[POWER_STRIKE_TIMING] ANIM_ENTER now={Time.time:F3}s motion='{motionName}' " +
            $"clipLen={_clipLength:F3}s hitWindowCenter={_eventHitTimeInClip:F3}s " +
            $"serverHitMs={serverTimeMs} serverHitSec={_serverHitTime:F3}s startSpeed={startSpeed:F3} " +
            $"estWallToHit={_serverHitTime:F3}s " +
            $"(remaining clip after hit ~{Mathf.Max(0f, (_clipLength - _eventHitTimeInClip) / Mathf.Max(0.01f, postHitSpeed)):F3}s at postHitSpeed={postHitSpeed:F2})");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float elapsed = Time.time - _startTime;

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
                Debug.Log(
                    $"[POWER_STRIKE_TIMING] ANIM_HIT now={Time.time:F3}s wallElapsed={elapsed:F3}s " +
                    $"diffToServerHitMs={diff:F1} norm={stateInfo.normalizedTime:F3} → postHitSpeed={postHitSpeed:F2}");
            }

            // Выход в Idle, когда анимация в стейте дошла до конца (normalizedTime >= 1)
            // stateInfo.normalizedTime показывает прогресс текущей анимации от 0 до 1
            if (stateInfo.normalizedTime >= SwitchToIdleNormalizedTime)
            {
                SwitchToIdle(animator);
            }
        }
    }

    private void SwitchToIdle(Animator animator)
    {
        if (_isSwitchIdle) return;
        _isSwitchIdle = true;

        float wallElapsed = Time.time - _startTime;
        Debug.Log(
            $"[POWER_STRIKE_TIMING] ANIM_END_IDLE now={Time.time:F3}s motion='{motionName}' " +
            $"wallLived={wallElapsed:F3}s serverHitSec={_serverHitTime:F3}s clipLen={_clipLength:F3}s");

        animator.speed = 1f; // Меняем системную скорость напрямую
        PlayerEntity.Instance.IsAttack = false;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float wallElapsed = Time.time - _startTime;
        Debug.Log(
            $"[POWER_STRIKE_TIMING] ANIM_EXIT now={Time.time:F3}s motion='{motionName}' " +
            $"wallLived={wallElapsed:F3}s norm={stateInfo.normalizedTime:F3} speed={animator.speed:F3}");
        animator.speed = 1f; // Меняем системную скорость напрямую
    }
}
