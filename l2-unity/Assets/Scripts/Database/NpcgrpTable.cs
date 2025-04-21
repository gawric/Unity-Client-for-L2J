using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class NpcgrpTable {
    private static NpcgrpTable _instance;
    public static NpcgrpTable Instance {
        get {
            if (_instance == null) {
                _instance = new NpcgrpTable();
            }

            return _instance;
        }
    }

    private Dictionary<int, Npcgrp> _npcgrps;

    public Dictionary<int, Npcgrp> Npcgrps { get { return _npcgrps; } }

    public void Initialize() {
        ReadNpcgrps();
    }

    private void ReadNpcgrps() {
        _npcgrps = new Dictionary<int, Npcgrp>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Npcgrp_Classic.txt");
        if (!File.Exists(dataPath)) {
            Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                Npcgrp npcgrp = new Npcgrp();

                string[] keyvals = line.Split('\t');

                for (int i = 0; i < keyvals.Length; i++) {
                    if (!keyvals[i].Contains("=")) {
                        continue;
                    }

                    string[] keyval = keyvals[i].Split("=");
                    string key = keyval[0];
                    string value = keyval[1];

                    switch (key) {
                        case "npc_id": 
                            npcgrp.NpcId = int.Parse(value); 
                            break;
                        case "class_name": //[LineageNPC.e_warehouse_keeper_FDwarf]	
                            npcgrp.ClassName = DatUtils.CleanupString(value);
                            break;
                        case "mesh_name": //[LineageNPCs.e_warehouse_keeper_FDwarf_m00]	
                            npcgrp.Mesh = DatUtils.CleanupString(value);
                            break;
                        case "texture_name": //{[LineageNPCsTex.e_warehouse_keeper_FDwarf_m00_t00_b00];[LineageNPCsTex.e_warehouse_keeper_FDwarf_m00_t00_b01];...}
                            npcgrp.Materials = DatUtils.ParseArray(value);
                            break;
                        case "attack_sound1": //{[ItemSound.fist_1];[ItemSound.fist_2];[ItemSound.fist_3]}	
                            npcgrp.AttackSounds = DatUtils.ParseArray(value);
                            break;
                        case "defense_sound1": //{[ItemSound.armor_underwear_1];[ItemSound.armor_underwear_2];[ItemSound.armor_underwear_3];[ItemSound.armor_underwear_4];[ItemSound.armor_leather_7]}
                            npcgrp.DefenseSounds = DatUtils.ParseArray(value);
                            break;
                        case "damage_sound": //{[ChrSound.FNpc_Young_Dmg_1];[ChrSound.FNpc_Young_Dmg_2];[ChrSound.FNpc_Young_Dmg_3]}
                            npcgrp.DamageSounds = DatUtils.ParseArray(value);
                            break;
                        case "attack_effect": //[LineageEffect.p_u002_a]
                            npcgrp.AttackEffect = DatUtils.CleanupString(value);
                            break;
                        case "npc_speed": //{[LineageNPCsTex.e_warehouse_keeper_FDwarf_m00_t00_b00];[LineageNPCsTex.e_warehouse_keeper_FDwarf_m00_t00_b01];...}
                            npcgrp.Speed = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat) / 52.5f;
                            break;
                        case "collision_height":
                            npcgrp.CollisionHeight = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat) / 52.5f;
                            break;
                        case "collision_radius":
                            npcgrp.CollisionRadius = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat) / 52.5f;
                            break;
                        case "social":
                            npcgrp.Friendly = (value == "1");
                            break;
                        case "hpshowable":
                            npcgrp.HpVisible = (value == "1");
                            break;
                        case "slot_rhand":
                            npcgrp.Rhand = int.Parse(value);
                            break;
                        case "slot_lhand":
                            npcgrp.Lhand = int.Parse(value);
                            break;
                        case "org_hp":
                           // npcgrp.MaxHp = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                           npcgrp.MaxHp = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                            break;
                        case "org_mp":
                            npcgrp.MaxMp = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                            break;
                        case "npc_type":
                            string type = DatUtils.CleanupString(value);
                            npcgrp.Type = type;
                            break;
                    }
                }

                _npcgrps.TryAdd(npcgrp.NpcId, npcgrp);
            }

            Debug.Log($"Successfully imported {_npcgrps.Count} npcgrp(s)");
        }
    }

    public Npcgrp GetNpcgrp(int id) {
        Npcgrp npcgrp = null;
        _npcgrps.TryGetValue(id, out npcgrp);

        if(npcgrp == null) {
            Debug.LogWarning($"Npcgrp not found for id [{id}]");
        }

        return npcgrp;
    }
}