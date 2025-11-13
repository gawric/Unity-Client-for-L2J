using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BaseAnimationController : MonoBehaviour
{
    [SerializeField] protected Animator _animator;
    [SerializeField] protected bool _resetStateOnReceive = false;
    [SerializeField] protected float _spAtk01ClipLength = 1000;
    [SerializeField] protected Dictionary<string, float> _atkClipLengths;
    private string _lastAnimationVariableName;
    private float _lastAtkClipLength;
    private float _pAtkSpd;

    public virtual void Initialize()
    {
        _animator = gameObject.GetComponentInChildren<Animator>(true);
        _lastAnimationVariableName = "wait_hand";
        
    }

    public void SetRunSpeed(float value)
    {
        _animator.SetFloat("run_speed", value);
    }

    public void SetWalkSpeed(float value)
    {
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
        //Debug.Log("PATACK speed set " + newAtkSpd);
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

    public void WeaponAnimChanged(string newWeaponAnim)
    {
        ClearAnimParams();

        if (!_lastAnimationVariableName.Contains("_"))
        {
            Debug.LogWarning($"The last animation was not a weapon animation: {_lastAnimationVariableName}");
            // The last animation was not a weapon animation
            return;
        }

        string[] parts = _lastAnimationVariableName.Split("_");
        if (parts.Length < 1)
        {
            // Should not happen
            Debug.LogWarning($"Error while parsing previous animation name: {_lastAnimationVariableName}");
            return;
        }

        string newAnimation = parts[0] + "_" + newWeaponAnim;
        Debug.Log($"New Weapon animation name: {newAnimation}");
        SetBool(newAnimation, true);
    }


    public void SetBool(string name, bool value)
    {
        //Debug.Log($"Set bool loooooooooooogggggggggg +++++++++++++ {name}={value}");

        // Save the last animation name
        if (value == true)
        {
            _lastAnimationVariableName = name;
        }
        
        _animator.SetBool(name, value);

    }

    public void Rebind()
    {
        _animator.Rebind();
    }

    public void debugPrint()
    {
        AnimatorControllerParameter[] parameters = _animator.parameters;

        // Проходим по всем параметрам и выводим только bool переменные
        foreach (var parameter in parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                bool value = _animator.GetBool(parameter.name);
                Debug.Log($"Параметр: {parameter.name}, Значение: {value}");
            }
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

    public void Test()
    {
        //bool t1 = _animator.GetBool("wait");
        //bool t2 = _animator.GetBool("atk01");
        //bool t3 = _animator.GetBool("run");
        Debug.Log("");
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

}
