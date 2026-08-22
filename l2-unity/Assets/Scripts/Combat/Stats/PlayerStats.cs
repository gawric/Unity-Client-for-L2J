using System;
using UnityEngine;

[System.Serializable]
public class PlayerStats : Stats
{
    [SerializeField] private int _pAtk;
    [SerializeField] private int _mAtk;
    [SerializeField] private int _pEvasion;
    [SerializeField] private int _mEvasion;
    [SerializeField] private int _pAccuracy;
    [SerializeField] private int _mAccuracy;
    [SerializeField] private int _pCritical;
    [SerializeField] private int _mCritical;
    [SerializeField] private int _mDef;
    [SerializeField] private int _pDef;
    [SerializeField] private int _shieldDef;

    [SerializeField] private double _exp;
    [SerializeField] private double _maxExp;
    [SerializeField] private double _cp;
    [SerializeField] private double _sp;
    [SerializeField] private double _oldSp;

    [SerializeField] private int _currWeight;
    [SerializeField] private int _maxWeight;

    [SerializeField] private float _attackRange;
    [SerializeField] private int _con;
    [SerializeField] private int _dex;
    [SerializeField] private int _str;
    [SerializeField] private int _wit;
    [SerializeField] private int _men;
    [SerializeField] private int _int;

    [SerializeField] private int _karma;
    [SerializeField] private int _pvpKills;
    [SerializeField] private int _pkKills;

    public int PAtk { get { return _pAtk; } set { _pAtk = value; } }
    public int MAtk { get { return _mAtk; } set { _mAtk = value; } }
    public int PEvasion { get { return _pEvasion; } set { _pEvasion = value; } }

    public int MEvasion { get { return _mEvasion; } set { _mEvasion = value; } }
    public int PAccuracy { get { return _pAccuracy; } set { _pAccuracy = value; } }
    public int MAccuracy { get { return _mAccuracy; } set { _mAccuracy = value; } }
    public int PCritical { get { return _pCritical; } set { _pCritical = value; } }
    public int MCritical { get { return _mCritical; } set { _mCritical = value; } }
    public int MDef { get { return _mDef; } set { _mDef = value; } }
    public int PDef { get { return _pDef; } set { _pDef = value; } }
    public double Exp { get { return _exp; } set { _exp = value; } }
    public double MaxExp { get { return _maxExp; } set { _maxExp = value; } }
    public double Sp { get { return _sp; } set { _sp = value; } }
    public double OldSp { get { return _oldSp; } set { _oldSp = value; } }

    public double Cp { get { return _cp; } set { _cp = value; } }

    public int ShieldDef { get { return _shieldDef; } set { _shieldDef = value; } }
    public int CurrWeight { get { return _currWeight; } set { _currWeight = value; } }
    public int MaxWeight { get { return _maxWeight; } set { _maxWeight = value; } }

    public float AttackRange { get => _attackRange; set => _attackRange = value; }
    public int Con { get { return _con; } set { _con = value; } }
    public int Dex { get { return _dex; } set { _dex = value; } }
    public int Str { get { return _str; } set { _str = value; } }
    public int Wit { get { return _wit; } set { _wit = value; } }
    public int Men { get { return _men; } set { _men = value; } }
    public int Int { get { return _int; } set { _int = value; } }

    public int Karma { get { return _karma; } set { _karma = value; } }
    public int PvpKills { get { return _pvpKills; } set { _pvpKills = value; } }
    public int PkKills { get { return _pkKills; } set { _pkKills = value; } }

    /// <summary>
    /// Progress (0-100) from <paramref name="level"/> towards <paramref name="level"/>+1. Exp is the
    /// character's total accumulated experience (from level 1), and LevelServer.GetExp(level) is the
    /// cumulative threshold to REACH that level - since a character already at that level has always
    /// passed its own threshold, dividing raw Exp by it (the old implementation) could only ever
    /// return >=100%. The bar needs the character's progress WITHIN the level's own span instead.
    /// </summary>
    public double ExpPercent(int level)
    {
        long expForLevel = LevelServer.GetExp(level);
        long expForNextLevel = LevelServer.GetExp(level + 1);
        long range = expForNextLevel - expForLevel;
        if (range <= 0)
        {
            // Invalid/max level (GetExp returns -1 past MAX_LEVEL) - nothing left to fill.
            return Exp > 0 ? 100 : 0;
        }

        double percent = (double)(Exp - expForLevel) / range * 100.0;
        return Math.Clamp(Math.Round(percent, 2), 0, 100);
    }

    public double WeightPercent()
    {
        if (Exp == 0)
        {
            return 0;
        }

        if (MaxWeight == 0 & CurrWeight > 0) return 100;

        if (MaxWeight != 0)
        {
            double persent = (double)100 / MaxWeight;
            double currentPerxent = persent * CurrWeight;
            return Math.Round(currentPerxent, 2);
        }

        return 0;
    }
}
