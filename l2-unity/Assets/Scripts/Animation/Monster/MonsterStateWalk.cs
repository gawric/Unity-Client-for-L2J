using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateWalk : StateMachineBehaviour
{
    public string parameterName;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        int objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        AnimationManager.Instance.StopMonsterCurrentAnimation(objectId , parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }
}
