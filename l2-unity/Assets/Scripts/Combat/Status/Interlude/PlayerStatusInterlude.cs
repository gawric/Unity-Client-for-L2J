using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusInterlude : Status
{
    [SerializeField] private int _cp;
    [SerializeField] private long _pvpFlag;

    public int Cp { get => _cp; set => _cp = value; }
    public long PvpFlag { get => _pvpFlag; set => _pvpFlag = value; }

    //[SerializeField] private double _hp;
    //[SerializeField] private double _mp;
    //public double Hp { get => _hp; set => _hp = value; }
    //public double Mp { get => _mp; set => _mp = value; }

    public PlayerStatusInterlude() { }
}
