using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateWait : MonsterStateBase
{
    public int playBreatheSoundChancePercent = 100;
    bool started = false;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }
}
