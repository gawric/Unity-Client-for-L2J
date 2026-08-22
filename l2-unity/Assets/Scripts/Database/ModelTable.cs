using System;
using System.Collections.Generic;
using UnityEngine;

public class ModelTable : AbstractCache
{
    private static ModelTable _instance;

    public static ModelTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = new ModelTable();
            return _instance;
        }
    }

    public class L2ArmorPiece
    {
        public GameObject baseArmorModel;
        public Material material;
        public GameObject[] baseAllModels;
        public Material[] allMaterials;

        public L2ArmorPiece(
            GameObject baseArmorModel,
            Material material,
            GameObject[] baseAllModels,
            Material[] allMaterials)
        {
            this.baseArmorModel = baseArmorModel;
            this.material = material;
            this.baseAllModels = baseAllModels;
            this.allMaterials = allMaterials;
        }
    }

    public void Initialize()
    {
        CacheRaceContainers();
        CacheFaces();
        CacheHair();
        CacheWeapons();
        CacheEtcItems();
        CacheArmors();
        CacheNpcs();
    }

    private static string RaceFolder(CharacterRaceAnimation raceId)
    {
        CharacterRace race = CharacterRaceParser.ParseRace(raceId);
        return "Data/Animations/" + race + "/" + raceId;
    }

    private void CacheRaceContainers()
    {
        _playerContainers = new GameObject[RACE_COUNT];
        _userContainers = new GameObject[RACE_COUNT];
        _pawnContainers = new GameObject[RACE_COUNT];

        for (int r = 0; r < RACE_COUNT; r++)
        {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            string folder = RaceFolder(raceId);
            _playerContainers[r] = Resources.Load<GameObject>(folder + "/Player_" + raceId);
            _userContainers[r] = Resources.Load<GameObject>(folder + "/User_" + raceId);
            _pawnContainers[r] = Resources.Load<GameObject>(folder + "/Pawn_" + raceId);
        }
    }

    private void CacheFaces()
    {
        _faces = new GameObject[RACE_COUNT, FACE_COUNT];

        for (int r = 0; r < RACE_COUNT; r++)
        {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            string folder = RaceFolder(raceId);
            for (int f = 0; f < FACE_COUNT; f++)
                _faces[r, f] = Resources.Load<GameObject>(folder + "/Faces/" + raceId + "_f_" + f);
        }
    }

    private void CacheHair()
    {
        _hair = new GameObject[RACE_COUNT, HAIR_STYLE_COUNT * HAIR_COLOR_COUNT * 2];

        for (int r = 0; r < RACE_COUNT; r++)
        {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            string folder = RaceFolder(raceId);
            for (int style = 0; style < HAIR_STYLE_COUNT; style++)
            {
                for (int color = 0; color < HAIR_COLOR_COUNT; color++)
                {
                    int index = style * HAIR_STYLE_COUNT + color * 2;
                    string hairPrefix = folder + "/Hair/" + raceId + "_h_" + style + "_" + color;
                    _hair[r, index] = Resources.Load<GameObject>(hairPrefix + "_ah");
                    _hair[r, index + 1] = Resources.Load<GameObject>(hairPrefix + "_bh");
                }
            }
        }
    }

    private void CacheWeapons()
    {
        _weapons = LoadItemModels(
            ItemTable.Instance.Weapons,
            weapon => weapon.Weapongrp != null ? weapon.Weapongrp.Model : null,
            "weapon");
    }

    private void CacheEtcItems()
    {
        _etcItems = LoadItemModels(
            ItemTable.Instance.EtcItems,
            etc => etc.EtcItemgrp != null ? etc.EtcItemgrp.Model : null,
            "EtcItemgrp");
    }

    private Dictionary<string, GameObject> LoadItemModels<T>(
        Dictionary<int, T> items,
        Func<T, string> modelOf,
        string label)
    {
        Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        int success = 0;

        foreach (KeyValuePair<int, T> pair in items)
        {
            string model = modelOf(pair.Value);
            if (string.IsNullOrEmpty(model) || cache.ContainsKey(model))
                continue;

            GameObject go = LoadWeaponModel(model);
            if (go == null)
                continue;

            cache[model] = go;
            success++;
        }

        Debug.Log("Successfully loaded " + success + "/" + items.Count + " " + label + " model(s).");
        return cache;
    }

    private void CacheArmors()
    {
        _armors = new Dictionary<string, L2Armor>();
        int materialCount = 0;

        foreach (KeyValuePair<int, Armor> pair in ItemTable.Instance.Armors)
        {
            Armorgrp grp = pair.Value.Armorgrp;
            if (grp.BodyPart == ItemSlot.alldress)
                continue;

            for (int race = 0; race < RACE_COUNT; race++)
            {
                string model = grp.FirstModel[race];
                if (string.IsNullOrEmpty(model))
                {
                    Debug.LogWarning("Model string is null for race " + (CharacterRaceAnimation)race + " in armor " + pair.Key);
                    continue;
                }

                L2Armor l2Armor = GetOrCreateArmor(model, grp.AllModels[race]);
                if (l2Armor == null || l2Armor.baseModel == null)
                    continue;

                AddArmorMaterials(grp, race, l2Armor, ref materialCount);
            }
        }

        Debug.Log("Successfully loaded " + _armors.Count + " armor model(s).");
        Debug.Log("Successfully loaded " + materialCount + " armor material(s).");
    }

    private L2Armor GetOrCreateArmor(string model, List<string> extraModels)
    {
        L2Armor existing;
        if (_armors.TryGetValue(model, out existing))
            return existing;

        L2Armor created = new L2Armor
        {
            baseModel = LoadArmorModel(model),
            allModels = LoadAllArmorModels(extraModels)
        };

        if (created.baseModel == null)
            return created;

        created.materials = new Dictionary<string, Material>();
        created.allMaterials = new Dictionary<string, Material[]>();
        _armors[model] = created;
        return created;
    }

    private void AddArmorMaterials(Armorgrp grp, int raceIndex, L2Armor l2Armor, ref int materialCount)
    {
        string texture = grp.FirstTexture[raceIndex];
        if (l2Armor.materials.ContainsKey(texture))
            return;

        if (NeedsExtraArmorModels(l2Armor, grp.AllModels[raceIndex], grp.BodyPart))
        {
            l2Armor.allModels = LoadAllArmorModels(grp.AllModels[raceIndex]);
            Debug.Log("Reloading all models for " + l2Armor.baseModel.name + ", loading " + l2Armor.allModels.Length + " models");
        }

        Material material = LoadArmorMaterial(texture);
        if (material == null)
            return;

        Material[] fullSet = LoadAllArmorMaterials(grp.AllTextures[raceIndex]);
        if (fullSet.Length > 0 && grp.BodyPart == ItemSlot.fullarmor)
            l2Armor.allMaterials[texture] = fullSet;

        l2Armor.materials[texture] = material;
        materialCount++;
    }

    private static bool NeedsExtraArmorModels(L2Armor l2Armor, List<string> models, ItemSlot slot)
    {
        return l2Armor.allModels[0] == null ||
               (l2Armor.allModels.Length < models.Count && slot == ItemSlot.fullarmor);
    }

    private void CacheNpcs()
    {
        _npcs = new Dictionary<string, L2Npc>();
        int success = 0;

        foreach (KeyValuePair<int, Npcgrp> pair in NpcgrpTable.Instance.Npcgrps)
        {
            string mesh = pair.Value.Mesh;
            if (_npcs.ContainsKey(mesh))
                continue;

            GameObject npc = LoadNpc(mesh);
            if (npc == null)
                continue;

            Dictionary<string, Material[]> materials = new Dictionary<string, Material[]>();
            materials[mesh] = LoadAllMaterials(new List<string>(pair.Value.Materials));
            _npcs[mesh] = new L2Npc(npc, materials);
            success++;
        }

        Debug.Log("Loaded " + success + " npc model(s).");
    }
}
