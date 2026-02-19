using UnityEngine;

public class PeriodicTimer
{
    private float _startTime;
    private float _interval;
    private readonly float _defaultInterval = 0.1f;

    public void UpdateStartTime(float startTime)
    {
        _startTime = startTime;
        _interval = _defaultInterval;
    }
    public void UpdateStartTime(float startTime , float interval)
    {
        _startTime = startTime;
        _interval = interval;
    }
    public int GetTriggerCount(float elapsedTime, int _moveTimeStamp)
    {
        float tick = elapsedTime - _startTime;

        if (tick >= _interval)
        {
            _startTime = elapsedTime;
            _moveTimeStamp++;
        }
        return _moveTimeStamp;
    }



    
}
