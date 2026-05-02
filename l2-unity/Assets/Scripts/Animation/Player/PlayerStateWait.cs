using UnityEngine;

public class PlayerStateWait : StateMachineBehaviour
{

    public string parameterName;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {


        // LoadComponents(animator);
        //if (!_enabled)
        //{
        //    return;
        //}

        AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //if (!_enabled)
        //{
        //    return;
       // }

        //if (ShouldDie())
       // {
        //    return;
       // }

        //if (ShouldAttack())
        //{
            //if (PlayerEntity.Instance.isAccesNewAtk)
            //{
              //  SetBool(PlayerEntity.Instance.RandomName, true, true, false);
           // }
            
         //   return;
        //}

        //if (ShouldRun())
        //{
        //    return;
        //}


       // if (ShouldWalk())
        //{
        //    return;
       // }


        //if (ShouldCast())
        //{
        //    return;
        //}

       // if (ShouldJump(false))
       // {
        //    return;
        //}

        //if (ShouldSit())
        //{
         //   return;
       // }

        //if (ShouldAtkWait())
        //{
        //    return;
        //}
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //if (!_enabled)
       // {
        //    return;
       // }

       // SetBool("wait", true, false, false);
    }
}
