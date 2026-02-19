using UnityEngine;

public class Combo_1177 : ICombo
{
    private int _statusEvent;

    private StartCombo _startCombo;
    private UpdateCombo _updateCombo;
    private FinalCombo _finalCombo;
    private FlyCombo _flyCombo;

    //ms
    private float _launchTimeSec = 0.6f;

    public Combo_1177()
    {
        _startCombo = new StartCombo();
        _updateCombo = new UpdateCombo();
        _finalCombo = new FinalCombo();
        _flyCombo = new FlyCombo();
    }

    public void SinglPreStart(float elapsedTime, BothModel bothModel)
    {
        _startCombo.CalcCompression(elapsedTime, bothModel, _launchTimeSec);
    }

    public void SinglPreUpdate(float elapsedTime, BothModel bothModel)
    {
        _updateCombo.StartAnim(elapsedTime, bothModel, _launchTimeSec);
    }
    public void SinglPreFinish(float elapsedTime, BothModel bothModel)
    {
        _finalCombo.StartAnim(elapsedTime, bothModel, _launchTimeSec);
    }

    public int StartEvent(float elapsedTime, BothModel bothModel)
    {
        return _startCombo.Event(elapsedTime, bothModel , _launchTimeSec);
    }

    public int UpdateEvent(float elapsedTime, BothModel bothModel)
    {
        return _updateCombo.Event(elapsedTime, bothModel, _launchTimeSec);
    }

    public int FinishEvent(float elapsedTime, BothModel bothModel)
    {
        return _finalCombo.Event(elapsedTime, bothModel, _launchTimeSec);
    }

    public float GetLaunchTime()
    {
       return _launchTimeSec;
    }

    public int FlyEvent(float elapsedTime, BothModel bothModel)
    {
        return _flyCombo.Fly(elapsedTime, bothModel);
    }
}
