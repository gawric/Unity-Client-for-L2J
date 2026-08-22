using UnityEngine;

public class AnimatorUtils
{
    public static string OBJECT_ID = "objectId";

    public static int GetObjectId(Animator animator)
    {
        if (animator == null)
            return 0;
        return animator.GetInteger(OBJECT_ID);
    }

    /// <summary>
    /// True only for the local player's Animator. Remote UserEntity SMBs must not
    /// touch PlayerAnimationController / PlayerStateMachine.
    /// </summary>
    public static bool IsLocalPlayerAnimator(Animator animator)
    {
        if (animator == null || PlayerEntity.Instance == null)
            return false;

        if (PlayerAnimationController.Instance != null)
        {
            Animator local = PlayerAnimationController.Instance.GetAnimator();
            if (local != null)
                return local == animator;
        }

        if (PlayerEntity.Instance.Identity == null)
            return false;
        return animator.GetInteger(OBJECT_ID) == PlayerEntity.Instance.Identity.Id;
    }

}
