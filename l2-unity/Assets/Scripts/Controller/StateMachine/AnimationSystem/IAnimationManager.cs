using System;
using System.Collections.Generic;
using UnityEngine;

public interface IAnimationManager
{
    void SetAnimationManager(PlayerAnimationController controller , PlayerEntity Player);
    void PlayAnimation(string animationName , bool disableTriggerAfterStart);
    void PlayOriginalAnimation(string animationName);
    string GetCurrentAnimationName();
    string GetLastAnimationName();
    void StopCurrentAnimation(string paramName , string runName = "");
    void PlayMonsterAnimation(int objId, NetworkAnimationController controllerAnimator, string animationName);
    void StopMonsterCurrentAnimation(Animator animator, string animationName);
    Dictionary<string, float> PlayerGetAllFloat();
    void PlayerSetAllFloat(Dictionary<string, float> floatValues);
    void UpdateRemainingAtkTime(float remainingAtkTime);
    float GetRemainingAtkTime();

    public event Action<string> OnAnimationFinished;

    public event Action<string , float> OnAnimationStartShoot;
    public event Action<string> OnAnimationLoadArrow;
   
}
