using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using UnityEngine;

public interface IAnimationManager
{
    public void RegisterController(int objectId, IAnimationController controller, Entity entity);
    public void UnregisterController(int objectId);

    void PlayAnimation(int objectId , string animationName , bool disableTriggerAfterStart);
    public void PlayAnimationTrigger(int objectId , string triggerName);

    /// <returns>true = clip finished; false = superseded by a newer cast (do not IDLE/WAIT_RETURN).</returns>
    Task<bool> AsyncPlayAnimationTrigger(int objectId, string animationName);
    /// <returns>true = clip finished; false = superseded by a newer cast (do not IDLE/WAIT_RETURN).</returns>
    Task<bool> AsyncPlayAnimationRaceOverrides(int objectId, string tiggerName , string overrideAnimationName);
    Task AsyncPlayLongCastLoopPhase(int objectId, string triggerName, string overrideAnimationName);
    /// <returns>true = finish event received; false = superseded by a newer cast.</returns>
    Task<bool> AsyncAwaitOverrideAnimationFinish(int objectId, string expectedFinishName);

    /// <summary>
    /// SMB magic phase end — completes runner await without clip OnAnimationComplete.
    /// </summary>
    void NotifyMagicPhaseFinished(int objectId, string phaseName);

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
    public void ResetPlayerAnimatorSpeed(int objectId, float speed = 1f);
    float ApplyLinearMeleePAtkSpeed(int objectId, string animName, float clipLengthSec);
    void SetPAtkSpeed(int objectId, float patkspd);
    void PlayLobbyLocomotion(IAnimationController controller, string stateOrPrefixWithWeapon);
    float PlayExactAnimatorState(int objectId, string stateName, bool snapToEnd = false);
}

