using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using static Org.BouncyCastle.Crypto.Digests.SkeinEngine;
using static UnityEngine.Rendering.DebugUI;

public class PlayerAnimationController : BaseAnimationController
{
    private static PlayerAnimationController _instance;
    public static PlayerAnimationController Instance { get { return _instance; } }

    public override void Initialize()
    {
        base.Initialize();

        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }

    public void StartTrigger(string name)
    {
        ToggleAnimationTrigger(name);
    }
    public void StartCrossFade(string name , float duration)
    {
        ToggleAnimationCrossFade(name, duration);
    }
    public void SetBool(string name, bool value, bool share)
    {
        if (_animator.GetBool(name) != value)
        {
            Debug.LogWarning($"Player Animatorr Cntoller: Set bool {name}={value}");

            SetBool(name, value);
        }
    }

    public Dictionary<string, float> GetParametrs()
    {
        Dictionary<string, float> floatValues = new Dictionary<string, float>();


        foreach (var parameter in _animator.parameters)
        {

            if (parameter.type == AnimatorControllerParameterType.Float)
            {
                floatValues[parameter.name] = _animator.GetFloat(parameter.name);
            }
        }
        return floatValues;
    }

    public void SetParametrs(Dictionary<string, float> floatValues)
    {

        foreach (var parameter in _animator.parameters)
        {

            if (parameter.type == AnimatorControllerParameterType.Float)
            {

                if (floatValues.TryGetValue(parameter.name, out float value))
                {

                    _animator.SetFloat(parameter.name, value);

                }

            }
        }
    }



}

