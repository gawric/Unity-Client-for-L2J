using UnityEngine;

public class FinalCombo : AbstractAnimCombo
{
    public int Event(float elapsedTime, BothModel bothModel, float launchTime)
    {
        //CAST SHOT

        if (PlayerAnimationController.Instance.GetNormalizedTimeOffsetSpeed() >= 1.0f)
        {
            //return 1;
        }
        else
        {
            //begin  animator[2]
            if (PlayerAnimationController.Instance.GetNormalizedTimeOffsetSpeed() >= 0.25f & bothModel.isFly())
            {
                bothModel.GetEffectManager().HideEffect(elapsedTime, 1177, bothModel.GetTimeToTravel());
                return 2;
            }
        }

        if (PlayerAnimationController.Instance.IsFinishAnimation(bothModel.GetCurrentAnimName()))
        {
            Debug.Log("TimeToFly Normalized end " + elapsedTime);
            Debug.Log(" Time Launch " + PlayerAnimationController.Instance.GetStateInfo().normalizedTime);
             PlayerAnimationController.Instance.SetBool(bothModel.GetCurrentAnimName(), false);

            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
            PlayerStateMachine.Instance.ChangeState(PlayerState.IDLE);
            PlayerStateMachine.Instance.NotifyEvent(global::Event.WAIT_RETURN);
            return 3;
        }

        return 1;
    }

   
    public void StartAnim(float elapsedTime, BothModel bothModel, float launchTime)
    {
        //float delay = bothModel.GetHitTime() - 600;
        //if (delay >= _elapsedTime) return;

        //_singl_run3 = true;
         //Debug.Log("Exec start shot start " + _elapsedTime + " hit_time" + _hitTime);
        //windstrike shot - 2000 ms
         PlayerAnimationController.Instance.SetShotSpeed(bothModel.GetCompressionByIndex(2));
         PlayerAnimationController.Instance.SetBool(bothModel.GetCurrentAnimName(), true);
         
    }
}
