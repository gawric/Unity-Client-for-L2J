using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AnimationEventsBase : MonoBehaviour
{
    public event Action<string> OnAnimationFinished;
    public event Action<string> OnAnimationStartShoot;
    public event Action<string> OnAnimationStartHit;
    public event Action<string> OnAnimationFinishedHit;
    public event Action<string> OnAnimationStartLoadArrow;


    protected Dictionary<string, bool> _priorityAnimations = new Dictionary<string, bool>();
    protected Queue<string> _animationQueue = new Queue<string>();
    protected bool _isProcessingQueue = false;

    // Клипы иногда содержат несколько одинаковых Animation Events на одном времени —
    // на длинных MagicShot один кадр вызывал OnAnimationComplete 10+, ломая await/переходы.
    private int _lastDuplicateCompleteFrame = int.MinValue;
    private string _lastDuplicateCompleteAnim;

    /// <summary>
    /// Два CastEnd/CastMid подряд с разницей в несколько сотен ms (blend/два слоя/оверрайд) —
    /// второй всё равно вызывает OnAnimationFinished и HandleQueueAnimation → обрыв MagicShot.
    /// </summary>
    private readonly Dictionary<string, float> _lastPriorityCompleteRealtime = new Dictionary<string, float>();
    private const float PriorityCompleteDebounceSeconds = 0.35f;


    public void InitializePriority()
    {
        _priorityAnimations = new Dictionary<string, bool>
        {
            //magic atk
            { "MagicShot", false },
            { "CastMid", false },
            { "CastEnd", false },
             //magic end
            { "jatk01_bow", false },
            { "jatk02_bow", false },
            { "jatk03_bow", false },

            { "jatk01_dual", false },
            { "jatk02_dual", false },
            { "jatk03_dual", false },

            { "jatk01_2HS", false },
            { "jatk02_2HS", false },
            { "jatk03_2HS", false },

            { "jatk01_1HS", false },
            { "jatk02_1HS", false },
            { "jatk03_1HS", false },

            { "jatk01_pole", false },
            { "jatk02_pole", false },
            { "jatk03_pole", false },

            { "SpAtk01_1HS", false },
            { "SpAtk01_2HS", false },
        };
    }
    public void OnAnimationComplete(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);

        if (_priorityAnimations.ContainsKey(animationName))
        {
            if (Time.frameCount == _lastDuplicateCompleteFrame &&
                string.Equals(_lastDuplicateCompleteAnim, animationName, StringComparison.Ordinal))
            {
                return;
            }

            float nowRt = Time.time;
            if (_lastPriorityCompleteRealtime.TryGetValue(animationName, out float prevRt) &&
                nowRt - prevRt < PriorityCompleteDebounceSeconds)
            {
                return;
            }

            _lastDuplicateCompleteFrame = Time.frameCount;
            _lastDuplicateCompleteAnim = animationName;
            _lastPriorityCompleteRealtime[animationName] = nowRt;

            _priorityAnimations[animationName] = false;
            _isProcessingQueue = false;
            OnAnimationFinished?.Invoke(animationName);

            if (_animationQueue.Count > 0)
            {
                var lastAnimation = _animationQueue.Last();

                foreach(string animName in _animationQueue)
                {
                    Debug.Log($"AnimationManager> start name убираем в листе ожиданий iteration animName " +animName+ " _animationQueue  " + _animationQueue.Count);
                }
                

                HandleQueueAnimation(lastAnimation);
                _animationQueue.Clear();
            }

        }

    }


    protected virtual void HandleQueueAnimation(string animationName)
    {
        // Base implementation does nothing, derived class will implement it
    }
    /// <summary>
    /// Вызывается из Unity Animation Event на клипе. Базовая реализация только диспатчит подписчикам;
    /// у игрока/монстров переопределено в <see cref="BaseAnimationController"/> (лог + дедуп дублей).
    /// </summary>
    public virtual void OnAnimationShoot(string animationName)
    {
        animationName = NormalizeAnimationEventName(animationName);
        OnAnimationStartShoot?.Invoke(animationName);
    }

    private static string NormalizeAnimationEventName(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            return animationName;
        }

        // MagicNoTarget clips are played through the shared MagicShot animator state.
        // Some imported clips also carry the truncated event payload "MagicNoTarge".
        if (animationName == "MagicNoTarget" ||
            animationName == "MagicNoTarge" ||
            animationName == "MagicNotarget")
        {
            return "MagicShot";
        }

        return animationName;
    }

 

    public void OnAnimationHit(string animationName)
    {
        OnAnimationStartHit?.Invoke(animationName);
    }

    public void OnAnimationAttackHitEnd(string animationName)
    {
        OnAnimationFinishedHit?.Invoke(animationName);
    }

    public void OnAnimationLoadArrow(string animationName)
    {
        OnAnimationStartLoadArrow?.Invoke(animationName);
    }
}