using System;
using System.Collections.Generic;
using UnityEngine;

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
    private CastClipPlayable _castPlayable;
    private bool _castIdleSkip;

    // Дубли OnAnimationShoot на одном кадре (два ивента на клипе / два слоя) — в подписчики уходит один раз.
    private int _lastDuplicateShootFrame = int.MinValue;
    private string _lastDuplicateShootAnim;

    protected bool TryGetAnimator()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);
        return _animator != null;
    }

    public virtual void Initialize()
    {
        _lastAnimationVariableName = "wait_hand";
        if (!TryGetAnimator() || _animator.runtimeAnimatorController == null)
            return;
        SkillAnimationDatabase.LoadRaceAnimations(_animator.runtimeAnimatorController.name);

        // Re-entrant: pooled NPC/monster call Initialize again on each spawn.
        _base_motion.Clear();
        _base_motion["CastMid"] = BASE_MOTION_CAST_MID;
        _base_motion["CastEnd"] = BASE_MOTION_END;
        _base_motion["MagicShoot"] = BASE_MOTION_MAGIC_SHOOT;

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

        // Leave death/corpse pose behind when reusing from ObjectPool.
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }

        _castIdleSkip = GetComponent<UserEntity>() != null;
        if (_castIdleSkip)
        {
            _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            _castPlayable?.Dispose();
            _castPlayable = null;
            return;
        }

        _castPlayable ??= new CastClipPlayable();
        _castPlayable.Bind(this, _animator);
    }

    void EnsureCastPlayable()
    {
        if (!_castIdleSkip || _castPlayable != null)
            return;
        if (!TryGetAnimator())
            return;
        _castPlayable = new CastClipPlayable();
        _castPlayable.Bind(this, _animator);
    }

    void Update()
    {
        if (_castPlayable == null || !_castPlayable.IsActive)
            return;
        _castPlayable.Tick();
    }

    protected void OnDestroy()
    {
        _castPlayable?.Dispose();
        _castPlayable = null;
    }

    /// <summary>
    /// Play this race's cast clip without writing the override slot.
    /// Returns false when the race clip is missing, so the caller keeps the animator state.
    /// </summary>
    public bool TryPlayCastPhase(string overrideAnimName, string phaseTriggerName)
    {
        EnsureCastPlayable();
        if (_castPlayable == null)
        {
            return false;
        }

        return _castPlayable.TryPlay(overrideAnimName, phaseTriggerName);
    }

    public void StopCastClip()
    {
        if (_castPlayable != null && _castPlayable.IsActive)
        {
            Entity e = EntityActionSkill.ResolveEntity(
                _animator != null ? _animator.GetInteger(AnimatorUtils.OBJECT_ID) : 0);
            MagicGaitTrace.Event(e, "StopCastClip", "wasActive=true");
        }
        _castPlayable?.Stop();
    }

    public bool IsCastClipActive => _castPlayable != null && _castPlayable.IsActive;

    public bool TryPlayLongHold(string endOverrideName, string shotOverrideName)
    {
        EnsureCastPlayable();
        if (_castPlayable == null)
        {
            return false;
        }

        return _castPlayable.TryPlayLongHold(endOverrideName, shotOverrideName);
    }

  

  

    public void SetRunSpeed(float value)
    {
        if (!TryGetAnimator())
            return;
        // Debug.Log("SetRun speed test " + value);
        DriveFloat("run_speed", value);
    }

    public void SetWalkSpeed(float value)
    {
        if (!TryGetAnimator())
            return;
        // Debug.Log("SetWalk speed test " + value);
        DriveFloat("walk_speed", value);
    }

    public void SetWalkSpeedLobby(float value)
    {
        if (!TryGetAnimator())
            return;
        DriveFloat("walk_speed", value);
    }


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
        if (!TryGetAnimator())
            return;
        float newAtkSpd = clipLength * 1000f / _pAtkSpd;
        //Debug.Log("PATACK speed set " + newAtkSpd);
        DriveFloat("patkspd", newAtkSpd);
    }

    public void UpdateAnimatorAtkSpeedL2j(float timeAtk , float clipLength)
    {
        if (!TryGetAnimator())
            return;
        float newAtkSpd = clipLength * 1000f / timeAtk;
        DriveFloat("patkspd", newAtkSpd);
    }

    public void SetPAtkSpeed(float newAtkSpd)
    {
        if (!TryGetAnimator())
            return;
        //Debug.Log("PATACK speed set 2 " + newAtkSpd);
        DriveFloat("patkspd", newAtkSpd);
    }


    public void SetMAtkSpd(float value)
    {
        if (!TryGetAnimator())
            return;
        //TODO: update for cast animation
        float newMAtkSpd = _spAtk01ClipLength / value;
        DriveFloat("matkspd", newMAtkSpd);
    }

    public void SetCastSpeed(float value)
    {
        if (!TryGetAnimator())
            return;
        DriveFloat("cast_speed", value);
    }
    public float GetNormalizedTimeOffsetSpeed()
    {
        if (!TryGetAnimator())
            return 0f;
        float normalized =  GetStateInfo().normalizedTime;
        float speed = _animator.speed;
        return  normalized / speed;
    }
    public void SetShotSpeed(float value)
    {
        if (!TryGetAnimator())
            return;
        DriveFloat("shot_speed", value);
    }

    // Set all animation variables to false
    public void ClearAnimParams()
    {
        if (!TryGetAnimator())
            return;
        for (int i = 0; i < _animator.parameters.Length; i++)
        {
            AnimatorControllerParameter anim = _animator.parameters[i];
            if (anim.type == AnimatorControllerParameterType.Bool)
            {
                DriveBool(anim.name, false);
            }
        }
    }


    public void ToggleAnimationTrigger(string name)
    {
        if (!TryGetAnimator())
            return;
        DriveTrigger(name);
    }

    public void ResetAnimationTrigger(string name)
    {
        if (!TryGetAnimator())
            return;
        DriveResetTrigger(name);
    }

    public void ToggleAnimationCrossFade(string name , float duration)
    {
        if (!TryGetAnimator())
            return;
        DriveCrossFade(name, duration, 0);
    }

    public void CrossFadeInFixedTime(string stateName, float fixedDuration, int layer = 0)
    {
        if (!TryGetAnimator() || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int hash = Animator.StringToHash(stateName);
        DriveCrossFadeInFixedTime(stateName, hash, fixedDuration, layer, 0f);
        _lastAnimationVariableName = stateName;
        // Debug.Log(
            // $"[ANIM_CROSSFADE] id={objectId} go={name} state={stateName} hasState={hasState} fixedDuration={fixedDuration:F3}s layer={layer}");
    }

    public void CrossFadeRestartInFixedTime(string stateName, float fixedDuration, int layer = 0)
    {
        if (!TryGetAnimator() || string.IsNullOrEmpty(stateName))
            return;

        // fixedTimeOffset is seconds from the start of the destination clip, not normalized time.
        DriveCrossFadeInFixedTime(stateName, Animator.StringToHash(stateName), fixedDuration, layer, 0f);
        _lastAnimationVariableName = stateName;
        // Debug.Log(
            // $"[ANIM_RESTART] id={objectId} go={name} state={stateName} fixedDuration={fixedDuration:F3}s " +
            // $"normBefore={normBefore:F3} inTransition={inTransition} layer={layer}");
    }

    public Animator GetAnimator() => _animator;

    // The cast graph owns the mesh. These writes update that controller playable
    // in place; they do not create a graph.
    void DriveFloat(string paramName, float value)
    {
        _animator.SetFloat(paramName, value);
        _castPlayable?.SetFloat(paramName, value);
    }

    void DriveBool(string paramName, bool value)
    {
        _animator.SetBool(paramName, value);
        _castPlayable?.SetBool(paramName, value);
    }

    void DriveInt(string paramName, int value)
    {
        _animator.SetInteger(paramName, value);
        _castPlayable?.SetInteger(paramName, value);
    }

    void DriveTrigger(string paramName)
    {
        _animator.SetTrigger(paramName);
        _castPlayable?.SetTrigger(paramName);
    }

    void DriveResetTrigger(string paramName)
    {
        _animator.ResetTrigger(paramName);
        _castPlayable?.ResetTrigger(paramName);
    }

    void DriveCrossFade(string stateName, float duration, int layer)
    {
        _animator.CrossFade(stateName, duration, layer);
        _castPlayable?.CrossFade(Animator.StringToHash(stateName), duration, layer);
    }

    void DriveCrossFadeInFixedTime(string stateName, int stateHash, float fixedDuration, int layer, float fixedTimeOffset)
    {
        _animator.CrossFadeInFixedTime(stateName, fixedDuration, layer, fixedTimeOffset);
        _castPlayable?.CrossFadeInFixedTime(stateHash, fixedDuration, layer, fixedTimeOffset);
    }

    public void SetBool(string name, bool value , string entityName = "")
    {
        if (!TryGetAnimator())
            return;

        if (value)
        {
            _lastAnimationVariableName = name;
        }

        DriveBool(name, value);
    }

    public void SetBoolDisabledOther(string nameAnim  , bool value, string[] disableds)
    {
        if (!TryGetAnimator())
            return;
        DisabledOtherAnim(disableds);
        DriveBool(nameAnim, value);
    }

    private void DisabledOtherAnim(string[] disableds)
    {
        if (!TryGetAnimator())
            return;
        foreach (string name in disableds)
        {
            DriveBool(name, false);
        }
    }


    public bool GetBool(string name)
    {
        if (!TryGetAnimator())
            return false;
        return _animator.GetBool(name);
    }



    public bool IsFinishAnimation(string name)
    {
        if (!TryGetAnimator())
            return false;

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
        if (!TryGetAnimator())
            return default;
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
        if (!TryGetAnimator())
            return;
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
                    DriveFloat(anim.name, value);
                    break;
                case AnimatorControllerParameterType.Int:
                    DriveInt(anim.name, (int)value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    SetBool(anim.name, value == 1f);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    DriveTrigger(anim.name);
                    break;
            }
        }
    }

    // Return an animator variable based on its ID
    public float GetAnimationProperty(int animId)
    {
        if (!TryGetAnimator())
            return 0f;
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
        if (!TryGetAnimator())
            return;
        DriveFloat(name, value);
    }

    public float GetFloat(string name)
    {
        if (!TryGetAnimator())
            return 0f;
        return  _animator.GetFloat(_animator.name);
    }

    public void SetInt(string name, int value)
    {
        if (!TryGetAnimator())
            return;
        DriveInt(name, value);
    }

    public int GetInt(string name)
    {
        if (!TryGetAnimator())
            return 0;
        return _animator.GetInteger(_animator.name);
    }

    public void SetAnimatorSpeed(float value)
    {
        if (!TryGetAnimator())
            return;
        _animator.speed = value;
        _castPlayable?.SetControllerSpeed(value);
    }

    public void SetEnabled(bool value)
    {
        enabled = value;
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
            // Debug.Log("BaseAnimationController>ReplaceAnimClip clip orig   " + animName + " replace name " + clip.name);
        }
        else
        {
            // Debug.LogWarning("BaseAnimationController>ReplaceAnimClip not found replace anim  " + animName);
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

    public override void OnAnimationShoot(string animationName)
    {
        if (!TryGetAnimator())
        {
            base.OnAnimationShoot(animationName);
            return;
        }

        int objectId = _animator.GetInteger(AnimatorUtils.OBJECT_ID);

        if (Time.frameCount == _lastDuplicateShootFrame &&
            string.Equals(_lastDuplicateShootAnim, animationName, StringComparison.Ordinal))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AnimationManager.LogAnimationShootFromAnimator(
                objectId,
                "SKIP_DEDUP_SAME_FRAME",
                animationName,
                BuildAnimatorShootDebugContext());
#endif
            return;
        }

        _lastDuplicateShootFrame = Time.frameCount;
        _lastDuplicateShootAnim = animationName;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AnimationManager.LogAnimationShootFromAnimator(
            objectId,
            "DISPATCH",
            animationName,
            BuildAnimatorShootDebugContext());
#endif

        base.OnAnimationShoot(animationName);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string BuildAnimatorShootDebugContext()
    {
        AnimatorStateInfo s0 = _animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        string clip0 = clipInfos.Length > 0 && clipInfos[0].clip != null ? clipInfos[0].clip.name : "(no clip info)";
        float clipLen = clipInfos.Length > 0 && clipInfos[0].clip != null ? clipInfos[0].clip.length : -1f;

        int layerCount = _animator.layerCount;
        string layer1 = "";
        if (layerCount > 1)
        {
            AnimatorStateInfo s1 = _animator.GetCurrentAnimatorStateInfo(1);
            AnimatorClipInfo[] c1 = _animator.GetCurrentAnimatorClipInfo(1);
            string n1 = c1.Length > 0 && c1[0].clip != null ? c1[0].clip.name : "?";
            layer1 = $" | L1 clip={n1} normT={s1.normalizedTime:F3}";
        }

        return
            $"controller='{GetAnimatorName()}' L0 clip={clip0} clipLen={clipLen:F3}s " +
            $"normT={s0.normalizedTime:F3} speed={_animator.speed:F3} shortNameHash={s0.shortNameHash}" +
            layer1;
    }
#endif
}
