using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EtcItemgrpTable {
    private static EtcItemgrpTable _instance;
    public static EtcItemgrpTable Instance {
        get {
            if (_instance == null) {
                _instance = new EtcItemgrpTable();
            }

            return _instance;
        }
    }

    private Dictionary<int, EtcItemgrp> _etcItemGrps;
    public Dictionary<int, EtcItemgrp> EtcItemGrps { get { return _etcItemGrps; } }

    public EtcItemgrp GetEtcItem(int id)
    {
        EtcItemgrp item;
        EtcItemGrps.TryGetValue(id, out item);
        return item;
    }


    public void Initialize() {
        ReadEtcItemGrpDat();
        ReadEtcItemInterlude();
    }

    private void ReadEtcItemGrpDat() {
        _etcItemGrps = new Dictionary<int, EtcItemgrp>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/EtcItemgrp_Classic.txt");
        if (!File.Exists(dataPath)) {
            Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                EtcItemgrp etcItemgrp = new EtcItemgrp();
                string[] keyvals = line.Split('\t');

                for (int i = 0; i < keyvals.Length; i++) {
                    if (!keyvals[i].Contains("=")) {
                        continue;
                    }

                    string[] keyval = keyvals[i].Split("=");
                    string key = keyval[0];
                    string value = keyval[1];

                    if (DatUtils.ParseBaseAbstractItemGrpDat(etcItemgrp, key, value)) {
                        continue;
                    }
                    if(etcItemgrp.ObjectId == 17)
                    {
                        Debug.Log("");
                    }
                    switch (key) {
                        case "etcitem_type": 
                            etcItemgrp.EtcItemType = value;
                            break;
                        case "consume_type":
                            //Debug.Log("Consume Category " + value  + " ID "  + etcItemgrp.ObjectId);
                            etcItemgrp.ConsumeType = ConsumeType.ParceCategory(value);
                            break;
                        case "mesh": //{{[LineageWeapons.hell_knife_m00_wp]};{1}}
                            //TODO for dualswords, store 2 models and textures
                            var modTex = DatUtils.ParseArray(value);
                            etcItemgrp.Model = modTex[0];
                            break;
                    }
                }


                if (!ItemTable.Instance.ShouldLoadItem(etcItemgrp.ObjectId)) {
                    continue;
                }

                _etcItemGrps.TryAdd(etcItemgrp.ObjectId, etcItemgrp);
            }

            Debug.Log($"Successfully imported {_etcItemGrps.Count} etcItemgrp(s)");
        }
    }


    const int InterludeId = 1;
    const int InterludeDropType = 2;
    const int InterludeDropAnimType = 3;
    const int InterludeDropRadius = 4;
    const int InterludeDropHeight = 5;
    const int InterludeDropMesh1 = 7;
    const int InterludeDropTex1 = 10;
    const int InterludeIcon = 13;
    const int InterludeEquipMesh = 24;

    public void ReadEtcItemInterlude()
    {
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/EtcItemgrp_interlude.txt");
        if (!File.Exists(dataPath))
        {
            Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            int index = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (index == 0)
                {
                    index++;
                    continue;
                }

                string[] ids = line.Split('\t');
                if (!IsIndexValid(ids, InterludeId) || !int.TryParse(ids[InterludeId], out int id))
                {
                    index++;
                    continue;
                }

                if (_etcItemGrps.TryGetValue(id, out EtcItemgrp existing))
                {
                    if (string.IsNullOrEmpty(existing.DropModel))
                        ApplyInterludeDropFields(existing, ids);
                }
                else
                {
                    EtcItemgrp etcItemgrp = new EtcItemgrp();
                    etcItemgrp.ObjectId = id;
                    ApplyInterludeDropFields(etcItemgrp, ids);
                    _etcItemGrps.Add(id, etcItemgrp);
                }

                index++;
            }
        }
    }

    static void ApplyInterludeDropFields(EtcItemgrp grp, string[] ids)
    {
        if (grp == null || ids == null)
            return;

        string dropMesh = ReadCell(ids, InterludeDropMesh1);
        if (!string.IsNullOrEmpty(dropMesh) && string.IsNullOrEmpty(grp.DropModel))
            grp.DropModel = dropMesh;

        string dropTex = ReadCell(ids, InterludeDropTex1);
        if (!string.IsNullOrEmpty(dropTex) && string.IsNullOrEmpty(grp.DropTexture))
            grp.DropTexture = dropTex;

        string icon = ReadCell(ids, InterludeIcon);
        if (!string.IsNullOrEmpty(icon) && string.IsNullOrEmpty(grp.Icon))
            grp.Icon = icon;

        string equip = ReadCell(ids, InterludeEquipMesh);
        if (!string.IsNullOrEmpty(equip) && string.IsNullOrEmpty(grp.Model))
            grp.Model = FirstToken(equip);

        if (TryReadInt(ids, InterludeDropType, out int dropType))
            grp.DropType = dropType;
        if (TryReadInt(ids, InterludeDropAnimType, out int dropAnim))
            grp.DropAnimType = dropAnim;
        if (TryReadInt(ids, InterludeDropRadius, out int dropRadius))
            grp.DropRadius = dropRadius;
        if (TryReadInt(ids, InterludeDropHeight, out int dropHeight))
            grp.DropHeight = dropHeight;
    }

    static string ReadCell(string[] ids, int index)
    {
        if (ids == null || index < 0 || index >= ids.Length)
            return null;
        string value = ids[index];
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }

    static bool TryReadInt(string[] ids, int index, out int value)
    {
        value = 0;
        string cell = ReadCell(ids, index);
        return !string.IsNullOrEmpty(cell) && int.TryParse(cell, out value);
    }

    static string FirstToken(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        if (value.IndexOf('[') < 0 && value.IndexOf('{') < 0)
            return value;
        string[] parts = DatUtils.ParseArray(value);
        return parts != null && parts.Length > 0 ? parts[0] : value;
    }

    bool IsIndexValid<T>(T[] array, int index)
    {
        return index >= 0 && index < array.Length;
    }
}