using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModelTable;
using static UnityEditor.Progress;

public abstract class AbstractGetCache
{
    public static int RACE_COUNT = 14;
    protected static int FACE_COUNT = 6;
    protected static int HAIR_STYLE_COUNT = 6;
    protected static int HAIR_COLOR_COUNT = 4;

    protected Dictionary<string, GameObject> _weapons;
    protected Dictionary<string, GameObject> _etcItems;
    protected Dictionary<string, L2Npc> _npcs;
    protected Dictionary<string, L2Armor> _armors;

    protected GameObject[] _playerContainers;
    protected GameObject[] _userContainers;
    protected GameObject[] _pawnContainers;
    protected GameObject[,] _faces;
    protected GameObject[,] _hair;

    protected class L2Armor
    {
        public GameObject baseModel;
        public GameObject[] allModels;
        public Dictionary<string, Material> materials;
        public Dictionary<string, Material[]> allMaterials;
    }

    protected class L2Npc
    {
        public L2Npc(GameObject baseModel , Dictionary<string, Material[]> materials)
        {
            this.baseModel = baseModel;
            this.allMaterials = materials;
        }
        public GameObject baseModel;
        public Dictionary<string, Material[]> allMaterials;
    }

    // -------
    // Getters
    // -------

    public L2ArmorPiece GetArmorPiece(Armor armor, CharacterRaceAnimation raceId)
    {
        
        if (armor == null)
        {
            Debug.LogWarning("Given armor is null");
            return null;
        }

  
        string model = armor.Armorgrp.FirstModel[(byte)raceId];
        if (!_armors.TryGetValue(model, out var l2Armor))
        {
            Debug.LogWarning($"Can't find armor model {model} in ModelTable");
            return null;
        }


        if (l2Armor.baseModel == null)
        {
            Debug.LogWarning($"Can't find armor model {model} for {raceId} in ModelTable");
            return null;
        }


        string textureName = GetValidTextureName(armor, raceId);
        if (textureName == null) return null;


        if (!TryGetMaterials(l2Armor, textureName, out var firstMaterial, out var allMaterials))
        {
            return null;
        }


        return new L2ArmorPiece(l2Armor.baseModel, firstMaterial, l2Armor.allModels, allMaterials);
    }

    private string GetValidTextureName(Armor armor, CharacterRaceAnimation raceId)
    {
        var textureArray = armor.Armorgrp.FirstTexture;
        if (textureArray == null || textureArray.Length < RACE_COUNT || textureArray[(byte)raceId] == null)
        {
            Debug.LogWarning($"Can't find armor texture for {raceId} in ModelTable");
            return null;
        }
        return textureArray[(byte)raceId];
    }

    private bool TryGetMaterials(L2Armor l2Armor, string textureName, out Material firstMaterial, out Material[] allMaterials)
    {
        firstMaterial = null;
        allMaterials = null;

        if (l2Armor.materials == null)
        {
            Debug.LogWarning($"Can't find armor materials for texture {textureName} in ModelTable");
            return false;
        }

        l2Armor.materials.TryGetValue(textureName, out firstMaterial);
        l2Armor.allMaterials.TryGetValue(textureName, out allMaterials);

        if (firstMaterial == null)
        {
            Debug.LogWarning($"Can't find armor material for model: {textureName} in ModelTable");
            return false;
        }

        return true;
    }


    public GameObject GetWeaponById(int itemId)
    {
        Weapon weapon = ItemTable.Instance.GetWeapon(itemId);
        if (weapon == null)
        {
            Debug.LogWarning($"Can't find weapon {itemId} in ItemTable");
        }

        return GetWeapon(weapon.Weapongrp.Model);
    }

    public GameObject GetEtcById(int itemId)
    {
        EtcItem etcItem = ItemTable.Instance.GetEtcItem(itemId);
        if (etcItem == null)
        {
            Debug.LogWarning($"Can't find etcItem  {itemId} in ItemTable");
        }

        return GetEtcItem(etcItem.EtcItemgrp.Model);
    }

    public GameObject GetWeapon(string model)
    {
        if (!_weapons.ContainsKey(model))
        {
            Debug.LogWarning($"Can't find weapon model {model} in ModelTable");
            return null;
        }

        GameObject go = _weapons[model];
        if (go == null)
        {
            Debug.LogWarning($"Can't find weapon model {model} in ModelTable");
            return null;
        }

        return go;
    }

    public GameObject GetEtcItem(string model)
    {
        if (!_etcItems.ContainsKey(model))
        {
            Debug.LogWarning($"Can't find weapon model {model} in ModelTable");
            return null;
        }

        GameObject go = _etcItems[model];
        if (go == null)
        {
            Debug.LogWarning($"Can't find weapon model {model} in ModelTable");
            return null;
        }

        return go;
    }

    public GameObject GetContainer(CharacterRaceAnimation raceId, EntityType entityType)
    {
        GameObject go = null;

        switch (entityType)
        {
            case EntityType.User:
                go = _userContainers[(byte)raceId];
                break;
            case EntityType.Player:
                go = _playerContainers[(byte)raceId];
                break;
            case EntityType.Pawn:
                go = _pawnContainers[(byte)raceId];
                break;
        }

        if (go == null)
        {
            Debug.LogError($"Can't find container for race {raceId} and entity type {entityType}");
        }

        return go;
    }

    public GameObject GetFace(CharacterRaceAnimation raceId, byte face)
    {
        GameObject go = _faces[(byte)raceId, face];
        if (go == null)
        {
            Debug.LogError($"Can't find face {face} for race {raceId} at index {raceId},{face}");
        }

        return go;
    }

    public GameObject GetHair(CharacterRaceAnimation raceId, byte hairStyle, byte hairColor, bool bh)
    {
        byte index = (byte)(hairStyle * 8 + hairColor * 2);
        index = BugFix(raceId, index);
        if (bh)
        {
            index += 1;
        }

        //Debug.Log($"Loading hair[{index}] Race:{raceId} Model:{hairStyle}_{hairColor}_{(bh ? "bh" : "ah")}");

        GameObject go = _hair[(byte)raceId, index];
        if (go == null)
        {
            Debug.LogError($"Can't find hairstyle {hairStyle} haircolor {hairColor} for race {raceId} at index {raceId},{index}");
        }



        return go;
    }

    public byte BugFix(CharacterRaceAnimation raceId, byte index)
    {
        if (CharacterRaceAnimation.FFighter == raceId)
        {
            if (index == 8)
            {
                return (byte)6;
            }
            else if (index == 10)
            {
                return (byte)8;
            }
            else if (index == 12)
            {
                return (byte)10;
            }
        }
        return index;
    }

    public GameObject GetNpc(string meshname)
    {
        L2Npc npc = null;
        _npcs.TryGetValue(meshname, out npc);
        if (npc == null)
        {
            Debug.LogWarning($"Can't find npc {meshname} model in ModelTable.");
            return null;
        }

        AddMaterial(npc);


        return npc.baseModel;
    }

    private void AddMaterial(L2Npc npc)
    {
        if (npc.allMaterials.Count > 0)
        {
            GameObject mesh = npc.baseModel;
            SkinnedMeshRenderer renderer = mesh.GetComponentInChildren<SkinnedMeshRenderer>();
            Material[] materials = npc.allMaterials.Values.First();
            if (renderer != null & materials.Length > 0 )
            {
                if (materials[0] != null) renderer.material = materials[0];

            }
        }
    }

 
}
