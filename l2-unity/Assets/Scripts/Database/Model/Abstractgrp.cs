using System.Linq;
using UnityEngine;

[System.Serializable]
public class Abstractgrp
{
    [SerializeField] protected int _objectId;
    [SerializeField] protected string _dropModel;
    [SerializeField] protected string _dropTexture;
    [SerializeField] protected string _icon;
    [SerializeField] protected int _weight;
    [SerializeField] protected ItemMaterial _material;
    [SerializeField] protected string _dropSound;
    [SerializeField] private ItemGrade _grade;
    [SerializeField] protected string _equipSound;
    [SerializeField] protected string _inventoryType;
    [SerializeField] private bool _crystallizable;
    [SerializeField] protected int _dropType;
    [SerializeField] protected int _dropAnimType;
    [SerializeField] protected int _dropRadius;
    [SerializeField] protected int _dropHeight;
    private string[] _otherIcon;

    public ItemMaterial Material { get { return _material; } set { _material = value; } }
    public int ObjectId { get { return _objectId; } set { _objectId = value; } }
    public int Weight { get { return _weight; } set { _weight = value; } }
    public string DropModel { get { return _dropModel; } set { _dropModel = value; } }
    public string DropTexture { get { return _dropTexture; } set { _dropTexture = value; } }
    public string Icon { get { return _icon; } set { _icon = value; } }
    public string DropSound { get { return _dropSound; } set { _dropSound = value; } }
    public string EquipSound { get { return _equipSound; } set { _equipSound = value; } }
    public string InventoryType { get { return _inventoryType; } set { _inventoryType = value; } }
    public bool Crystallizable { get { return _crystallizable; } set { _crystallizable = value; } }
    public ItemGrade Grade { get { return _grade; } set { _grade = value; } }
    /// <summary>weapongrp drop_type: 1/2 sword-like stick, 3/4 club/staff, 0 flat.</summary>
    public int DropType { get { return _dropType; } set { _dropType = value; } }
    /// <summary>drop_anim_type: 1/2 throw+spin, 3 throw no spin, 5 adena FX, 0 none.</summary>
    public int DropAnimType { get { return _dropAnimType; } set { _dropAnimType = value; } }
    public int DropRadius { get { return _dropRadius; } set { _dropRadius = value; } }
    public int DropHeight { get { return _dropHeight; } set { _dropHeight = value; } }

    public string[] OtherIcon { get { return _otherIcon; } }
    public void SetOtherIcon(string[] allIcon)
    {
        _otherIcon = allIcon
         .Where(icon => !icon.Equals("None"))
         .Distinct()                  
         .ToArray();                  
    }
}
