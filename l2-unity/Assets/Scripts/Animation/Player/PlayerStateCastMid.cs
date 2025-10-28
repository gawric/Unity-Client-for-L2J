using UnityEngine;

public class PlayerStateCastMid : PlayerStateAction
{
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        if (!_enabled)
        {
            return;
        }
        SetBool("CastMid", false, false, false);
        //PlayerController.Instance.SetCanMove(false);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (!_enabled)
        {
            return;
        }

        //if (PlayerStateMachine.Instance.State == PlayerState.ANIMATION_LOCKED)
        //{
        //    return;
       // }

        if (ShouldIdle())
        {
            return;
        }

        //if (ShouldCastShot())
        //{
        //    return;
        //}

        ////if (PlayerStateMachine.Instance.State == PlayerState.MAGIC_CAST_SHOT)
        // {
        //    return;
        //}


        // if (IsFinishAnimation("cast_short"))
        //{
        // PlayerStateMachine.Instance.ChangeState(PlayerState.MAGIC_CAST_SHOT);
        //}

        //if (ShouldCastLoop())
        //{
        //    return;
        //}

        //SetBool("cast_short", false, true, false);

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_enabled)
        {
            return;
        }

        SetBool("CastMid", false, false, false);
    }
}