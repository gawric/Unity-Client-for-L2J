using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PlayerInterludeAppearance: Appearance
{
    [SerializeField] private int _race;
    [SerializeField] private int _sex;
    [SerializeField] private int _face;
    private int _baseClass;
    private bool _isRunning;
    [SerializeField] private int _hairStyle;
    [SerializeField] private int _hairColor;
    [SerializeField] private int _chest;
    [SerializeField] private int _legs;
    [SerializeField] private int _gloves;
    [SerializeField] private int _feet;
    [SerializeField] private PaperDollSelection _paperDool;

    public PlayerInterludeAppearance()
    {
        _paperDool = new PaperDollSelection();
        _isRunning = true;
    }


    public PaperDollSelection PaperDoll { get => _paperDool; }
    public int Race { get { return _race; } set { _race = value; } }

    public int BaseClass { get { return _baseClass; } set { _baseClass = value; } }

    public bool Running { get { return _isRunning; } set { _isRunning = value; } }
    public int Sex { get { return _sex; } set { _sex = value; } }
    public int Face { get { return _face; } set { _face = value; } }
    public int HairStyle { get { return _hairStyle; } set { _hairStyle = value; } }
    public int HairColor { get { return _hairColor; } set { _hairColor = value; } }
    public int Chest { get { return _chest; } set { _chest = value; } }
    public int Legs { get { return _legs; } set { _legs = value; } }
    public int Gloves { get { return _gloves; } set { _gloves = value; } }
    public int Feet { get { return _feet; } set { _feet = value; } }

    public byte FaceByte { get { return Convert.ToByte(_face); }  }
    public byte HairStyleByte { get { return Convert.ToByte(_hairStyle); } }
    public byte HairColorByte { get { return Convert.ToByte(_hairColor); } }
}

