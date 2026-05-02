using System.ComponentModel;
using UnityEngine;

public interface ICombo
{
    public void SinglPreStart(float elapsedTime, BothModel bothModel);
    public void SinglPreUpdate(float elapsedTime, BothModel bothModel);

    public void SinglPreFinish(float elapsedTime, BothModel bothModel);

    public int StartEvent(float elapsedTime, BothModel bothModel);
    public int UpdateEvent(float elapsedTime, BothModel bothModel);
    public int FinishEvent(float elapsedTime, BothModel bothModel);

    public int FlyEvent(float elapsedTime, BothModel bothModel);

    public float GetLaunchTime();
}
