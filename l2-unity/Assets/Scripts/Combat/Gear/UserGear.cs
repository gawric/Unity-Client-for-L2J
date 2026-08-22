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
            GearFlowLog.Warn("EquipArmor abort not in ItemTable id=" + itemId + " askedSlot=" + slot);
            Debug.LogWarning($"Can't find armor {itemId} in ItemTable");
            return;
        }

        ItemSlot slotArmor = ResolveArmorSlot(armor, slot);
        GearFlowLog.Info("EquipArmor id=" + itemId +
            " askedSlot=" + slot +
            " resolved=" + slotArmor +
            " body=" + (armor.Armorgrp != null ? armor.Armorgrp.BodyPart.ToString() : "null"));
        if (ItemSlot.fullarmor != slotArmor) {
            EquipSingleArmor(armor, slotArmor, itemId);
        } else {
            EquipFullArmor(armor, slotArmor, itemId);
        }
    }

    /// <summary>
    /// CharInfo / spawn paperdoll: chest/legs/gloves/feet. Full-body chest skips legs.
    /// Old piece is replaced by <see cref="CharacterArmorDresser.EquipNewArmor"/>.
    /// </summary>
    public void SyncEquippedArmor(PlayerAppearance appearance)
    {
        if (appearance == null)
        {
            GearFlowLog.Warn("SyncArmor abort appearance=null");
            return;
        }

        int chestId = appearance.Chest != 0 ? appearance.Chest : ItemTable.NAKED_CHEST;
        int glovesId = appearance.Gloves != 0 ? appearance.Gloves : ItemTable.NAKED_GLOVES;
        int feetId = appearance.Feet != 0 ? appearance.Feet : ItemTable.NAKED_BOOTS;

        Armor chestArmor = ItemTable.Instance.GetArmor(chestId);
        bool fullBody = chestArmor != null && chestArmor.Armorgrp != null &&
            chestArmor.Armorgrp.BodyPart == ItemSlot.fullarmor;

        GearFlowLog.Info("SyncArmor " + GearFlowLog.Paperdoll(appearance) +
            " fullBody=" + fullBody + " chestId=" + chestId);

        EquipArmor(chestId, fullBody ? ItemSlot.fullarmor : ItemSlot.chest);

        if (!fullBody)
        {
            int legsId = appearance.Legs != 0 ? appearance.Legs : ItemTable.NAKED_LEGS;
            EquipArmor(legsId, ItemSlot.legs);
        }

        EquipArmor(glovesId, ItemSlot.gloves);
        EquipArmor(feetId, ItemSlot.feet);
    }

    static ItemSlot ResolveArmorSlot(Armor armor, ItemSlot fallback)
    {
        ItemSlot body = armor.Armorgrp != null ? armor.Armorgrp.BodyPart : ItemSlot.none;
        body = ArmorDresserModel.GetExtendedArmorPart(body);
        if (body == ItemSlot.fullarmor || body == ItemSlot.chest || body == ItemSlot.legs ||
            body == ItemSlot.gloves || body == ItemSlot.feet)
            return body;
        return ArmorDresserModel.GetExtendedArmorPart(fallback);
    }

    private void EquipFullArmor(Armor armor, ItemSlot slotArmor, int itemId)
    {
        if (_armorDresser.IsArmorEquipped(armor, slotArmor))
        {
            GearFlowLog.Info("EquipFullArmor SKIP already equipped id=" + itemId + " slot=" + slotArmor);
            return;
        }

        L2ArmorPiece armorPiece = (L2ArmorPiece)LoadMesh(EquipmentCategory.FullArmor, itemId, (int)_raceId);
        if (!ValidateArmorPieceFullArmor(armorPiece, itemId))
        {
            ReportMissingArmorAndReset(armor, slotArmor, itemId);
            return;
        }

        GearFlowLog.Info("EquipFullArmor APPLY id=" + itemId);

        try
        {
            GameObject[] listGo = CreateListArmorMesh(armorPiece.baseAllModels, armorPiece.allMaterials);
            GearFlowLog.Info("EquipFullArmor meshes id=" + itemId + " count=" + (listGo != null ? listGo.Length : 0));

            if (listGo != null && listGo.Length == 2)
            {
                GameObject goChest = listGo[0];
                GameObject goLegs = listGo[1];

                _armorDresser.SetFullArmor(false , armor, goChest, ItemSlot.chest);
                _armorDresser.SetFullArmor(true , armor, goLegs, ItemSlot.legs);
            }
            else
            {
                ReportMissingArmorAndReset(armor, slotArmor, itemId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UserGear-> EquipFullArmor: Error equipping armor {itemId}: {e.Message}");
            ReportMissingArmorAndReset(armor, slotArmor, itemId);
        }
    }

    private void EquipSingleArmor(Armor armor , ItemSlot slotArmor , int itemId)
    {
        if (_armorDresser.IsArmorEquipped(armor, slotArmor))
        {
            GearFlowLog.Info("EquipSingleArmor SKIP already equipped id=" + itemId + " slot=" + slotArmor);
            return;
        }

        L2ArmorPiece armorPiece = (L2ArmorPiece)LoadMesh(EquipmentCategory.Armor, itemId, (int)_raceId);
        if (!ValidateArmorPiece(armorPiece, itemId))
        {
            ReportMissingArmorAndReset(armor, slotArmor, itemId);
            return;
        }

        GearFlowLog.Info("EquipSingleArmor APPLY id=" + itemId + " slot=" + slotArmor);

        try
        {
            GetDefaultGoWithArmorModel(ItemSlot.fullarmor, out Armor[] defaultArmor, out GameObject[] listArmorPiece , (int)_raceId);

            GameObject armorMesh = CreateArmorMesh(armorPiece.baseArmorModel, armorPiece.material);
            if (armorMesh != null)
            {
                _armorDresser.SetArmorPiece(armor, armorMesh, slotArmor , defaultArmor, listArmorPiece);

            }
            else
            {
                ReportMissingArmorAndReset(armor, slotArmor, itemId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UserGear-> Error equipping armor {itemId}: {e.Message}");
            ReportMissingArmorAndReset(armor, slotArmor, itemId);
        }
    }

    void ReportMissingArmorAndReset(Armor armor, ItemSlot slotArmor, int itemId)
    {
        string visual = GearFlowLog.ArmorVisual(armor, _raceId, itemId, slotArmor);
        GearFlowLog.Info("Missing mesh/texture, reset to naked " + visual);

        GetDefaultGoWithArmorModel(slotArmor, out Armor[] defaultArmor, out GameObject[] listArmorPiece, (int)_raceId);
        _armorDresser.EquipDefaultSlot(slotArmor, defaultArmor, listArmorPiece);
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
        Debug.Log($"[ShieldDebug] UserGear.OnDestroyGameObject: {go?.name} (parent={go?.transform.parent?.name})");
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
