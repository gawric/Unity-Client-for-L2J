using UnityEngine;
using static ModelTable;
using static UnityEngine.EventSystems.EventTrigger;


public class AbstractMeshManager : MonoBehaviour
{

  protected GameObject _go;
  protected object LoadMesh(EquipmentCategory category , params int[] itemIds)
  {
        try
        {
            switch (category)
            {
                case EquipmentCategory.Weapon:
                    int weaponId = itemIds[0];
                    Weapon weapon = ItemTable.Instance.GetWeapon(weaponId);
                    GameObject goWeapon = LoadWeapon(weapon, weaponId);
                    if(weapon != null) ObjectPoolManager.Instance?.AddPrefabToPool(ObjectType.Weapon, goWeapon);
                    return goWeapon;
                case EquipmentCategory.Armor:
                    int aramorId = itemIds[0];
                    int raceId = itemIds[1];
                    Armor armor = ItemTable.Instance.GetArmor(aramorId);
                    ModelTable.L2ArmorPiece goArmor = LoadArmor(armor, CharacterRaceAnimationParser.SafeConvertToRace(raceId));
                    if (armor != null) ObjectPoolManager.Instance?.AddPrefabToPool(ObjectType.Armor, goArmor.baseArmorModel);
                    return goArmor;
                case EquipmentCategory.FullArmor:
                    int fullArmorId = itemIds[0];
                    int fullArmorRaceId = itemIds[1];
                    Armor armorModel = ItemTable.Instance.GetArmor(fullArmorId);
                    ModelTable.L2ArmorPiece fullGoArmor = LoadArmor(armorModel, CharacterRaceAnimationParser.SafeConvertToRace(fullArmorRaceId));
                    if (armorModel != null) AddPrefabToList(ObjectType.Armor , fullGoArmor.baseAllModels);
                    return fullGoArmor;
                case EquipmentCategory.EtcItem:
                    int etcId = itemIds[0];
                    EtcItem etcItem = ItemTable.Instance.GetEtcItem(etcId);
                    GameObject goEtcItem = LoadEtcItem(etcItem, etcId);
                    if (goEtcItem != null) ObjectPoolManager.Instance?.AddPrefabToPool(ObjectType.Arrow, goEtcItem);
                    return goEtcItem;

                default:
                    Debug.LogWarning("Unknown equipment category");
                    return null;

            }

        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"AbstractMeshManager-> LoadMesh не смогли загрузить нужную Mesh !!! ArmorID {itemIds[0]} Type {category} error print: ->\n" + ex.ToString());
            return null;
        }

    }

    protected GameObject CreateCopy(GameObject originalGameObject , string name , ObjectType type)
    {
        // Instantiating weapon
        if(originalGameObject != null)
        {
            _go = GetOrCreate(originalGameObject , type);
            _go.SetActive(false);
            _go.transform.name = name;

            return _go;
        }
        return null;
    }

    private void AddPrefabToList(ObjectType type , GameObject[] allModels)
    {
        if(allModels != null)
        {
            for (int i = 0; i < allModels.Length; i++)
            {
                GameObject go = allModels[i];
                ObjectPoolManager.Instance?.AddPrefabToPool(type, go);
            }
        }
    }
    protected GameObject GetOrCreate(GameObject originalGameObject , ObjectType type)
    {
        if (ObjectPoolManager.Instance != null)
        {
            return  ObjectPoolManager.Instance.SpawnFromPool(type, originalGameObject);
        }
        else
        {
           return  GameObject.Instantiate(originalGameObject);
        }
    }

    private GameObject LoadWeapon(Weapon weapon , int weaponId)
    {
        if (weapon == null)
        {
            Debug.LogWarning($"Could find weapon {weaponId}");
            return null;
        }

        GameObject weaponPrefab = ModelTable.Instance.GetWeaponById(weaponId);
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"Could load prefab for {weaponId}");
            return null;
        }

        return weaponPrefab;
    }

    private GameObject LoadEtcItem(EtcItem etcItem, int etcId)
    {
        if (etcItem == null)
        {
            Debug.LogWarning($"Could find etc item {etcItem}");
            return null;
        }

        GameObject weaponPrefab = ModelTable.Instance.GetEtcById(etcId);
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"Could load prefab for {etcId}");
            return null;
        }

        return weaponPrefab;
    }

    private ModelTable.L2ArmorPiece LoadArmor(Armor armor, CharacterRaceAnimation raceId)
    {
        if (armor == null ) return null;

        ModelTable.L2ArmorPiece armorPiece = ModelTable.Instance.GetArmorPiece(armor, raceId);

        if (armorPiece == null)
        {
            Debug.LogWarning($"MeshManager->Can't find armor {armor.Id} for race {raceId}");
            return null ;
        }

        return armorPiece;
    }



    //GetShieldBone
    //GetLeftHandBone
    //GetLeftHandBone
    //GetRightHandBone
    protected void SetType(WeaponType weaponType , bool leftSlot , Transform[] allBone)
    {
        SetParentGo(_go, weaponType, leftSlot, allBone);
    }

    protected void SetTypeOtherGo(GameObject go, WeaponType weaponType, bool leftSlot, Transform[] allBone)
    {
        SetParentGo(go, weaponType, leftSlot, allBone);
    }
    private void SetParentGo(GameObject go , WeaponType weaponType, bool leftSlot, Transform[] allBone)
    {
        if (go == null)
        {
            Debug.LogError("Gear: Not Create GameObject " + weaponType);
            return;
        }

        if (weaponType == WeaponType.none)
        {
            go.transform.SetParent(allBone[0], false);
        }
        else if (weaponType == WeaponType.bow)
        {
            go.transform.SetParent(allBone[1], false);
        }
        else if (weaponType == WeaponType.arrow)
        {
            go.transform.SetParent(allBone[3], false);
        }
        else if (leftSlot)
        {
            go.transform.SetParent(allBone[2], false);
        }
        else
        {
            go.transform.SetParent(allBone[3], false);
        }
    }


    public void GetDefaultGoWithArmorModel(ItemSlot slot, out Armor[] defaultArmor, out GameObject[] listArmorPiece , int raceId)
    {
        //int race = (int)_raceId;
        slot = ArmorDresserModel.GetExtendedArmorPart(slot);

        defaultArmor = CharacterDefaultEquipment.GetDefaultArmorByItemSlot(slot);
        listArmorPiece = CopyListMash(slot, defaultArmor, raceId);
    }


    protected GameObject[] CopyListMash(ItemSlot slot , Armor[] defaultArmor , int raceId)
    {
        var armorPiece = GetMeshBaseArmor(slot, defaultArmor, raceId);
        return  CreateCopyOrGetPool(armorPiece, slot);
    }

    public L2ArmorPiece[] GetMeshBaseArmor(ItemSlot slot, Armor[] defaultArmor, int raceId)
    {
        L2ArmorPiece[] baseArmorArr = new L2ArmorPiece[defaultArmor.Length];

        for (int i = 0; i < defaultArmor.Length; i++)
        {
            int baseArmorId = defaultArmor[i].Id;
            baseArmorArr[i] = (L2ArmorPiece)LoadMesh(EquipmentCategory.Armor, baseArmorId, raceId);
        }

        return baseArmorArr;
    }

    protected GameObject[] CreateCopyOrGetPool(L2ArmorPiece[] armorArrPiece, ItemSlot slot)
    {
        GameObject[] arrGo = new GameObject[armorArrPiece.Length];
        for (int i = 0; i < armorArrPiece.Length; i++)
        {
            var armorPiece = armorArrPiece[i];
            arrGo[i] = CreateArmorMesh(armorPiece.baseArmorModel, armorPiece.material);
        }
        return arrGo;
    }

    protected GameObject CreateArmorMesh(GameObject baseArmorModel, Material material)
    {
        GameObject mesh = GetOrCreate(baseArmorModel, ObjectType.Armor);
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

        renderer.material = material;
        return mesh;
    }

    protected string GetWeaponModelName(int itemId)
    {
        Weapon weapon = ItemTable.Instance.GetWeapon(itemId);
        if (weapon == null)
        {
            return "Not_Found_Model_Name";
        }
       return  weapon.Weapongrp.Model.Replace("LineageWeapons." , "");

    }



}
