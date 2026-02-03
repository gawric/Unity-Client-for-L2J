using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BaseAnimationController : AnimationEventsBase, IAnimationController
{
    [SerializeField] protected Animator _animator;
    [SerializeField] protected bool _resetStateOnReceive = false;
    [SerializeField] protected float _spAtk01ClipLength = 1000;
    [SerializeField] protected Dictionary<string, float> _atkClipLengths;
    private string _lastAnimationVariableName;
    private float _lastAtkClipLength;
    private float _pAtkSpd;

    private readonly Dictionary<string, string> _base_motion = new Dictionary<string, string>(3);

    private const string BASE_MOTION_CAST_MID = "FDarkElf_m001_b.ao_CastMid_FDarkElf";
    private const string BASE_MOTION_END = "FDarkElf_m001_b.ao_CastEnd_FDarkElf";
    private const string BASE_MOTION_MAGIC_SHOOT = "FDarkElf_m001_b.ao_MagicShot_A_FDarkElf";

    private AnimatorOverrideController _overrideController;

    public virtual void Initialize()
    {
        _animator = gameObject.GetComponentInChildren<Animator>(true);
        _lastAnimationVariableName = "wait_hand";
        InitializePriority();
        SkillAnimationDatabase.LoadRaceAnimations(_animator?.runtimeAnimatorController.name);

        _base_motion.Add("CastMid" , BASE_MOTION_CAST_MID);
        _base_motion.Add("CastEnd", BASE_MOTION_END);
        _base_motion.Add("MagicShoot", BASE_MOTION_MAGIC_SHOOT);

        // Создаем экземпляр оверрайда на основе текущего контроллера
        if (_animator.runtimeAnimatorController is not AnimatorOverrideController)
        {
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
        }
        else
        {
            _overrideController = (AnimatorOverrideController)_animator.runtimeAnimatorController;
        }

    }

  

    protected override void HandleQueueAnimation(string animationName)
    {
        Debug.Log($"AnimationManager> start name убираем из ожидания и запускаем {animationName}");
        SetBool(animationName, true, "player");
    }

    public void SetRunSpeed(float value)
    {
        Debug.Log("SetRun speed test " + value);
        _animator.SetFloat("run_speed", value);
    }

    public void SetWalkSpeed(float value)
    {
        Debug.Log("SetWalk speed test " + value);
        _animator.SetFloat("walk_speed", value);
    }

    public void SetWalkSpeedLobby(float value) => _animator.SetFloat("walk_speed", value);


    public void SetPAtkSpd(float value)
    {
        //Debug.Log("Update Patak speed set " + value);
        _pAtkSpd = value;
        if (_lastAtkClipLength != 0)
        {
            UpdateAnimatorAtkSpdMultiplier(_lastAtkClipLength);
        }
    }

    public void UpdateAnimatorAtkSpdMultiplier(float clipLength)
    {
        float newAtkSpd = clipLength * 1000f / _pAtkSpd;
        //Debug.Log("PATACK speed set " + newAtkSpd);
        _animator.SetFloat("patkspd", newAtkSpd);
    }

    public void UpdateAnimatorAtkSpeedL2j(float timeAtk , float clipLength)
    {
        float newAtkSpd = clipLength * 1000f / timeAtk;
        _animator.SetFloat("patkspd", newAtkSpd);
    }

    public void SetPAtkSpeed(float newAtkSpd)
    {
        //Debug.Log("PATACK speed set 2 " + newAtkSpd);
        _animator.SetFloat("patkspd", newAtkSpd);
    }


    public void SetMAtkSpd(float value)
    {
        //TODO: update for cast animation
        float newMAtkSpd = _spAtk01ClipLength / value;
        _animator.SetFloat("matkspd", newMAtkSpd);
    }

    public void SetCastSpeed(float value)
    {
        _animator.SetFloat("cast_speed", value);
    }
    public float GetNormalizedTimeOffsetSpeed()
    {
        float normalized =  GetStateInfo().normalizedTime;
        float speed = _animator.speed;
        return  normalized / speed;
    }
    public void SetShotSpeed(float value)
    {
        _animator.SetFloat("shot_speed", value);
    }

    // Set all animation variables to false
    public void ClearAnimParams()
    {
        for (int i = 0; i < _animator.parameters.Length; i++)
        {
            AnimatorControllerParameter anim = _animator.parameters[i];
            if (anim.type == AnimatorControllerParameterType.Bool)
            {
                _animator.SetBool(anim.name, false);
            }
        }
    }


    public void ToggleAnimationTrigger(string name)
    {
        IfSpecialAnimationsCreateProcessQueue(name, ref _isProcessingQueue, _priorityAnimations, true);
        _animator.SetTrigger(name);
    }

    public void ToggleAnimationCrossFade(string name , float duration)
    {
        IfSpecialAnimationsCreateProcessQueue(name, ref _isProcessingQueue, _priorityAnimations, true);
        _animator.CrossFade(name, duration, 0);
    }

    public void SetBool(string name, bool value , string entityName = "")
    {
        
        if(_isProcessingQueue && value == true)
        {
            IfAnimationNeedsWait( _priorityAnimations, name);

            if (value) return;
            Debug.Log($"AnimationManager> start name player  добавление в список ожидания {name} статус {value} продолжение return ");
        }


        IfSpecialAnimationsCreateProcessQueue(name , ref _isProcessingQueue, _priorityAnimations, value);

        // Save the last animation name
        if (value == true)
        {
            _lastAnimationVariableName = name;
        }
        
        _animator.SetBool(name, value);

        if (!string.IsNullOrEmpty(entityName))
        {
            Debug.Log($"AnimationManager> start name player  animation {name} and value {value}");
        }

    }

    private void IfSpecialAnimationsCreateProcessQueue(string animName , ref bool _isProcessingQueue , Dictionary<string, bool> _priorityAnimations , bool value)
    {
       
        if (_priorityAnimations.ContainsKey(animName) && _priorityAnimations[animName] == false && value == true)
        {
            _priorityAnimations[animName] = true;
            _isProcessingQueue = true;
        }
    }

    private void IfAnimationNeedsWait(Dictionary<string, bool> _priorityAnimations , string animName)
    {
        if (!_priorityAnimations.ContainsKey(animName))
        {
            _animationQueue.Enqueue(animName);
            Debug.Log($"AnimationManager> start name player  добавление в список ожидания {animName} испольнение return ");
        }
    }




    public void SetBoolDisabledOther(string nameAnim  , bool value, string[] disableds)
    {
        DisabledOtherAnim(disableds);
        _animator.SetBool(nameAnim, value);
    }

    private void DisabledOtherAnim(string[] disableds)
    {
        foreach (string name in disableds)
        {
            _animator.SetBool(name, false);
        }
    }


    public bool GetBool(string name)
    {
        return _animator.GetBool(name);
    }



    public bool IsFinishAnimation(string name)
    {

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        bool isName = stateInfo.IsName(name);

        if (GetNormalizedTimeOffsetSpeed() >= 1 && stateInfo.IsName(name))
        {
            return true;

        }

        return false;
    }

  

    public AnimatorStateInfo GetStateInfo()
    {
        return  _animator.GetCurrentAnimatorStateInfo(0);
    }


    // Update animator variable based on Animation Id
    public void SetAnimationProperty(int animId, float value)
    {
        SetAnimationProperty(animId, value, false);
    }

    // Update animator variable based on Animation Id
    public void SetAnimationProperty(int animId, float value, bool forceReset)
    {
        //Debug.Log("animId " + animId + "/" + _animator.parameters.Length);
        if (animId >= 0 && animId < _animator.parameters.Length)
        {
            if (_resetStateOnReceive || forceReset)
            {
                ClearAnimParams();
            }

            AnimatorControllerParameter anim = _animator.parameters[animId];

            switch (anim.type)
            {
                case AnimatorControllerParameterType.Float:
                    _animator.SetFloat(anim.name, value);
                    break;
                case AnimatorControllerParameterType.Int:
                    _animator.SetInteger(anim.name, (int)value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    SetBool(anim.name, value == 1f);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    _animator.SetTrigger(anim.name);
                    break;
            }
        }
    }

    // Return an animator variable based on its ID
    public float GetAnimationProperty(int animId)
    {
        if (animId >= 0 && animId < _animator.parameters.Length)
        {
            AnimatorControllerParameter anim = _animator.parameters[animId];

            switch (anim.type)
            {
                case AnimatorControllerParameterType.Float:
                    return _animator.GetFloat(anim.name);
                case AnimatorControllerParameterType.Int:
                    return (int)_animator.GetFloat(anim.name);
                case AnimatorControllerParameterType.Bool:
                    return _animator.GetBool(anim.name) == true ? 1f : 0;
            }
        }

        return 0f;
    }

    public void SetFloat(string name, float value)
    {
        _animator.SetFloat(name, value);
    }

    public float GetFloat(string name)
    {
        return  _animator.GetFloat(_animator.name);
    }

    public void SetInt(string name, int value)
    {
        _animator.SetInteger(name, value);
    }

    public int GetInt(string name)
    {
        return _animator.GetInteger(_animator.name);
    }

    public string GetAnimatorName()
    {
       return _animator?.runtimeAnimatorController.name;
    }

    public void ReplaceAnimClip(string animName , string overrideAnimName)
    {
        string raceName = _animator?.runtimeAnimatorController.name;
        AnimationClip clip = SkillAnimationDatabase.GetOverrideClip(overrideAnimName , raceName);
        if (HasClip(animName))
        {
            _overrideController[animName] = clip;
            Debug.Log("BaseAnimationController>ReplaceAnimClip clip orig   " + animName + " replace name " + clip.name);
        }
        else
        {
            Debug.LogWarning("BaseAnimationController>ReplaceAnimClip not found replace anim  " + animName);
        }

    }

    public bool HasClip(string originalClipName)
    {
        // Проверяем, есть ли вообще такой "слот" для замены
        return _overrideController[originalClipName] != null;
    }

    public float GetEventTimeByName(AnimationClip clip, string eventName)
    {
        return AnimationDataCache.GetEventTimeByName(_animator, clip, eventName);
    }
}
