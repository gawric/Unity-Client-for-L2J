using UnityEngine;

public class PlayerMovementSpeedCalculator : ICalcSpeed
{
    private float _defaultRunSpeed;
    private float _defaultWalkSpeed;
    private float _default =  0.7f;
    private float _currentSpeed = 0.0f;

    private float _defaultRotateSpeedBehind = 0.06f;
    private float _defaultRotateSpeed = 0.2f;


    public float GetSpeed(bool _running)
    {
       return  GetRealSpeedMove(_running);
    }

    public float CalculateInitialSpeed()
    {
          return _currentSpeed = _default;
    }

    protected float GetRealSpeedMove(bool _running)
    {
        if (_running)
        {
            _currentSpeed = _defaultRunSpeed;
        }
        else
        {
            _currentSpeed = _defaultWalkSpeed;
        }
        //Is Rotate to target slow speed to 1f
        return _currentSpeed;
    }

    public float GetSpeedRotate(bool behindPlayer)
    {
        if (behindPlayer)
        {
            return _defaultRotateSpeedBehind;
        }
        else
        {
            return _defaultRotateSpeed; ;
        }
    }

    public void UpdateSpeedWalk(float defaultWalkSpeed)
    {
        _defaultWalkSpeed = defaultWalkSpeed;
    }
    public void UpdateSpeedRun(float defaultRunSpeed)
    {
        _defaultRunSpeed = defaultRunSpeed;
    }

  
}
