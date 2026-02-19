using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModelTable;
using static Unity.Burst.Intrinsics.Arm;
using static UnityEngine.Rendering.DebugUI;

public class Gear : AbstractMeshManager 
{
    protected NetworkAnimationController _networkAnimationReceive;
    protected int _ownerId;
    protected CharacterRaceAnimation _raceId;
    public const string etcName = "etc_";
    public const string weaponName = "weapon_";
    public const string shieldName = "shield_";
    public Transform[] allBone = new Transform[4];
    private GameObject _goCurrentEtcItem;
    [Header("Weapons")]
    [Header("Meta")]
    private Weapon _rightHandWeapon;
    private Weapon _leftHandWeapon;
    private Weapon _leftHandShield;
    private string _origShieldPrefabName = "";
 
    public Action<int, Weapon>  OnEquipAnimationRefresh;
    public Action<string> OnUnequipAnimationRefresh;

    [Header("Models")]
    [Header("Right hand")]
    private WeaponType _lastRightHandType;
    private WeaponType _lastShieldHandType;
    [SerializeField] private WeaponType _rightHandType;
    [SerializeField] protected Transform _rightHandBone;
    [SerializeField] protected Transform _rightHand;
    [Header("LeftHand")]
    [SerializeField] private WeaponType _leftHandType;
    [SerializeField] protected Transform _leftHandBone;
    protected Transform _spineBone;
    protected Transform _rightUpperArm;
    [SerializeField] protected Transform _shieldBone;
    [SerializeField] protected Transform _leftHand;
    [SerializeField] protected string _weaponAnim;
    protected string _lastWeaponAnim = "";
    private int _weaponRange;

    public WeaponType WeaponType { get { return _leftHandType != WeaponType.none ? _leftHandType : _rightHandType; } }
    public string WeaponAnim { get { return _weaponAnim; } }
    public string LastWeaponAnim { get { return _lastWeaponAnim; } }

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
        RefreshDataShield(weapon , WeaponType.shield);
        UpdateWeaponType(type);
        string shieldNameId = shieldName+weaponId;
        Transform[] refreshAllBone = RefreshBone(allBone);
        GameObject go = CreateCopy(weaponPrefab, shieldNameId, ObjectType.Weapon);
        _lastWeaponAnim = _weaponAnim;
        ActivateGameObject(go, type, true, refreshAllBone);

        if (_rightHandType != _lastRightHandType | type != _lastShieldHandType 
            | _rightHandType == WeaponType.pole 
            | _rightHandType == WeaponType.staff)
        {
            OnEquipAnimationRefresh?.Invoke(-1, weapon);
        }
        else
        {
            //if we have nothing in our right hand
            if (_rightHandWeapon == null) OnEquipAnimationRefresh?.Invoke(-1, weapon);
        }

        _lastShieldHandType = type;
    }

    public virtual void EquipWeapon(int weaponId, bool leftSlot) {

        Weapon weapon = ItemTable.Instance.GetWeapon(weaponId);
        leftSlot = weapon?.Weapongrp?.WeaponType == WeaponType.bow;


        ErrorPrint(weapon, weaponId);

        if (weaponId == 0 | IsWeaponEquipped(weaponId, leftSlot) | weapon == null) {
            return;
        }

        GameObject weaponPrefab = (GameObject)LoadMesh(EquipmentCategory.Weapon, weaponId);
        if (weaponPrefab == null) return;

        var weaponNameAndId = weaponName + weaponId;
        WeaponType type = weapon.Weapongrp.WeaponType;
        RefreshData(leftSlot, weapon);
        UpdateWeaponType(type);

        Transform[] refreshAllBone = RefreshBone(allBone);
        GameObject go = CreateCopy(weaponPrefab, weaponNameAndId, ObjectType.Weapon);


        ActivateGameObject(go, type, leftSlot, refreshAllBone);

        if(type != _lastRightHandType & _leftHandShield == null 
            | _rightHandType == WeaponType.pole
            | _rightHandType == WeaponType.staff
            | _rightHandType == WeaponType.bigword
            | _leftHandType == WeaponType.bow)
        {
            OnEquipAnimationRefresh?.Invoke(-1, weapon);
        }
        else
        {
            if (_leftHandWeapon == null | _leftHandShield == null) OnEquipAnimationRefresh?.Invoke(-1, weapon);
        }

        _lastRightHandType = type;

    }


    public void EquipArrowEtcItem(int etcId, bool leftSlot)
    {
        EtcItem etcItem = ItemTable.Instance.GetEtcItem(etcId);

        if (etcId == 0 | IsWeaponEquipped(etcId, leftSlot) | etcItem == null)
        {
            return;
        }

        GameObject etcItemPrefab = (GameObject)LoadMesh(EquipmentCategory.EtcItem, etcId);
        if (etcItemPrefab == null) return;

        var etcIdNameAndId = etcName + etcId;
        Transform[] refreshAllBone = RefreshBone(allBone);
        GameObject go = CreateCopy(etcItemPrefab, etcIdNameAndId, ObjectType.Arrow);

        _goCurrentEtcItem = go;

        ActivateGameObject(go, WeaponType.arrow, leftSlot, refreshAllBone);

    }

    public void EquipLeftAndRightWeapon(int weaponId)
    {
        Weapon weapon = ItemTable.Instance.GetWeapon(weaponId);

        ErrorPrint(weapon, weaponId);

        if (weaponId == 0 | IsWeaponEquippedInBothHands(weaponId) | weapon == null)
        {
            return;
        }

        GameObject weaponPrefab = (GameObject)LoadMesh(EquipmentCategory.Weapon, weaponId);
        //GameObject weaponPrefabRight = (GameObject)LoadMesh(EquipmentCategory.Weapon, weaponId);

        //_origWeaponPrefabName = weaponPrefab.name;
        WeaponType type = weapon.Weapongrp.WeaponType;
        var weaponNameAndId = weaponName + weaponId;
        RefreshData(false, weapon);
        RefreshData(true, weapon);
        UpdateWeaponType(type);

        Transform[] refreshAllBone = RefreshBone(allBone);

        GameObject leftGo = CreateCopy(weaponPrefab, weaponNameAndId, ObjectType.Weapon);
        GameObject rightGo = CreateCopy(weaponPrefab, weaponNameAndId, ObjectType.Weapon);

        if (type != _lastRightHandType
            | _rightHandType == WeaponType.pole
            | _rightHandType == WeaponType.staff
            | _leftHandType == WeaponType.bow)
        {
            OnEquipAnimationRefresh?.Invoke(-1, weapon);
        }
        else
        {
            if (_leftHandWeapon == null | _leftHandShield == null) OnEquipAnimationRefresh?.Invoke(-1, weapon);
        }

        ActivateOtherGameObject(leftGo, type, true, refreshAllBone);
        ActivateOtherGameObject(rightGo, type, false, refreshAllBone);

        _lastRightHandType = type;
        //_lastShieldHandType = type;
    }


    private void ActivateGameObject(GameObject go, WeaponType type , bool leftSlot , Transform[] refreshAllBone)
    {
        if (go != null)
        {
            SetType(type, leftSlot, refreshAllBone);
            go.SetActive(true);
        }
    }

    private void ActivateOtherGameObject(GameObject go, WeaponType type, bool leftSlot, Transform[] refreshAllBone)
    {
        if (go != null)
        {
            SetTypeOtherGo(go , type, leftSlot, refreshAllBone);
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
        _lastWeaponAnim = _weaponAnim;
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

    public Transform GetSpineBone()
    {
        if (_spineBone == null)
        {
            _spineBone = transform.FindRecursive("Bip01_Spine1");
        }
        return _spineBone;
    }

    public Transform GetRightUpperArm()
    {
        if (_rightUpperArm == null)
        {
            _rightUpperArm = transform.FindRecursive("Bip01_R_UpperArm");
        }
        return _rightUpperArm;
    }

    public Transform GetTransformRightHandBone()
    {
        return GetRightHandBone();
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

    public  void UnequipWeapon(bool leftSlot , int weaponId, bool lrDestroy = false)
    {
        string weapondNameId = weaponName + weaponId;

        if (lrDestroy)
        {
            UnequipSingleWeapon(true, weaponId, weapondNameId);
            UnequipSingleWeapon(false, weaponId, weapondNameId);
        }
        else
        {
            UnequipSingleWeapon(leftSlot, weaponId, weapondNameId);
        }

    }

    private void UnequipSingleWeapon(bool leftSlot , int weaponId , string weapondNameId)
    {

        Transform weapon = (leftSlot ? GetLeftHandBone() : GetRightHandBone())?.Find(weapondNameId);


        if (weapon != null)
        {
            string origWeaponPrefabName = GetWeaponModelName(weaponId);

            DestroyObject(weapon.gameObject, origWeaponPrefabName);
            RefreshLastType(leftSlot, weaponId, WeaponType.none);
            RefreshHandWeapon(leftSlot, null, weaponId);


            int findCount = FindAllWeaponCount(weaponName, shieldName);

            if (findCount == 0)
            {
                RefreshData(leftSlot, null);
                _lastWeaponAnim = _weaponAnim;
                UpdateWeaponType(WeaponType.hand);
                OnUnequipAnimationRefresh?.Invoke("");
                Debug.Log("Destroy request item id 3 " + weaponId);
            }
        }
    }

    private void RefreshLastType(bool leftslot , int weaponId, WeaponType type)
    {
        if (!leftslot)
        {
            if(_rightHandWeapon != null)
            {
                if(_rightHandWeapon.Id == weaponId)
                {
                    _lastRightHandType = type;
                }
            }
        }

    }

    public void UnequipShield(int shieldId)
    {
        string shieldNameId = shieldName + shieldId;
        Transform shield = GetShieldBone()?.Find(shieldNameId);



        if (shield != null)
        {
            DestroyObject(shield.gameObject, _origShieldPrefabName);

            RefreshHandShield(null);
            int findCount = FindAllWeaponCount(weaponName, shieldName);
            //if no equip sword and no equip shield. findCount = 1 > then remove current shield 
            if (findCount == 0)
            {
                RefreshDataShield(null);
                UpdateWeaponType(WeaponType.hand);
                OnUnequipAnimationRefresh?.Invoke("");
            }

        }
    }

    private void DestroyObject(GameObject go , string origPrefabName)
    {
        if (ObjectPoolManager.Instance != null)
        {
            go.name = origPrefabName;
            if (!ObjectPoolManager.Instance.ReturnToPool(ObjectType.Weapon, go))
            {
                Destroy(go);
            }
        }
        else
        {
            Destroy(go);
        }
    }

    public bool IsWeaponEquipped(int weaponId, bool leftSlot)
    {
        Weapon weapon = leftSlot ? _leftHandWeapon : _rightHandWeapon;
        if (weapon == null) return false;
        return weapon.Id == weaponId;
    }

    public bool IsWeaponEquippedInBothHands(int weaponId)
    {

        bool inLeftHand = _leftHandWeapon != null && _leftHandWeapon.Id == weaponId;
        bool inRightHand = _rightHandWeapon != null && _rightHandWeapon.Id == weaponId;

    
        return inLeftHand && inRightHand;
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
            var typeL = (weapon == null) ? WeaponType.hand : weapon.Weapongrp.WeaponType;
            _leftHandType = typeL;
        }
        else
        {
            _rightHandWeapon = weapon;
            var typeR = (weapon == null) ? WeaponType.hand : weapon.Weapongrp.WeaponType;
            _rightHandType = typeR;
        }
    }

    private void RefreshHandWeapon(bool leftSlot, Weapon weapon, int weaponId)
    {
        var targetWeapon = leftSlot ? _leftHandWeapon : _rightHandWeapon;
        if (targetWeapon?.Id == weaponId)
        {
            if (leftSlot) _leftHandWeapon = weapon;
            else _rightHandWeapon = weapon;
        }
    }


    private void RefreshDataShield(Weapon weapon , WeaponType weaponType = WeaponType.hand)
    {
        _leftHandShield = weapon;
        _leftHandWeapon = weapon;
        _leftHandType = weaponType;
    }

    private void RefreshHandShield(Weapon weapon)
    {
        _leftHandShield = weapon;
        _leftHandWeapon = weapon;
    }


    private IEnumerable<Transform> FindTransformsWithName(Transform parent, string pattern)
    {
        if (parent == null) yield break;

    
        if (parent.name.Contains(pattern))
        {
            yield return parent;
        }

 
        foreach (Transform child in parent)
        {
            foreach (Transform match in FindTransformsWithName(child, pattern))
            {
                yield return match;
            }
        }
    }
    private int FindAllWeaponCount(string allWeapons , string allShields)
    {
        var allWeaponsFound = new List<Transform>();
        allWeaponsFound.AddRange(FindTransformsWithName(GetLeftHandBone(), allWeapons));
        allWeaponsFound.AddRange(FindTransformsWithName(GetRightHandBone(), allWeapons));

        var allShieldsFound = FindTransformsWithName(GetShieldBone(), allShields).ToList();

        return allWeaponsFound.Count + allShieldsFound.Count;
    }

    public Vector3 GetPositionRightHand()
    {
        return GetRightHandBone().position;
    }

    public GameObject GetGoEtcItem()
    {
        return _goCurrentEtcItem;
    }

    public Transform FindRecursiveBone(string boneName)
    {
        return transform.FindRecursive(boneName);
    }

    public Transform[] GetAllTransformByRightHand(string[] arrNameGameObject)
    {
        Transform[] transfroms = new Transform[arrNameGameObject.Length];
        for (int i=0; i < arrNameGameObject.Length; i++)
        {
            string name =  arrNameGameObject[i];
            IEnumerable<Transform> result = FindTransformsWithName(GetRightHandBone(), name);
            if (result.GetEnumerator().MoveNext())
            {
                transfroms[i] = result.First();
            }

        }

        return transfroms;
    }

    public bool IsTwoHandedEquipped()
    {
        return _rightHandType == WeaponType.bigword || 
               _rightHandType == WeaponType.pole ||    
               _rightHandType == WeaponType.staff ||  
               _rightHandType == WeaponType.bigblunt;  
    }

}
