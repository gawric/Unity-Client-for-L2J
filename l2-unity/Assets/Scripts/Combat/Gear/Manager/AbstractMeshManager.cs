using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public class AbstractMeshManager : MonoBehaviour
{

  protected GameObject _go;
  protected object LoadMesh(EquipmentCategory category , params int[] itemIds)
  {

        switch (category)
        {
            case EquipmentCategory.Weapon:
                int weaponId = itemIds[0];
                Weapon weapon = ItemTable.Instance.GetWeapon(weaponId);
                return LoadWeapon(weapon, weaponId);
            case EquipmentCategory.Armor:
                int aramorId = itemIds[0];
                int raceId = itemIds[1];
                Armor armor = ItemTable.Instance.GetArmor(aramorId);
                return LoadArmor(armor, CharacterRaceAnimationParser.SafeConvertToRace(raceId));


            default:
                Debug.LogWarning("Unknown equipment category");
                return null;

        }

    }

    protected GameObject CreateCopy(GameObject originalGameObject , string name)
    {
        // Instantiating weapon
        if(originalGameObject != null)
        {
            _go = GameObject.Instantiate(originalGameObject);
            _go.SetActive(false);
            _go.transform.name = name;
            return _go;
        }
        return null;
    }

    protected GameObject CreateCopy(GameObject originalGameObject)
    {
        return Instantiate(originalGameObject);
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

    private ModelTable.L2ArmorPiece LoadArmor(Armor armor, CharacterRaceAnimation raceId)
    {
        if (armor == null ) return null;

        ModelTable.L2ArmorPiece armorPiece = ModelTable.Instance.GetArmorPiece(armor, raceId);
        //Debug.Log("UserGear: EquipArmor Material " + armorPiece.material + " BaseArmor Model " + armorPiece.baseArmorModel);

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
        if(_go == null)
        {
            Debug.LogError("Gear: Not Create GameObject " + weaponType);
            return;
        }

        if (weaponType == WeaponType.none)
        {
            _go.transform.SetParent(allBone[0], false);
        }
        else if (weaponType == WeaponType.bow)
        {
            _go.transform.SetParent(allBone[1], false);
        }
        else if (leftSlot)
        {
            _go.transform.SetParent(allBone[2], false);
        }
        else
        {
            _go.transform.SetParent(allBone[3], false);
        }
    }


}
