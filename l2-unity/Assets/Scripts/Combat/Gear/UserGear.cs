using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static ModelTable;

public class UserGear : Gear
{
    protected SkinnedMeshSync _skinnedMeshSync;
    [Header("Armors")]
    [Header("Meta")]

    [Header("Models")]
    [SerializeField] private GameObject _container;

    [System.NonSerialized] public GameObject Hair1;
    [System.NonSerialized] public GameObject Hair2;
    [System.NonSerialized] public GameObject Face;
    
    private CharacterArmorDresser _armorDresser;
    
    public override void Initialize(int ownderId, CharacterRaceAnimation raceId) {
        base.Initialize(ownderId, raceId);

        if(this is PlayerGear) {
            _container = this.gameObject;
        } else {
            _container = transform.GetChild(0).gameObject;
        }

        _armorDresser = new CharacterArmorDresser(_container.transform);
        _armorDresser.OnDestroyGameObject += OnDestroyGameObject;
        _armorDresser.OnSyncMash += OnSyncMesh;
        _armorDresser.OnAddSyncMash += OnAddSyncMesh;
        _armorDresser.OnEquipArmor += OnEquipArmor;
        _skinnedMeshSync = _container.GetComponentInChildren<SkinnedMeshSync>();
    }

    public void UnequipArmor(int itemId, ItemSlot slot)
    {
        int race = (int)_raceId;
        GetDefaultGoWithArmorModel(slot, out Armor[] defaultArmor, out GameObject[] listArmorPiece , (int)_raceId);

        if (listArmorPiece != null && listArmorPiece.Length > 0)
        {
            _armorDresser.UnequipArmorPiece(slot, itemId, defaultArmor, listArmorPiece);
        }

    }

    public void EquipArmor(int itemId, ItemSlot slot)
    {
        Armor armor = ItemTable.Instance.GetArmor(itemId);
        if (armor == null)
        {
            Debug.LogWarning($"Can't find armor {itemId} in ItemTable");
            return;
        }

        ItemSlot slotArmor = _armorDresser.GetExtendedOrGetCurrentArmorPart(slot);
        if (ItemSlot.fullarmor != slotArmor) {
            EquipSingleArmor(armor, slotArmor, itemId);
        } else {
            EquipFullArmor(armor, slotArmor, itemId);
        }
    }

    private void EquipFullArmor(Armor armor, ItemSlot slotArmor, int itemId)
    {
        if (_armorDresser.IsArmorEquipped(armor, slotArmor))
        {
            return;
        }

        L2ArmorPiece armorPiece = (L2ArmorPiece)LoadMesh(EquipmentCategory.FullArmor, itemId, (int)_raceId);
        if (!ValidateArmorPieceFullArmor(armorPiece, itemId))
        {
            return;
        }

        try
        {
            GameObject[] listGo = CreateListArmorMesh(armorPiece.baseAllModels, armorPiece.allMaterials);

            if (listGo.Length == 2)
            {
                GameObject goChest = listGo[0];
                GameObject goLegs = listGo[1];

                _armorDresser.SetFullArmor(false , armor, goChest, ItemSlot.chest);
                _armorDresser.SetFullArmor(true , armor, goLegs, ItemSlot.legs);
            }
            else
            {
                Debug.LogWarning("UserGear->EquipFullArmor: Not Found GameObject FullPlateArmor!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UserGear-> EquipFullArmor: Error equipping armor {itemId}: {e.Message}");
        }
    }

    private void EquipSingleArmor(Armor armor , ItemSlot slotArmor , int itemId)
    {
        if (_armorDresser.IsArmorEquipped(armor, slotArmor))
        {
            return;
        }

        L2ArmorPiece armorPiece = (L2ArmorPiece)LoadMesh(EquipmentCategory.Armor, itemId, (int)_raceId);
        if (!ValidateArmorPiece(armorPiece, itemId))
        {
            return;
        }

        try
        {
            GetDefaultGoWithArmorModel(ItemSlot.fullarmor, out Armor[] defaultArmor, out GameObject[] listArmorPiece , (int)_raceId);

            GameObject armorMesh = CreateArmorMesh(armorPiece.baseArmorModel, armorPiece.material);
            if (armorMesh != null)
            {
                _armorDresser.SetArmorPiece(armor, armorMesh, slotArmor , defaultArmor, listArmorPiece);

            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UserGear-> Error equipping armor {itemId}: {e.Message}");
        }
    }

    /// <summary>
    /// Validates the armor piece data
    /// </summary>
    private bool ValidateArmorPiece(L2ArmorPiece armorPiece, int itemId)
    {
        if (armorPiece == null || armorPiece.baseArmorModel == null || armorPiece.material == null)
        {
            Debug.LogWarning($"UserGear-> Invalid armor data for item {itemId}");
            return false;
        }
        return true;
    }

    private bool ValidateArmorPieceFullArmor(L2ArmorPiece armorPiece, int itemId)
    {
        if (armorPiece == null || armorPiece.baseAllModels == null || armorPiece.allMaterials == null)
        {
            Debug.LogWarning($"UserGear->ValidateArmorPieceFullArmor: Invalid armor data for item {itemId}");
            return false;
        }
        return true;
    }
    
    private GameObject[] CreateListArmorMesh(GameObject[] baseListArmorModel, Material[] materials)
    {
        GameObject[] listGo = new GameObject[baseListArmorModel.Length];

        for(int i = 0; i < baseListArmorModel.Length; i++)
        {
            GameObject baseArmorModel = baseListArmorModel[i];
            Material material = materials[i];
            listGo[i] = CreateArmorMesh(baseArmorModel, material);
        }
        return listGo;
    }

    protected override Transform GetLeftHandBone() {
        if (_leftHandBone == null) {
            _leftHandBone = transform.FindRecursive("Weapon_L_Bone");
        }

        return _leftHandBone;
    }

    protected override Transform GetRightHandBone() {
        if (_rightHandBone == null) {
            _rightHandBone = transform.FindRecursive("Weapon_R_Bone");
        }
        return _rightHandBone;
    }

    protected override Transform GetShieldBone() {
        if (_shieldBone == null) {
            _shieldBone = transform.FindRecursive("Shield_L_Bone");
        }
        return _shieldBone;
    }

    public void EquipHair(GameObject hair1Piece, GameObject hair2Piece)
    {
        EquipHairTest(hair1Piece, hair2Piece);
    }
    
    public void EquipHairTest(GameObject hair1Piece , GameObject hair2Piece)
    {
        if (Hair1 != null)
        {
            DestroyImmediate(Hair1);
            DestroyImmediate(Hair2);

            Hair1 = null;
            Hair2 = null;

        }
        var tr = _container.transform;
        Hair1 = hair1Piece;
        Hair1.transform.SetParent(tr, false);

        Hair2 = hair2Piece;
        Hair2.transform.SetParent(tr, false);

        _skinnedMeshSync.SyncMesh();
    }

    public void EquipFace(GameObject facePiece)
    {
        if (Face != null)
        {
            Destroy(Face);
            //_torsoMeta = null;
        }
        var tr = _container.transform;
        Face = facePiece;
        Face.transform.SetParent(tr, false);

       _skinnedMeshSync.SyncMesh();
    }

    public void OnDestroyGameObject(GameObject go)
    {
        if(ObjectPoolManager.Instance != null)
        {
            if (!ObjectPoolManager.Instance.ReturnToPool(ObjectType.Armor , go))
            {
                Destroy(go);
            }
        }
        else
        {
            Destroy(go);
        }

        //Debug.LogWarning("Запрос на удаление. Удаление состоялось размер " + _container.transform.childCount);
    }

    public void OnSyncMesh(int status)
    {
        //Debug.LogWarning("Запрос на удаление. Синхронизация начало");
        _skinnedMeshSync?.SyncMesh();
        //Debug.LogWarning("Запрос на удаление. Синхронизация конец");
    }

    public void OnAddSyncMesh(GameObject add) {
        _skinnedMeshSync?.AddObjectToQueue(add);
    }

    public void OnEquipArmor(int naked, ItemSlot slot) {
        EquipArmor(naked , slot);
    }

    public void AddUserGearLink(GameObject face, GameObject hair1, GameObject hair2) {
        Face = face;
        Hair1 = hair1;
        Hair2 = hair2;
    }

}
