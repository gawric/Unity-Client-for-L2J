using UnityEngine;

public class PlayerStateWalk: StateMachineBehaviour
{
    public string parameterName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //LoadComponents(animator);
        //if (!_enabled)
        // {
        //    return;
        // }

        //_hasStarted = true;
        //_lastNormalizedTime = 0;
        Debug.Log("Walking State STOP");
        AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);


    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //if (!_enabled)
       // {
        //    return;
       // }

        

       // if (ShouldDie())
       // {
        //    return;
       // }

       // if (ShouldAttack())
       // {
        //    SetBool(PlayerEntity.Instance.RandomName, true, true, false);
       //     return;
       // }

        //if (ShouldJump(true))
       // {
        //    return;
       // }

       // if (ShouldRun())
        //{
        //    return;
       // }

        //if (ShouldSit())
        //{
        //    return;
        //}

        //if (ShouldAtkWait())
        //{
        //    return;
       // }

        //if (ShouldIdle())
        //{
        //    return;
        //}
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //if (!_enabled)
        //{
        //    return;
       // }

        //SetBool("walk", true, false, false);
    }
}
