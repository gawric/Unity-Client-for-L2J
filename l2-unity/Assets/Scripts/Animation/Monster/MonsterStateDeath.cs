using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateDeath : MonsterStateBase
{
    public string parameterName;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //LoadComponents(animator);

        StopAnimationTrigger(animator, parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopMonsterCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
        }

    }
}
