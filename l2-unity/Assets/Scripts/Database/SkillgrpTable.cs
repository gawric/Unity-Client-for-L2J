using FMOD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class SkillgrpTable
{
    private static SkillgrpTable _instance;

    public static SkillgrpTable Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SkillgrpTable();
            }

            return _instance;
        }
    }

    private Dictionary<int, Dictionary<int, Skillgrp>> _skills;
    private Dictionary<string, AnimationCombo>_animationsCombo;

    public Skillgrp GetSkill(int id , int level)
    {
       if(!_skills.ContainsKey(id)) return null;

       Dictionary<int, Skillgrp> _skillLevel = _skills[id];
       if (!_skillLevel.ContainsKey(level)) return null;

       return _skillLevel[level];
    }

    public SkillNameData GetSkillName(int id, int level)
    {
        return SkillNameTable.Instance.GetName(id, level);
    }

  

    public Dictionary<int, Dictionary<int, Skillgrp>> Skills { get { return _skills; } }

    public void Initialize()
    {
        _skills = new Dictionary<int, Dictionary<int, Skillgrp>>();
        _animationsCombo = new Dictionary<string, AnimationCombo>();
        ReadActions();
        ReadAnimationCombo_Classic();
    }

    private void ReadActions()
    {
        
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Skillgrp_Classic.txt");
        if (!File.Exists(dataPath))
        {
            Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Skillgrp skillGrp = new Skillgrp();

                string[] keyvals = line.Split('\t');

                for (int i = 0; i < keyvals.Length; i++)
                {
                    if (!keyvals[i].Contains("="))
                    {
                        continue;
                    }

                    string[] keyval = keyvals[i].Split("=");
                    string key = keyval[0];
                    string value = keyval[1];

                    switch (key)
                    {
                        case "skill_id":
                            skillGrp.Id = int.Parse(value);
                            break;
                        case "skill_level":
                            skillGrp.Level = int.Parse(value);
                            break;
                        case "skill_sublevel":
                            skillGrp.SubLevel = int.Parse(value);
                            break;
                        case "icon_type":
                            skillGrp.Icon_type = int.Parse(value);
                            break;
                        case "MagicType":
                            skillGrp.MagicType = int.Parse(value);
                            break;
                        case "operate_type":
                            skillGrp.OperateType = int.Parse(value);
                            break;
                        case "mp_consume":
                            skillGrp.MpConsume = int.Parse(value);
                            break;
                        case "cast_range":
                            skillGrp.CastRange = int.Parse(value);
                            break;
                        case "cast_style":
                            skillGrp.CastStyle = int.Parse(value);
                            break;
                        case "hit_time":
                           // float.Parse(settings.data, CultureInfo.InvariantCulture);
                            skillGrp.HitTime = double.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case "cool_time":
                            skillGrp.CoolTime = double.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case "reuse_delay":
                            skillGrp.ReuseDelay = double.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case "effect_point":
                            skillGrp.EffectPoint = int.Parse(value);
                            break;
                        case "is_magic":
                            skillGrp.IsMagic = int.Parse(value);
                            break;
                        case "origin_skill":
                            skillGrp.IsDouble = int.Parse(value);
                            break;
                        case "animation":
                            string animation = DatUtils.CleanupString(value);
                            skillGrp.Animation = DatUtils.CleanupStringNew(animation);
                            break;
                        case "skill_visual_effect":
                            skillGrp.SkillVisualEffect = DatUtils.CleanupString(value);
                            break;
                        case "icon":
                            skillGrp.Icon = DatUtils.CleanupString(value);
                            break;
                        case "icon_panel":
                            skillGrp.IconPanel = DatUtils.CleanupString(value);
                            break;
                        case "debuff":
                            skillGrp.Debuff = int.Parse(value);
                            break;
                        case "resist_cast":
                            skillGrp.ResistCast = int.Parse(value);
                            break;
                        case "enchant_skill_level":
                            skillGrp.EnchantSkillLevel = int.Parse(value);
                            break;
                        case "enchant_icon":
                            skillGrp.EnchantIcon = DatUtils.CleanupString(value);
                            break;
                        case "hp_consume":
                            skillGrp.HpConsume = int.Parse(value);
                            break;
                        case "rumble_self":
                            skillGrp.RumbleSelf = int.Parse(value);
                            break;
                        case "rumble_target":
                            skillGrp.RumbleTarget = int.Parse(value);
                            break;
                    }
                }
                TryAdd(skillGrp);
            }
            
            Debug.Log($"Successfully imported {_skills.Count} skills(s)");
        }

    }

    private void TryAdd(Skillgrp skillGrp)
    {

        if (_skills.ContainsKey(skillGrp.Id) == true)
        {
            Dictionary<int, Skillgrp> dataGrp = _skills[skillGrp.Id];
            dataGrp.TryAdd(skillGrp.Level, skillGrp);
        }
        else
        {
            Dictionary<int, Skillgrp> dataGrp = CreateDict();
            AddDict(dataGrp, skillGrp);
            _skills.TryAdd(skillGrp.Id, dataGrp);
        }
    }

    private Dictionary<int, Skillgrp> CreateDict()
    {
        return  new Dictionary<int, Skillgrp>();
    }
    private void AddDict(Dictionary<int, Skillgrp> dataGrp , Skillgrp skillGrp)
    {
        dataGrp.TryAdd(skillGrp.Level, skillGrp);
    }

    public AnimationCombo GetAnimComboBySkillId(int skillId , int level)
    {
        if (_skills.ContainsKey(skillId))
        {
            Dictionary<int, Skillgrp> skills = _skills[skillId];
            Skillgrp grp = skills[level];
            if (_animationsCombo.ContainsKey(grp.Animation))
            {
                return _animationsCombo[grp.Animation];
            }
        }
        return null;
    }


    public void ReadAnimationCombo_Classic()
    {
        //_actionsInterlude = new Dictionary<int, ActionData>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/AnimationCombo_Classic.txt");

        if (!File.Exists(dataPath))
        {
            Debug.LogWarning("File not found: " + dataPath);
        }

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
                    string tag = ids[0];

                  if (!_animationsCombo.ContainsKey(tag))
                    {
                        string anim_name1 = РЎlearCombo(ids[1]);
                        string anim_name2 = РЎlearCombo(ids[2]);
                        string anim_name3 = РЎlearCombo(ids[3]);
                        var arr = new string[3] { anim_name1  , anim_name2 , anim_name3 };
                        string unk0 = ids[4];
                        var combo  = new AnimationCombo(tag, arr , unk0);
                        _animationsCombo.Add(tag , combo);
                    }
                }

                index++;
            }

        }

 
    }

    private string РЎlearCombo(string anim_name)
    {
        string anim_name1 = anim_name.Replace("a," , "");
        string anim_name2 = anim_name1.Replace("\\0", "");
        string anim_name3 = anim_name2.Replace("AnimName", "");
        return anim_name3;
    }
}
