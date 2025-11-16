
using UnityEngine;

[System.Serializable]
public class Stats {
    [SerializeField] private int _level;
    [SerializeField] protected int _runSpeed;
    [SerializeField] protected float _moveSpeedMultiplier;
    [SerializeField] protected int _walkSpeed;
    [SerializeField] private float _scaledSpeed;
    [SerializeField] private int _pAtkSpd;
    [SerializeField] private int _mAtkSpd;
    [SerializeField] private int _maxHp;
    [SerializeField] private int _maxMp;
    [SerializeField] private int _maxCp;
    [SerializeField] private int _karma;

    [SerializeField] private float _scaledRunSpeed;
    [SerializeField] private float _scaledWalkSpeed;
    [SerializeField] protected float _attackSpeedMultiplier;
    [SerializeField] private float _attackRange;
    [SerializeField] private float _basePAtkSpeed;

    private float _scaledAnimRunSpeed;
    private float _scaledAnimWalkSpeed;

    public float AttackRange { get => _attackRange; set => _attackRange = value; }
    public int Level { get => _level; set => _level = value; }
    public int RunSpeed { get => _runSpeed; set => _runSpeed = value; }
    public int WalkSpeed { get => _walkSpeed; set => _walkSpeed = value; }
    public float MoveSpeedMultiplier { get => _moveSpeedMultiplier; set => _moveSpeedMultiplier = value; }
    public float AttackSpeedMultiplier { get => _attackSpeedMultiplier; set => _attackSpeedMultiplier = value; }
    public float ScaledAnimRunSpeed { get => _scaledAnimRunSpeed; set => _scaledAnimRunSpeed = value; }
    public float ScaledAnimWalkSpeed { get => _scaledAnimWalkSpeed; set => _scaledAnimWalkSpeed = value; }
    public float ScaledSpeed { get => _scaledSpeed; set => _scaledSpeed = value; }

    public float BasePAtkSpeed { get => _basePAtkSpeed; set => _basePAtkSpeed = value; }
    public int PAtkSpd { get => _pAtkSpd; set => _pAtkSpd = value; }
    public int MAtkSpd { get => _mAtkSpd; set => _mAtkSpd = value; }
    public int MaxHp { get => _maxHp; set => _maxHp = value; }
    public int MaxMp { get => _maxMp; set => _maxMp = value; }
    public int MaxCp { get => _maxCp; set => _maxCp = value; }
    public int Karma { get { return _karma; } set { _karma = value; } }

}
