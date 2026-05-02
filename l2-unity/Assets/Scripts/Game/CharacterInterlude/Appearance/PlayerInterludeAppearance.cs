using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class PlayerInterludeAppearance : Appearance
{
    public int Race;
    public int Sex;
    public int Face;
    public int BaseClass;
    public bool Running = true;
    public int HairStyle;
    public int HairColor;
    public int Chest;
    public int Legs;
    public int Gloves;
    public int Feet;
    public PaperDollSelection PaperDoll { get; } = new();
    
    public byte FaceByte { get { return Convert.ToByte(Face); }  }
    public byte HairStyleByte { get { return Convert.ToByte(HairStyle); } }
    public byte HairColorByte { get { return Convert.ToByte(HairColor); } }
}

