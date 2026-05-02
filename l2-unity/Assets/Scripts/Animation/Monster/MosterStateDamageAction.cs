using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateDamageACtion : MonsterStateBase
{

    private float _startTime = -1;
    private float _endTime = -1;
    private float _remainingTime = 0f;
    private AnimationCurve _speedCurve;
    private float _animationLength;

    // Настройки кривой скорости
    [SerializeField] private float fastPhaseSpeed = 1.5f;    // Скорость быстрой фазы (подъём)
    [SerializeField] private float slowPhaseSpeed = 0.5f;     // Скорость медленной фазы (опускание)
    [SerializeField] private float transitionPoint = 0.5f;    // Точка перехода между фазами (0-1)

    public string parameterName;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);


        // Инициализация переменных
        _startTime = Time.time;
        _animationLength = stateInfo.length;
        _endTime = _animationLength;
        _remainingTime = _endTime;

        // Создание кривой скорости
        CreateSpeedCurve();

        // Применение начальной скорости
        SetAnimationSpeed(animator, fastPhaseSpeed);

        StopAnimationTrigger(animator, parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = Time.time;
        float timeOut = currentTime - _startTime;

        // Обновление оставшегося времени
        _remainingTime = Mathf.Max(0, _endTime - timeOut);

        // Расчет нормализованного времени (0-1)
        float normalizedTime = timeOut / _endTime;

        // Получение скорости из кривой
        float currentSpeed = _speedCurve.Evaluate(normalizedTime);

        // Применение скорости к анимации
        SetAnimationSpeed(animator, currentSpeed);
        //Debug.Log($"Current speed {currentSpeed} timeanimation {_remainingTime} ");
        // Проверка завершения состояния
        if (timeOut >= _endTime)
        {
            SwitchToIdle(animator, stateInfo);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Сброс скорости анимации
        SetAnimationSpeed(animator, 1f);
    }

    private void CreateSpeedCurve()
    {
        _speedCurve = new AnimationCurve();

        // Создание ключей кривой
        Keyframe startKey = new Keyframe(0f, fastPhaseSpeed);
        Keyframe transitionKey = new Keyframe(transitionPoint, fastPhaseSpeed);
        Keyframe endKey = new Keyframe(1f, slowPhaseSpeed);

        // Добавление ключей в кривую
        _speedCurve.AddKey(startKey);
        _speedCurve.AddKey(transitionKey);
        _speedCurve.AddKey(endKey);

        // Настройка обертки и сглаживания
        _speedCurve.preWrapMode = WrapMode.ClampForever;
        _speedCurve.postWrapMode = WrapMode.ClampForever;

        // Сглаживание кривой
        _speedCurve.SmoothTangents(0, 0.2f);
        _speedCurve.SmoothTangents(1, 0.2f);
        _speedCurve.SmoothTangents(2, 0.2f);
    }

    private void SetAnimationSpeed(Animator animator, float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    private void SwitchToIdle(Animator animator, AnimatorStateInfo stateInfo)
    {
        // Проверка, что анимация почти завершена
        if (stateInfo.normalizedTime > 0.9f)
        {
            // Возврат к скорости 1f при выходе из состояния
            SetAnimationSpeed(animator, 1f);

            // Здесь можно добавить логику для переключения в состояние ожидания
            // PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        }
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopMonsterCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
        }

    }
}
