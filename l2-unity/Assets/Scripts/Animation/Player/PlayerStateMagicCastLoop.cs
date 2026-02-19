using UnityEngine;

public class PlayerStateMagicCastLoop : PlayerStateAction
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        if (!_enabled)
        {
            return;
        }

        //PlayerController.Instance.SetCanMove(false);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (!_enabled)
        {
            return;
        }

        if (ShouldIdle())
        {
            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
            //Debug.Log("Test 1 ddddddddddd");
            return;
        }

        //SetBool("cast_short", false, true, false);

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_enabled)
        {
            return;
        }

        SetBool("spatk_loop01", false, false, false);
    }
}
