using UnityEngine;

public class FlyCombo : AbstractAnimCombo
{
    public int Fly(float elapsedTime, BothModel bothModel)
    {
            //Debug.Log("ELAAAAPSEEED TINE w1 " + elapsedTime + " hit time " + bothModel.GetHitTime());
            float copyHitTime = bothModel.GetHitTime();
            float copyElapsed = elapsedTime;
            float old = copyHitTime - copyElapsed;
           // Debug.Log("ELAAAAPSEEED TINE w2 " + elapsedTime + " hit time " + bothModel.GetHitTime() + " old " + old);
           // Debug.Log("TimeToFly Normalized" + PlayerAnimationController.Instance.GetNormalizedTimeOffsetSpeed() + " elapsed time " + elapsedTime);
           // Debug.Log("TESSSSSTTTTTTTTT ON OLD " + old);
           // Debug.Log("ELAAAAPSEEED TINE fly " + elapsedTime);
            bothModel.GetEffectManager().StartFlyOrActive(1177, bothModel.GetTargetObjId(), bothModel.GetSpeedFly(), bothModel.GetTimeToTravel());
            //bothModel.GetEffectManager().HideEffect(elapsedTime, 1177);
            return 1;
    }
}
