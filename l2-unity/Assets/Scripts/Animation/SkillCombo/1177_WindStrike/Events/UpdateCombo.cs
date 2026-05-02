using UnityEngine;

public class UpdateCombo : AbstractAnimCombo
{
    private float delay1;
    public int Event(float elapsedTime, BothModel bothModel, float launchTime)
    {
        //if (delay1 >= elapsedTime)
        if (elapsedTime >= delay1)
        {
            return 1;
        }

        return 0;
    }
    //CAST END
    public void StartAnim(float elapsedTime, BothModel bothModel, float launchTime)
    {
        string animName = bothModel.GetCurrentAnimName();
        PlayerAnimationController.Instance.SetBool(animName, true);
        PlayerAnimationController.Instance.SetCastSpeed(bothModel.GetCompressionByIndex(1));
        PlayerAnimationController.Instance.SetBool(animName, false);
        float fly_s = ConvertHitTimeToMs(bothModel.GetTimeToTravel());
        float lauchTime_s = ConvertHitTimeToMs(launchTime);
        float delay = bothModel.GetHitTime() - lauchTime_s;
        delay1 = delay - fly_s;
    }
   
}
