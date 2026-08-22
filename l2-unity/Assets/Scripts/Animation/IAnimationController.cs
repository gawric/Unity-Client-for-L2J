using System;
using UnityEngine;

public interface IAnimationController
{
    public void SetBool(string name, bool value, string entityName = "");

    public string GetAnimatorName();
    public void SetInt(string name, int value);
    public void ToggleAnimationTrigger(string name);
    public void ResetAnimationTrigger(string name);
    public void ToggleAnimationCrossFade(string name, float duration = 0.3f);
    public void CrossFadeInFixedTime(string stateName, float fixedDuration, int layer = 0);
    /// <summary>Raw Animator for locomotion helpers (<see cref="PlayerLocomotionCrossFade"/>).</summary>
    public Animator GetAnimator();

    public float GetEventTimeByName(AnimationClip clip, string eventName);
    public void ReplaceAnimClip(string animName, string overrideAnimName);
    public bool GetBool(string name);
    public int GetInt(string name);
    public void SetAnimatorSpeed(float value);
    void SetEnabled(bool value);
    /// <summary>Animator float <c>patkspd</c> — clip rate so the swing fits server timeAtk.</summary>
    public void SetPAtkSpeed(float newAtkSpd);
  
    event Action<string> OnAnimationFinished;
    event Action<string> OnAnimationStartShoot;
    event Action<string> OnAnimationStartHit;
    event Action<string> OnAnimationFinishedHit;
    event Action<string> OnAnimationStartLoadArrow;
}
