
using UnityEngine;

[System.Serializable]
public class Stats {
    [SerializeField] private int _level;
    [SerializeField] private int _speed;
    [SerializeField] private float _scaledSpeed;
    [SerializeField] private int _walkSpeed;
    [SerializeField] private int _pAtkSpd;
    [SerializeField] private int _mAtkSpd;
    [SerializeField] private int _maxHp;
    [SerializeField] private int _maxMp;
    [SerializeField] private int _maxCp;

     private int _baseWalkingSpeed;
     private int _baseRunSpeed;
     private float _realWalkingSpeed;
     private float _realRunSpeed;
     private float _realPAtkSpeed;
     private float _unityWalkingSpeedMetrS;
     private float _unityRunSpeedMetrS;




    [SerializeField] private float _scaledRunSpeed;
    [SerializeField] private float _scaledWalkSpeed;
    [SerializeField] private float _basePAtkSpeed;

    private float _scaledAnimRunSpeed;
    private float _scaledAnimWalkSpeed;

    public int Level { get => _level; set => _level = value; }
    public int Speed { get => _speed; set => _speed = value; }
    public float ScaledAnimRunSpeed { get => _scaledAnimRunSpeed; set => _scaledAnimRunSpeed = value; }
    public float ScaledAnimWalkSpeed { get => _scaledAnimWalkSpeed; set => _scaledAnimWalkSpeed = value; }
    public float ScaledSpeed { get => _scaledSpeed; set => _scaledSpeed = value; }

    public float BasePAtkSpeed { get => _basePAtkSpeed; set => _basePAtkSpeed = value; }
    public int PAtkSpd { get => _pAtkSpd; set => _pAtkSpd = value; }
    public int MAtkSpd { get => _mAtkSpd; set => _mAtkSpd = value; }
    public int MaxHp { get => _maxHp; set => _maxHp = value; }
    public int MaxMp { get => _maxMp; set => _maxMp = value; }
    public int MaxCp { get => _maxCp; set => _maxCp = value; }

    public float UnitySpeedWalking { get => _unityWalkingSpeedMetrS; set => _unityWalkingSpeedMetrS = value; }

    public float UnitySpeedRun { get => _unityRunSpeedMetrS; set => _unityRunSpeedMetrS = value; }

    public int BaseWalkingSpeed { get { return _baseWalkingSpeed; } set { _baseWalkingSpeed = value; } }
    public int BaseRunSpeed { get { return _baseRunSpeed; } set { _baseRunSpeed = value; } }

    public float RunRealSpeed { get { return _realRunSpeed; } set { _realRunSpeed = value; } }

    public float PAtkRealSpeed { get { return _realPAtkSpeed; } set { _realPAtkSpeed = value; } }

    public float WalkRealSpeed { get { return _realWalkingSpeed; } set { _realWalkingSpeed = value; } }

   // public int WalkSpeed { get => _walkSpeed; set => _walkSpeed = value; }

}
