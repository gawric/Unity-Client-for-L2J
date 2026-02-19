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
        if (_priorityAnimations.ContainsKey(animationName))
        {

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
    public void OnAnimationShoot(string animationName)
    {
        OnAnimationStartShoot?.Invoke(animationName);
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