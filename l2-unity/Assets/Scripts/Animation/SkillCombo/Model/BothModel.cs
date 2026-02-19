using UnityEngine;

public class BothModel
{
    private float _hitTime;
    private AnimationCombo _AllAnim;
    private float _timeToTravel;
    private float _dist;
    private string _currentAnimName;
    private float[] _compressionTime;
    private EffectSkillsmanager _effectManager;
    private float _speedFly;
    private int _targetObjId;
    private bool _isFly;

    public BothModel(float hitTime , AnimationCombo AllAnim , float dist , int targetObjId)
   {
        _hitTime = hitTime;
        _AllAnim = AllAnim;
        _dist = dist;
        _compressionTime = new float[AllAnim.GetAnimCycle().Length];
        _targetObjId = targetObjId;
        _isFly = true;
    }

    public bool isFly()
    {
        return _isFly;
    }

    public void SetIsFly(bool isFly)
    {
        _isFly = isFly;
    }

    public int GetTargetObjId()
    {
        return _targetObjId;
    }
    public void SetSpeedFly(float speedFly)
    {
        _speedFly = speedFly;
    }

    public float GetSpeedFly()
    {
        return _speedFly;
    }

    public void SetEffectManager(EffectSkillsmanager effectManager)
    {
        _effectManager = effectManager;
    }

    public EffectSkillsmanager GetEffectManager()
    {
        return _effectManager;
    }

    public void SetCompressionAll(float time)
    {
        for(int i = 0; i < _compressionTime.Length; i++)
        {
            _compressionTime[i] = time;
        }
    }
    public void SetCompressionByIndex(int index , float time)
    {
        _compressionTime[index] = time;
    }

    public float GetCompressionByIndex(int index)
    {
        return _compressionTime[index];
    }

    public float GetDist()
    {
        return _dist;
    }
    public float GetHitTime()
    {
        return _hitTime;
    }
    public string GetCurrentAnimName()
    {
        return _currentAnimName;
    }   

    public AnimationCombo GetAllCombo()
    {
        return _AllAnim;
    }
    public void SetTimeToTravel(float timeToTravel)
    {
        _timeToTravel = timeToTravel;
    }

    public float GetTimeToTravel()
    {
        return _timeToTravel;
    }

    public void SetCurrentAnimName(string currentAnimName)
    {
        _currentAnimName = currentAnimName;
    }
}
