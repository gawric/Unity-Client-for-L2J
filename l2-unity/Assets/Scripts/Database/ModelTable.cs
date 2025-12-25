using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class ModelTable : AbstractCache
{
    private static ModelTable _instance;
    public static ModelTable Instance { 
        get { 
            if (_instance == null) {
                _instance = new ModelTable();
            }

            return _instance; 
        } 
    }


    public class L2ArmorPiece {
        public GameObject baseArmorModel;
        public Material material;
        public GameObject[] baseAllModels;
        public Material[] allMaterials;
        public L2ArmorPiece(GameObject baseArmorModel, Material material , GameObject[] baseAllModels, Material[] allMaterials) {
            this.baseArmorModel = baseArmorModel;
            this.material = material;
            this.baseAllModels = baseAllModels;
            this.allMaterials = allMaterials;
        }
    }

    public void Initialize()
    {
        CachePlayerContainers();
        CacheFaces();
        CacheHair();
        CacheWeapons();
        CacheArmors();
        CacheEtcItems();
        CacheNpcs();
    }

    private void OnDestroy() {
        _faces = null;
        _hair = null;
        _instance = null;
        _playerContainers = null;
        _userContainers = null;
        _weapons.Clear();
        _armors.Clear();
        _instance = null;
    }

    // -------
    // CACHE
    // -------
    private void CachePlayerContainers() {
        _playerContainers = new GameObject[RACE_COUNT];
        _userContainers = new GameObject[RACE_COUNT];
        _pawnContainers = new GameObject[RACE_COUNT];

        // Player Containers
        for (int r = 0; r < RACE_COUNT; r++) {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            CharacterRace race = CharacterRaceParser.ParseRace(raceId);

            string path = $"Data/Animations/{race}/{raceId}/Player_{raceId}";
            _playerContainers[r] = Resources.Load<GameObject>(path);
         //   Debug.Log($"Loading player container {r} [{path}]");
        }

        // User Containers
        for (int r = 0; r < RACE_COUNT; r++) {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            CharacterRace race = CharacterRaceParser.ParseRace(raceId);

            string path = $"Data/Animations/{race}/{raceId}/User_{raceId}";
            _userContainers[r] = Resources.Load<GameObject>(path);
            //Debug.Log($"Loading user container {r} [{path}]");
        }

        // Pawn Containers
        for (int r = 0; r < RACE_COUNT; r++) {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            CharacterRace race = CharacterRaceParser.ParseRace(raceId);

            if(raceId == CharacterRaceAnimation.FElf)
            {
                Debug.Log("");
            }

            string path = $"Data/Animations/{race}/{raceId}/Pawn_{raceId}";
            _pawnContainers[r] = Resources.Load<GameObject>(path);
            //  Debug.Log($"Loading user container {r} [{path}]");
        }
    }

    private void CacheFaces() {   
        _faces = new GameObject[RACE_COUNT, FACE_COUNT]; // there is 14 races, each race has 6 faces

        // Faces
        for (int r = 0; r < RACE_COUNT; r++) {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            CharacterRace race = CharacterRaceParser.ParseRace(raceId);
            for (int f = 0; f < FACE_COUNT; f++) {
                string path = $"Data/Animations/{race}/{raceId}/Faces/{raceId}_f_{f}";
                _faces[r, f] = Resources.Load<GameObject>(path);
               // Debug.Log($"Loading face {f} for race {raceId} [{path}]");
            }
        }
    }

    private void CacheHair() {
        _hair = new GameObject[RACE_COUNT, HAIR_STYLE_COUNT * HAIR_COLOR_COUNT * 2]; // there is 14 races, each race has 6 hairstyle (2 models each) of 4 colors

        // Hair
        for (int r = 0; r < RACE_COUNT; r++) {
            CharacterRaceAnimation raceId = (CharacterRaceAnimation)r;
            CharacterRace race = CharacterRaceParser.ParseRace(raceId);

            for (int hs = 0; hs < HAIR_STYLE_COUNT; hs++) {
                for (int hc = 0; hc < HAIR_COLOR_COUNT; hc++) {
                    int index = hs * HAIR_STYLE_COUNT + (hc * 2);
                    string path = $"Data/Animations/{race}/{raceId}/Hair/{raceId}_h_{hs}_{hc}_ah";
                    _hair[r, index] = Resources.Load<GameObject>(path);
                    //Debug.Log($"Loading hair {hs} color {hc} at {index} for race {raceId} [{path}]");

                    path = $"Data/Animations/{race}/{raceId}/Hair/{raceId}_h_{hs}_{hc}_bh";
                    _hair[r, index + 1] = Resources.Load<GameObject>(path);
                    //Debug.Log($"Loading hair {hs} color {hc} at {index + 1} for race {raceId} [{path}]");
                }
            }
        }
    }

    private void CacheWeapons() {
        _weapons = new Dictionary<string, GameObject>();
        int success = 0;
        foreach (KeyValuePair<int, Weapon> kvp in ItemTable.Instance.Weapons) {

            if(_weapons.ContainsKey(kvp.Value.Weapongrp.Model)) {
                continue;
            }

            GameObject weapon = LoadWeaponModel(kvp.Value.Weapongrp.Model);
            if (weapon != null) {
                success++;
                _weapons[kvp.Value.Weapongrp.Model] = weapon;
            }
        }

        Debug.Log($"Successfully loaded {success}/{ItemTable.Instance.Weapons.Count} weapon model(s).");
    }

    private void CacheEtcItems()
    {
          _etcItems = new Dictionary<string, GameObject>();
          int success = 0;
          foreach (KeyValuePair<int, EtcItem> kvp in ItemTable.Instance.EtcItems)
          {

            if (kvp.Value.EtcItemgrp == null || kvp.Value.EtcItemgrp.Model == null ||
             _etcItems.ContainsKey(kvp.Value.EtcItemgrp.Model))
            {
                continue;
            }


            GameObject etcItem = LoadWeaponModel(kvp.Value.EtcItemgrp.Model);
                if (etcItem != null)
                {
                    success++;
                    _etcItems[kvp.Value.EtcItemgrp.Model] = etcItem;
                }
          }

          Debug.Log($"Successfully loaded {success}/{ItemTable.Instance.EtcItems.Count} EtcItemgrp model(s).");
    }



    private void CacheArmors()
    {
        _armors = new Dictionary<string, L2Armor>();
        int armorMaterials = 0;

        foreach (var kvp in ItemTable.Instance.Armors)
        {
            for (int i = 0; i < RACE_COUNT; i++)
            {
                var armor = kvp.Value;
                var armorgrp = armor.Armorgrp;

                // Early exit for invalid slot
                if (armorgrp.BodyPart == ItemSlot.alldress) return;

                // Get model data
                string model = armorgrp.FirstModel[i];
                if (string.IsNullOrEmpty(model))
                {
                    Debug.LogWarning($"Model string is null for race {(CharacterRaceAnimation)i} in armor {kvp.Key}");
                    continue;
                }

                // Get or create L2Armor
                L2Armor l2Model = _armors.TryGetValue(model, out var existing)
                    ? existing
                    : CreateNewArmorModel(model, armorgrp.AllModels[i]);

                if (l2Model == null || l2Model.baseModel == null) continue;

                // Process materials
                ProcessArmorMaterials(armorgrp, i, l2Model, ref armorMaterials);
            }
        }

        LogCacheResults(_armors.Count, armorMaterials);
    }

    private L2Armor CreateNewArmorModel(string model, List<string> models)
    {
        var l2Model = new L2Armor
        {
            baseModel = LoadArmorModel(model),
            allModels = LoadAllArmorModels(models)
        };

        if (l2Model.baseModel != null)
        {
            l2Model.materials = new Dictionary<string, Material>();
            l2Model.allMaterials = new Dictionary<string, Material[]>();
            _armors[model] = l2Model;
        }

        return l2Model;
    }

    private void ProcessArmorMaterials(Armorgrp armorgrp, int raceIndex, L2Armor l2Model, ref int armorMaterials)
    {
        string texture = armorgrp.FirstTexture[raceIndex];
        List<string> textures = armorgrp.AllTextures[raceIndex];

        if (l2Model.materials.ContainsKey(texture)) return;

        Material[] allArmorMaterials = LoadAllArmorMaterials(textures);
        Material armorMaterial = LoadArmorMaterial(texture);

        // Update models if needed
        if (ShouldUpdateModels(l2Model, armorgrp.AllModels[raceIndex], armorgrp.BodyPart))
        {
            l2Model.allModels = LoadAllArmorModels(armorgrp.AllModels[raceIndex]);
            Debug.Log($"Reloading all models for {l2Model.baseModel.name}, loading {l2Model.allModels.Length} models");
        }

        if (armorMaterial == null) return;

        if (allArmorMaterials.Length > 0 && armorgrp.BodyPart == ItemSlot.fullarmor)
        {
            l2Model.allMaterials[texture] = allArmorMaterials;
        }

        l2Model.materials[texture] = armorMaterial;
        armorMaterials++;
    }

    private bool ShouldUpdateModels(L2Armor l2Model, List<string> models, ItemSlot slot)
    {
        return l2Model.allModels[0] == null ||
               (l2Model.allModels.Length < models.Count && slot == ItemSlot.fullarmor);
    }

    private void LogCacheResults(int modelCount, int materialCount)
    {
        Debug.Log($"Successfully loaded {modelCount} armor model(s).");
        Debug.Log($"Successfully loaded {materialCount} armor material(s).");
    }

    private void CacheNpcs()
    {
        _npcs = new Dictionary<string, L2Npc>();
        int success = 0;

        foreach (var kvp in NpcgrpTable.Instance.Npcgrps)
        {
            if (_npcs.ContainsKey(kvp.Value.Mesh)) continue;

            var npc = LoadNpc(kvp.Value.Mesh);
            if (npc == null) continue;

            var materials = new Dictionary<string, Material[]>
            {
                [kvp.Value.Mesh] = LoadAllMaterials(kvp.Value.Materials.ToList())
            };

            _npcs[kvp.Value.Mesh] = new L2Npc(npc, materials);
            success++;
        }

        Debug.Log($"Loaded {success} npc model(s).");
    }








}
