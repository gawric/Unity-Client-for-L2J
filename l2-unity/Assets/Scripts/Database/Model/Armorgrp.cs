using UnityEngine;

[System.Serializable]
public class Armorgrp : Abstractgrp {
   
    [SerializeField] private int _mpBonus;
    private int _pDef;
    private int _mDef;
    [SerializeField] private ItemSlot _bodypart;
    [SerializeField] private string[] _model;
    [SerializeField] private string[] _texture;
    private ArmorType armorType;
    
    public int PDef{ get { return _pDef; } set { _pDef = value; } }
    public int MDef { get { return _mDef; } set { _mDef = value; } }
    public int MpBonus { get { return _mpBonus; } set { _mpBonus = value; } }

    public ArmorType ArmorType { get { return armorType; } set { armorType = value; } }
   

    public ItemSlot BodyPart { get { return _bodypart; } set { _bodypart = value; } }
    public string[] Model { get { return _model; } set { _model = value; } }
    public string[] Texture { get { return _texture; } set { _texture = value; } }
}
