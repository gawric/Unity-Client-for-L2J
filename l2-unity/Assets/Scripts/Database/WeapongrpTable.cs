using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class WeapongrpTable {
    private static WeapongrpTable _instance;
    public static WeapongrpTable Instance {
        get {
            if (_instance == null) {
                _instance = new WeapongrpTable();
            }

            return _instance;
        }
    }

    private Dictionary<int, Weapongrp> _weaponGrps;
    public Dictionary<int, Weapongrp> WeaponGrps { get { return _weaponGrps; } }

    public void Initialize() {
        ReadWeaponGrpDat();
        ReadWeaponInterlude();
    }

    public Weapongrp GetWeapon(int id)
    {
        if (_weaponGrps.ContainsKey(id))
        {
            return _weaponGrps[id];
        }
        return null;
    }

    private void ReadWeaponGrpDat() {
        _weaponGrps = new Dictionary<int, Weapongrp>();

        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Weapongrp_Classic.txt");
        if (!File.Exists(dataPath)) {
            Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath)) {
            string line;
            while ((line = reader.ReadLine()) != null) {

                string[] keyvals = line.Split('\t');

                Weapongrp weaponGrp = new Weapongrp();

                WeaponType weaponType = WeaponType.none;
                //WeaponMaterial weaponMaterial = WeaponMaterial.None;
                int handness = 0;
                string[] modTex;

                for (int i = 0; i < keyvals.Length; i++) {
                    if (!keyvals[i].Contains("=")) {
                        continue;
                    }

                    string[] keyval = keyvals[i].Split("=");
                    string key = keyval[0];
                    string value = keyval[1];

                    if (!key.Equals("icon"))
                    {
                        if (!key.Equals("weight"))
                        {
                            if (DatUtils.ParseBaseAbstractItemGrpDat(weaponGrp, key, value))
                            {
                                continue;
                            }
                        }

                    }
  

                    switch (key) {              
                        case "body_part": //artifact_a1 = chest, artifact_a2 = legs, artifact_a3 = boots, head = head, artifactbook = gloves, rfinger, lfinger, rear, lear, onepiece,
                            weaponGrp.BodyPart = ItemSlotParser.ParseBodyPart(value); 
                            break;             
                        case "mp_consume": //mp_consume=0
                            weaponGrp.MpConsume = int.Parse(value);
                            break;                  
                        case "handness":
                            handness = int.Parse(value);
                            break;
                        case "weapon_type":
                            weaponType = WeaponTypeParser.Parse(value);
                            break;
                        case "soulshot_count":
                            weaponGrp.Soulshot = byte.Parse(value);
                            break;
                        case "spiritshot_count":
                            weaponGrp.Spiritshot = byte.Parse(value);
                            break;
                        case "weight":
                            weaponGrp.Weight = int.Parse(value);
                           break;
                        case "icon":
                            weaponGrp.Icon = DatUtils.ParseArray(value)[0];
                            break;
                        case "is_magic_weapon":
                            weaponGrp.IsMagicWeapon = int.Parse(value);
                            break;
                        case "wp_mesh": //{{[LineageWeapons.hell_knife_m00_wp]};{1}}
                            //TODO for dualswords, store 2 models and textures
                            modTex = DatUtils.ParseArray(value);
                            weaponGrp.Model = modTex[0];
                            break;
                        case "texture": //{[LineageWeaponsTex.hell_knife_t00_wp]}	
                            //TODO for dualswords, store 2 models and textures
                            modTex = DatUtils.ParseArray(value);
                            weaponGrp.Texture = modTex[0];
                            break;
                        case "item_sound": // {[ItemSound.sword_small_2];[ItemSound.public_sword_shing_9];[ItemSound.sword_small_7];[ItemSound.dagger_4]}
                            string[] itemsounds = DatUtils.ParseArray(value);
                            weaponGrp.ItemSounds = itemsounds;
                            break;                 
                    }
                }


                weaponType = WeaponClassifier.GetType(weaponType, weaponGrp, handness);
                weaponGrp.WeaponType = weaponType;

                if (!ItemTable.Instance.ShouldLoadItem(weaponGrp.ObjectId)) {
                    continue;
                }

                if (!_weaponGrps.ContainsKey(weaponGrp.ObjectId))
                {
                    _weaponGrps.Add(weaponGrp.ObjectId, weaponGrp);
                }
                
            }

            Debug.Log($"Successfully imported {_weaponGrps.Count} weapongrps(s)");
        }
    }

    

    //41 P atk
    //42 M atk
    //45 Crit rate
    //50 P tak speed
    //51 Mana (MpConsume)
    //52 Soulshot
    //53 SpiritShot

    //49 - shield_rate
    //47 - dex
    //48 - shield_pdef
    public void ReadWeaponInterlude()
    {
        //_actionsInterlude = new Dictionary<int, ActionData>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Weapongrp_interlude.txt");

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            int index = 0;
            while ((line = reader.ReadLine()) != null)
            {
                //string[] test = line.Split('\t');
                if (index != 0)
                {
                    string[] ids = line.Split('\t');
                    int id =  Int32.Parse(ids[1]);

                    if (_weaponGrps.ContainsKey(id))
                    {
                        Weapongrp grp = _weaponGrps[id];
                        grp.PAtk = Int32.Parse(ids[41]);
                        grp.Matk = Int32.Parse(ids[42]);
                        grp.CriticalRate = Int32.Parse(ids[45]);

                        grp.Dex = Int32.Parse(ids[47]);
                        grp.ShieldPdef = Int32.Parse(ids[48]);
                        grp.ShieldRate = Int32.Parse(ids[49]);

                        grp.PAtkSpeed = Int32.Parse(ids[50]);
                        grp.MpConsume = Int32.Parse(ids[51]);

                        grp.Soulshot =(byte) Int32.Parse(ids[52]);
                        grp.Spiritshot = (byte)Int32.Parse(ids[53]);
                        grp.DropType = Int32.Parse(ids[2]);
                        grp.DropAnimType = Int32.Parse(ids[3]);
                    }
                    else
                    {
                        Weapongrp grp = new Weapongrp();

                        var wp_model = ids[7];
                        var texture = ids[10];
                        var icon = ids[13];
                        var weight = ids[19];
                        var material = ids[20];
                        var body_part = ids[24];
                        var hadness = ids[25];
                        var weapon_type_id = ids[44];
                        var weaponType1 = WeaponTypeParser.Parse(weapon_type_id);


                        var model = DatUtils.ParseArray(wp_model);
                        grp.Model = model[0];

                        var modTex = DatUtils.ParseArray(texture);
                        grp.Texture = modTex[0];


                        grp.Icon = DatUtils.ParseArray(icon)[0];
                        grp.Weight = int.Parse(weight);
                        grp.Material = ItemMaterialParser.Parse(material);

                        var weaponType = WeaponClassifier.GetType(weaponType1, grp, Int32.Parse(hadness));
                        grp.WeaponType = weaponType;

                        grp.PAtk = Int32.Parse(ids[41]);
                        grp.Matk = Int32.Parse(ids[42]);
                        grp.CriticalRate = Int32.Parse(ids[45]);

                        grp.Dex = Int32.Parse(ids[47]);
                        grp.ShieldPdef = Int32.Parse(ids[48]);
                        grp.ShieldRate = Int32.Parse(ids[49]);

                        grp.PAtkSpeed = Int32.Parse(ids[50]);
                        grp.MpConsume = Int32.Parse(ids[51]);

                        grp.Soulshot = (byte)Int32.Parse(ids[52]);
                        grp.Spiritshot = (byte)Int32.Parse(ids[53]);
                        grp.DropType = Int32.Parse(ids[2]);
                        grp.DropAnimType = Int32.Parse(ids[3]);

                        _weaponGrps.Add(id , grp);
                    }
                }

                index++;
            }

        }
    }

  
}
