using UnityEngine;


public class PlayerStateAtk : PlayerStateAction
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        if (!_enabled)
        {
            return;
        }

        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(0);
        if (clipInfos == null || clipInfos.Length == 0)
        {
            clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        }

        PlayerAnimationController.Instance.UpdateAnimatorAtkSpdMultiplier(clipInfos[0].clip.length);
       // Debug.Log("PlayerStateAtk > OnStateEnter ");

        SetBool("atk01", true, false, false);

        PlaySoundAtRatio(CharacterSoundEvent.Atk_1H, _audioHandler.AtkRatio);
        PlaySoundAtRatio(ItemSoundEvent.sword_small, _audioHandler.SwishRatio);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_enabled)
        {
            return;
        }

        SetBool("atkwait", true, false, false);
        SetBool("atk01", true, false, false);

        if (ShouldDie())
        {
           // Debug.Log("1");
            return;
        }

        if (ShouldAttack())
        {
            //Debug.Log("2");
            return;
        }

        if (ShouldRun())
        {
            //Debug.Log("3");
            return;
        }

        if (ShouldWalk())
        {
           // Debug.Log("4");
            return;
        }

        if (ShouldSit())
        {
           // Debug.Log("5");
            return;
        }

        if (ShouldIdle())
        {
           // Debug.Log("6");
            return;
        }
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_enabled)
        {
            return;
        }
    }
}