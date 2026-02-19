using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using UnityEngine;

public interface IAnimationManager
{
    public void RegisterController(int objectId, IAnimationController controller, Entity entity);

    void PlayAnimation(int objectId , string animationName , bool disableTriggerAfterStart);
    public void PlayAnimationTrigger(int objectId , string triggerName);

    Task AsyncPlayAnimationTrigger(int objectId, string animationName);
    Task AsyncPlayAnimationRaceOverrides(int objectId, string tiggerName , string overrideAnimationName);

    public float[] GetOverrideClipsDurations(int objectId, string[] cycle);
    public float GetOverrideEventTimeByName(int objectId, string[] cycle , string eventName);
    void PlayOriginalAnimation(int objectId , string animationName);
    string GetCurrentAnimationName(int objectId);
    string GetLastAnimationName(int objectId);
    void StopCurrentAnimation(int objectId , string paramName , string runName = "");
    void PlayMonsterAnimation(int objectId, string animationName);
    void StopMonsterCurrentAnimation(int objectId, string animationName);
    Dictionary<string, float> PlayerGetAllFloat(int objectId);
    void PlayerSetAllFloat(int objectId , Dictionary<string, float> floatValues);
    public AnimationEventsBase  GetAnimationEvents(int objectId);
    public void SetSpTimeAtk(int objectId , int timeAtk);


}
