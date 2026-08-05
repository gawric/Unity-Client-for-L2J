using UnityEngine;

public class PlayerStateMagicShot : PlayerStateBase
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        if (!_enabled)
        {
            return;
        }

        SetBool("MagicShot", false, false, false);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_enabled)
        {
            return;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_enabled)
        {
            return;
        }

        SetBool("MagicShot", false, false, false);
    }
}
