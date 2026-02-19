using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ArmorgrpTable {
    private static ArmorgrpTable _instance;
    public static ArmorgrpTable Instance {
        get {
            if (_instance == null) {
                _instance = new ArmorgrpTable();
            }

            return _instance;
        }
    }

    private Dictionary<int, Armorgrp> _armorgrps;

    public Dictionary<int, Armorgrp> ArmorGrps { get { return _armorgrps; } }


    public void Initialize() {
        ReadArmorGrpDat();
        ReadArmorInterlude();
    }

    public  Armorgrp GetArmor(int armor)
    {
        if(_armorgrps.ContainsKey(armor)) return _armorgrps[armor];
        return null;
    }

    private void ReadArmorGrpDat() {
        _armorgrps = new Dictionary<int, Armorgrp>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Armorgrp_Classic.txt");
        if (!File.Exists(dataPath)) {
            Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                Armorgrp armorgrp = new Armorgrp();
                armorgrp.FirstModel = new string[ModelTable.RACE_COUNT];
                armorgrp.FirstTexture = new string[ModelTable.RACE_COUNT];

                armorgrp.AllModels = new List<List<string>>(ModelTable.RACE_COUNT);
                InitializeList(armorgrp.AllModels);

                armorgrp.AllTextures = new List<List<string>>(ModelTable.RACE_COUNT);
                InitializeList(armorgrp.AllTextures);

                string[] modTex;

                string[] keyvals = line.Split('\t');
                string armor_type = "";

                for (int i = 0; i < keyvals.Length; i++) {
                    if(!keyvals[i].Contains("=")) {
                        continue;
                    }

                    string[] keyval = keyvals[i].Split("=");
                    string key = keyval[0];
                    string value = keyval[1];
                    
                    if(DatUtils.ParseBaseAbstractItemGrpDat(armorgrp, key, value)) {
                        continue;
                    }


                    switch (key) {                
                        case "body_part": //artifact_a1 = chest, artifact_a2 = legs, artifact_a3 = boots, head = head, artifactbook = gloves, rfinger, lfinger, rear, lear, onepiece,
                            armorgrp.BodyPart = ItemSlotParser.ParseBodyPart(value); //TODO for fullbody store 2 models and textures for one item
                            break;
                        case "armor_type":
                            armorgrp.ArmorType = ArmorClassifier.GetArmorType(DatUtils.CleanupString(value));
                            break;
                        case "m_HumnFigh": // {{[Fighter.MFighter_m002_g]};{[mfighter.mfighter_m002_t10_g]}} or m_HumnFigh={{[Fighter.MFighter_m002_u];[Fighter.MFighter_m006_l]};{[mfighter.mfighter_m002_t45_u];[MFighter.MFighter_m006_t45_l]}}
                            //List<List<string>>
  
                            var mFighter = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MFighter] = mFighter[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MFighter] = mFighter[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte) CharacterRaceAnimation.MFighter] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MFighter] = mFighter[1][0];
                            break;
                        case "f_HumnFigh":

                            var fFighter = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FFighter] = fFighter[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FFighter] = fFighter[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FFighter] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FFighter] = modTex[1];
                            break;
                        case "m_DarkElf":

                            var mDarkElf = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MDarkElf] = mDarkElf[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MDarkElf] = mDarkElf[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.MDarkElf] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MDarkElf] = modTex[1];
                            break;
                        case "f_DarkElf":

                            var fDarkElf = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FDarkElf] = fDarkElf[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FDarkElf] = fDarkElf[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FDarkElf] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FDarkElf] = modTex[1];
                            break;
                        case "m_Dorf":

                            var mDrof = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MDwarf] = mDrof[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MDwarf] = mDrof[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.MDwarf] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MDwarf] = modTex[1];
                            break;
                        case "f_Dorf":


                            var fDrof = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FDwarf] = fDrof[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FDwarf] = fDrof[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FDwarf] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FDwarf] = modTex[1];
                            break;
                        case "m_Elf":

                            var mElf = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MElf] = mElf[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MElf] = mElf[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.MElf] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MElf] = modTex[1];
                            break;
                        case "f_Elf":

                            var fElf = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FElf] = fElf[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FElf] = fElf[1];


                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FElf] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FElf] = modTex[1];
                            break;
                        case "m_HumnMyst":

                            var mMyst = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MMagic] = mMyst[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MMagic] = mMyst[1];


                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.MMagic] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MMagic] = modTex[1];
                            break;
                        case "f_HumnMyst":

                            var fMyst = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FMagic] = fMyst[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FMagic] = fMyst[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FMagic] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FMagic] = modTex[1];
                            break;
                        case "m_OrcFigh":


                            var mOrcFigh = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MOrc] = mOrcFigh[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MOrc] = mOrcFigh[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.MOrc] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MOrc] = modTex[1];
                            break;
                        case "f_OrcFigh":

                            var fOrcFigh = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FOrc] = fOrcFigh[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FOrc] = fOrcFigh[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FOrc] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FOrc] = modTex[1];
                            break;
                        case "m_OrcMage":

                            var mOrcMage = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.MShaman] = mOrcMage[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.MShaman] = mOrcMage[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.MShaman] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.MShaman] = modTex[1];
                            break;
                        case "f_OrcMage":

                            var fOrcMage = DatUtils.ParseMultiArray(value);
                            armorgrp.AllModels[(byte)CharacterRaceAnimation.FShaman] = fOrcMage[0];
                            armorgrp.AllTextures[(byte)CharacterRaceAnimation.FShaman] = fOrcMage[1];

                            modTex = DatUtils.ParseArray(value);
                            armorgrp.FirstModel[(byte)CharacterRaceAnimation.FShaman] = modTex[0];
                            armorgrp.FirstTexture[(byte)CharacterRaceAnimation.FShaman] = modTex[1];
                            break;
                        case "mp_bonus": //mp_bonus=0
                            armorgrp.MpBonus = int.Parse(value);
                            break;
                        case "weight": //weight
                            armorgrp.Weight = int.Parse(value);
                            break;
                    }
                }

               // ArmorType armorTyme = ArmorClassifier.GetArmorType(armor_type);


                _armorgrps.TryAdd(armorgrp.ObjectId, armorgrp);
            }

            Debug.Log($"Successfully imported {_armorgrps.Count} armorgrp(s)");
        }
    }
    //329 PDef
    //330 Mdef
    //331 MpBonus

    private int indexPdef = 329;
    private int indexMdef = 330;
    private int indexMpBouns = 331;
    public void ReadArmorInterlude()
    {
        //_actionsInterlude = new Dictionary<int, ActionData>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Armorgrp_interlude.txt");

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
                    int id = Int32.Parse(ids[1]);
  
                    if (_armorgrps.ContainsKey(id))
                    {
                        Armorgrp grp = _armorgrps[id];
                        
                        if (IsIndexValid(ids, indexPdef))
                        {
                            grp.PDef  = Int32.Parse(ids[indexPdef]);
                        }
                        if (IsIndexValid(ids, indexMdef))
                        {
                            grp.MDef  = Int32.Parse(ids[indexMdef]);
                        }
                        if (IsIndexValid(ids, indexMpBouns))
                        {
                            grp.MpBonus = Int32.Parse(ids[indexMpBouns]);
                        }

                    }
                }

                index++;
            }

        }
    }


    private void InitializeList(List<List<string>> multilist)
    {
        for (int i =0; i < ModelTable.RACE_COUNT; i++)
        {
            multilist.Add(null);
        }

    }
    bool IsIndexValid<T>(T[] array, int index)
    {
        return index >= 0 && index < array.Length;
    }
}
