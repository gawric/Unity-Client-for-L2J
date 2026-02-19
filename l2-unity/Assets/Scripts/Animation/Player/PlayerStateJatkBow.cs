using UnityEngine;

public class PlayerStateJatkBow : StateMachineBehaviour
{
    private float _startTime;
    private float _endTime;
    private float _clipLength;
    private float _eventTimeInClip; // Время ивента в самом файле (3.22с)
    private float _targetShootTime; // Когда ивент ДОЛЖЕН сработать в реале (2.3с)
    private bool _isSwitchIdle;

    public string parameterName;
    public string motionName;
    public string eventShootName;
    private AnimationCurve _animationCurve;



    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _startTime = Time.time;
        _animationCurve = CreateNormalizedCurve();

        // 1. Получаем данные клипа и ивента из кэша
        _clipLength = AnimationDataCache.GetOverrideLength(animator, motionName);
        _eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, motionName, eventShootName);

        // 2. Получаем серверное время (3.3с)
        float serverTimeMs = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);
        _endTime = serverTimeMs / 1000f;

        // 3. Рассчитываем время полета и когда ДОЛЖЕН быть выстрел
        float dist = PlayerEntity.Instance.TargetDistance();
        float[] times = CalcBaseParam.CalculateAttackAndFlightTimes(dist, serverTimeMs);
        float flightTime = times[1] / 1000f;

        // Точка выстрела в реальном времени (например: 3.3 - 1.0 = 2.3с)
        _targetShootTime = _endTime - flightTime;

        // Предохранитель, чтобы не было деления на ноль
        if (_targetShootTime <= 0) _targetShootTime = _endTime * 0.5f;

        StopAnimationTrigger(animator, parameterName);

        Debug.Log($"[Sync] Клип: {_clipLength}с | Ивент в клипе: {_eventTimeInClip}с | Цель выстрела: {_targetShootTime}с | Цель итог: {_endTime}с");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float timeOut = Time.time - _startTime;

        if (timeOut >= _endTime)
        {
            PlayerAnimationController.Instance.SetPAtkSpeed(1.0f);
            SwitchToIdle(animator);
            return;
        }

        float progress = Mathf.Clamp01(timeOut / _endTime);

        // Расчет базовой скорости для синхронизации
        float baseSpeed;

        if (timeOut < _targetShootTime)
        {
            // ФАЗА 1: До выстрела. Должны пройти путь _eventTimeInClip за время _targetShootTime
            baseSpeed = _eventTimeInClip / _targetShootTime;
        }
        else
        {
            // ФАЗА 2: После выстрела. Должны пройти остаток клипа за остаток времени
            float remainingClip = _clipLength - _eventTimeInClip;
            float remainingTime = _endTime - _targetShootTime;
            baseSpeed = (remainingTime > 0) ? (remainingClip / remainingTime) : 1.0f;
        }

        // Модификатор кривой (slope)
        float delta = 0.01f;
        float v1 = _animationCurve.Evaluate(progress);
        float v2 = _animationCurve.Evaluate(Mathf.Min(progress + delta, 1.0f));
        float slope = (v2 - v1) / delta;

        float targetSpeed = baseSpeed * slope;

        PlayerAnimationController.Instance.SetPAtkSpeed(targetSpeed);

        // Дебаг прогресса выстрела
        if (timeOut < _targetShootTime)
            Debug.Log($"[Sync] До выстрела: {(timeOut / _targetShootTime) * 100:F0}% | Скорость: {targetSpeed:F2}");
    }

    private AnimationCurve CreateNormalizedCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0f));
        curve.AddKey(new Keyframe(1f, 1f)); // Для двухфазной скорости лучше использовать линейную или мягкую кривую
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0);
        return curve;
    }

    private void SwitchToIdle(Animator animator)
    {
        if (_isSwitchIdle) return;
        _isSwitchIdle = true;
        PlayerEntity.Instance.IsAttack = false;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName, "player");
        }

    }

    private bool IsBow(string animName) => animName.IndexOf("bow") != -1;

    private float GetTimeAtk(string animName)
    {
        if (IsBow(animName))
        {
            //return CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);
            float baseAttackTime = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed);
            float targetDistance = PlayerEntity.Instance.TargetDistance();
            float[] timeAndFlye = CalcBaseParam.CalculateAttackAndFlightTimes(targetDistance, baseAttackTime);


            return timeAndFlye[0];
            // return CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed) / 2;
        }
        return CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed) / 2;
    }
}