using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateRun : MonsterStateBase
{

    public string parameterName;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimationManager.Instance.StopMonsterCurrentAnimation(animator, parameterName);
    }


    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
 
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

   
}
