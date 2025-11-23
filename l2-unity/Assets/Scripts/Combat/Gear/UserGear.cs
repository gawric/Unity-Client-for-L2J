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

    //private GameObject _face;
    [Header("Models")]
    [SerializeField] private GameObject _container;

    private GameObject _hair1;
    private GameObject _face;
    private GameObject _hair2;
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
        _armorDresser.OnSyncMash += OnSyncMash;
        _armorDresser.OnEquipArmor += OnEquipArmor;
        _skinnedMeshSync = _container.GetComponentInChildren<SkinnedMeshSync>();
    }




    /// <summary>
    /// Equips armor piece to the specified slot
    /// </summary>
    /// <param name="itemId">ID of the armor item to equip</param>
    /// <param name="slot">Equipment slot where the armor will be equipped</param>
    public void EquipArmor(int itemId, ItemSlot slot)
    {

        Armor armor = ItemTable.Instance.GetArmor(itemId);

        if (armor == null)
        {
            Debug.LogWarning($"Can't find armor {itemId} in ItemTable");
            return;
        }


        L2ArmorPiece armorPiece = (L2ArmorPiece)LoadMesh(EquipmentCategory.Armor, itemId, (int)_raceId);
        if (!ValidateArmorPiece(armorPiece, itemId))
        {
            return;
        }

        try
        {
            GameObject armorMesh = CreateArmorMesh(armorPiece);
            if (armorMesh != null)
            {
                _armorDresser.SetArmorPiece(armor, armorMesh, slot);
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

    /// <summary>
    /// Creates and configures the armor mesh
    /// </summary>
    private GameObject CreateArmorMesh(L2ArmorPiece armorPiece)
    {
        GameObject mesh = CreateCopy(armorPiece.baseArmorModel);
        if (mesh == null)
        {
            Debug.LogWarning("UserGear-> Failed to create armor model copy");
            return null;
        }

        SkinnedMeshRenderer renderer = mesh.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("UserGear-> No SkinnedMeshRenderer found in the armor model");
            return null;
        }

        renderer.material = armorPiece.material;
        return mesh;
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



    public void SetFace(GameObject facePiece)
    {
        _face = facePiece;
    }
    public void SetHair1(GameObject hair1Piece)
    {
        _hair1 = hair1Piece;
    }

    public void SetHair2(GameObject hair2Piece)
    {
        _hair2 = hair2Piece;
    }


    public void EquipHair(GameObject hair1Piece, GameObject hair2Piece)
    {
        EquipHairTest(hair1Piece, hair2Piece);
    }
    public void EquipHairTest(GameObject hair1Piece , GameObject hair2Piece)
    {
        if (_hair1 != null)
        {
            DestroyImmediate(_hair1);
            DestroyImmediate(_hair2);

            _hair1 = null;
            _hair2 = null;

        }
        var tr = _container.transform;
        _hair1 = hair1Piece;
        _hair1.transform.SetParent(tr, false);

        _hair2 = hair2Piece;
        _hair2.transform.SetParent(tr, false);

        _skinnedMeshSync.SyncMesh();
    }

    public void EquipFace(GameObject facePiece)
    {
        if (_face != null)
        {
            Destroy(_face);
            //_torsoMeta = null;
        }
        var tr = _container.transform;
        _face = facePiece;
        _face.transform.SetParent(tr, false);

       _skinnedMeshSync.SyncMesh();
    }

    public void OnDestroyGameObject(GameObject go)
    {
        Destroy(go);
    }

    public void OnSyncMash(int status)
    {
        _skinnedMeshSync?.SyncMesh();
    }

    public void OnEquipArmor(int naked, ItemSlot slot)
    {
        EquipArmor(naked , slot);
    }

}
