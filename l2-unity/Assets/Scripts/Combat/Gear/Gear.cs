using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ModelTable;
using static Unity.Burst.Intrinsics.Arm;
using static UnityEditor.Progress;

public class Gear : AbstractMeshManager 
{
    protected NetworkAnimationController _networkAnimationReceive;
    protected int _ownerId;
    protected CharacterRaceAnimation _raceId;
    public const string weaponName = "weapon";
    public const string shieldName = "shield";
    public Transform[] allBone = new Transform[4];
    [Header("Weapons")]
    [Header("Meta")]
    private Weapon _rightHandWeapon;
    private Weapon _leftHandWeapon;
    private Weapon _leftHandShield;
    private string _origWeaponPrefabName = "";
    private string _origShieldPrefabName = "";

    [Header("Models")]
    [Header("Right hand")]
    [SerializeField] private WeaponType _rightHandType;
    [SerializeField] protected Transform _rightHandBone;
    [SerializeField] protected Transform _rightHand;
    [Header("LeftHand")]
    [SerializeField] private WeaponType _leftHandType;
    [SerializeField] protected Transform _leftHandBone;
    [SerializeField] protected Transform _shieldBone;
    [SerializeField] protected Transform _leftHand;
    [SerializeField] protected string _weaponAnim;
    private int _weaponRange;

    public WeaponType WeaponType { get { return _leftHandType != WeaponType.none ? _leftHandType : _rightHandType; } }
    public string WeaponAnim { get { return _weaponAnim; } }


    public virtual void Initialize(int ownderId, CharacterRaceAnimation raceId) {
        TryGetComponent(out _networkAnimationReceive);
        _ownerId = ownderId;
        _raceId = raceId;
        _weaponAnim = "hand";
        _weaponRange = 40; //default
    }

    public void EquipShield(int weaponId)
    {
        Weapon weapon = ItemTable.Instance.GetWeapon(weaponId);
        ErrorPrint(weapon, weaponId);

        if (weaponId == 0 | IsShieldEquipped(weaponId) | IsWeaponEquipped(weaponId , true) | weapon == null)
        {
            return;
        }

        GameObject weaponPrefab = (GameObject)LoadMesh(EquipmentCategory.Weapon, weaponId);
        _origShieldPrefabName = weaponPrefab.name;
        WeaponType type = WeaponType.none;
        RefreshDataShield(weapon);
        UpdateWeaponType(type);

        Transform[] refreshAllBone = RefreshBone(allBone);
        GameObject go = CreateCopy(weaponPrefab, shieldName , ObjectType.Weapon);

        ActivateGameObject(go, type, true, refreshAllBone);

    }

    public virtual void EquipWeapon(int weaponId, bool leftSlot) {

        Weapon weapon = ItemTable.Instance.GetWeapon(weaponId);

        ErrorPrint(weapon, weaponId);

        if (weaponId == 0 | IsWeaponEquipped(weaponId, leftSlot) | weapon == null) {
            return;
        }


        GameObject weaponPrefab = (GameObject)LoadMesh(EquipmentCategory.Weapon, weaponId);
        _origWeaponPrefabName = weaponPrefab.name;
        WeaponType type = weapon.Weapongrp.WeaponType;
        RefreshData(leftSlot, weapon);
        UpdateWeaponType(type);

        Transform[] refreshAllBone = RefreshBone(allBone);
        GameObject go = CreateCopy(weaponPrefab, weaponName , ObjectType.Weapon);

        ActivateGameObject(go, type, leftSlot, refreshAllBone);

    }



    private void ActivateGameObject(GameObject go, WeaponType type , bool leftSlot , Transform[] refreshAllBone)
    {
        if (go != null)
        {
            SetType(type, leftSlot, refreshAllBone);
            go.SetActive(true);
        }
    }

    private void ErrorPrint(Weapon weapon , int weaponId)
    {
        if (weapon == null)
        {
            Debug.LogWarning("Gear->EquipWeapon: Not Found item in database id " + weaponId);
        }

    }
    private void UpdateWeaponType(WeaponType weaponType) {
        _weaponAnim = WeaponTypeParser.GetWeaponAnim(weaponType);
        _weaponRange = WeaponTypeParser.WeaponRange(weaponType);
    }

    private Transform[] RefreshBone(Transform[] allBone)
    {
        allBone[0] = GetShieldBone();
        allBone[1] = GetLeftHandBone();
        allBone[2] = GetLeftHandBone();
        allBone[3] = GetRightHandBone();
        return allBone;
    }

    protected virtual Transform GetLeftHandBone() {
        if (_leftHandBone == null) {
            _leftHandBone = transform.FindRecursive("Bow Bone");
        }
        return _leftHandBone;
    }

    protected virtual Transform GetRightHandBone() {
        if (_rightHandBone == null) {
            _rightHandBone = transform.FindRecursive("Sword Bone");
        }
        return _rightHandBone;
    }

    protected virtual Transform GetShieldBone() {
        if (_shieldBone == null) {
            _shieldBone = transform.FindRecursive("Shield Bone");
        }
        return _shieldBone;
    }

    public float GetWeaponRange() {
        return VectorUtils.ConvertL2jDistance(_weaponRange);
    }


    public  void UnequipWeapon(bool leftSlot)
    {
        Transform weapon = (leftSlot ? GetLeftHandBone() : GetRightHandBone())?.Find("weapon");
        if (weapon != null)
        {
            Debug.LogWarning("Gear: UnequipShield->Unequip weapon");


            if(ObjectPoolManager.Instance != null)
            {
                weapon.gameObject.name = _origWeaponPrefabName;
                if(!ObjectPoolManager.Instance.ReturnToPool(ObjectType.Weapon, weapon.gameObject))
                {
                    Destroy(weapon.gameObject);
                }
            }
            else
            {
                Destroy(weapon.gameObject);
            }
            RefreshData(leftSlot, null);

        }
    }

    public void UnequipShield()
    {
        Transform shield = GetShieldBone()?.Find("shield");
        if (shield != null)
        {
            if (ObjectPoolManager.Instance != null)
            {
                shield.gameObject.name = _origShieldPrefabName;
                if (!ObjectPoolManager.Instance.ReturnToPool(ObjectType.Weapon, shield.gameObject))
                {
                    Destroy(shield.gameObject);
                }
            }
            else
            {
                Destroy(shield.gameObject);
            }

            Debug.LogWarning("Gear: UnequipShield->Unequip shield");
            RefreshDataShield(null);
        }
    }

    public bool IsWeaponEquipped(int weaponId, bool leftSlot)
    {
        Weapon weapon = leftSlot ? _leftHandWeapon : _rightHandWeapon;
        return weapon != null && weapon.Id == weaponId;
    }

    public bool IsShieldEquipped(int weaponId)
    {
        Weapon weapon = _leftHandShield;
        return weapon != null && weapon.Id == weaponId;
    }


    private void RefreshData(bool leftSlot , Weapon weapon)
    {
        // Updating weapon type
        if (leftSlot)
        {
            _leftHandWeapon = weapon;
            _leftHandType = (weapon == null) ? WeaponType.none : weapon.Weapongrp.WeaponType;
        }
        else
        {
            _rightHandWeapon = weapon;
            _rightHandType = (weapon == null) ? WeaponType.none : weapon.Weapongrp.WeaponType;
        }
    }

    private void RefreshDataShield(Weapon weapon)
    {
        _leftHandShield = weapon;
        _leftHandWeapon = weapon;
        _leftHandType = WeaponType.none;
    }

    public L2ArmorPiece GetMeshBaseArmor(ItemSlot slot , int baseArmorId , int raceId)
    {
        return (L2ArmorPiece)LoadMesh(EquipmentCategory.Armor, baseArmorId, raceId);
    }

}
