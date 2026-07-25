using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SkillAnimationDatabase
{
    private static readonly Dictionary<string, SkillClips> _raceCacheMMagic = new Dictionary<string, SkillClips>();
    private static readonly Dictionary<string, SkillClips> _raceCacheFDarkElf = new Dictionary<string, SkillClips>();
    private const string  FOLDER_MMagic = "Data/Animations/Magic/MMagic/Clips/";
    private const string  FOLDER_FDarkElf = "Data/Animations/DarkElf/FDarkElf/";
    private const string MMagic = "MMagic";
    private const string FDarkElf = "FDarkElf";
    public class SkillClips
    {
        private AnimationClip _clip;
        private string _name;
        private string _path;
        
        public SkillClips(string folder , string name)
        {
            string path = folder + name;

            _name = name;
            _path = path;
        }

        public void Load()
        {
            if(_clip == null)
            {
                _clip = Resources.Load<AnimationClip>(_path);
                if (_clip != null)
                {
                    Debug.Log($"SkillAnimationDatabase> Загрузился ролик анимации  в кэш имя {_name} его путь {_path}");
                }
                else
                {
                    Debug.LogWarning($"SkillAnimationDatabase> Не смогли загрузить в кэщ {_name} его путь {_path}");
                }

            }
        }

        public AnimationClip GetClip()
        {
            return _clip;
        }

        public string GetName()
        {
            return _name;
        }
    }


    public static void Initialize()
    {
        //MMagic Эти ролики берет для переиспользования (этот override аниматор)
        _raceCacheMMagic.Add("CastLong_" + MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_CastLong_MMagic"));
        _raceCacheMMagic.Add("CastMid_"+ MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_CastMid_MMagic"));
        _raceCacheMMagic.Add("CastEnd_"+ MMagic, new SkillClips (FOLDER_MMagic, "MMagic_M000_b.ao_CastEnd_MMagic"));
        _raceCacheMMagic.Add("MagicShot_" + MMagic, new SkillClips (FOLDER_MMagic, "MMagic_M000_b.ao_MagicShot_MMagic"));
        _raceCacheMMagic.Add("MagicNoTarget_" + MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_MagicNoTarget_MMagic"));
        _raceCacheMMagic.Add("MagicNotarget_" + MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_MagicNoTarget_MMagic"));
        _raceCacheMMagic.Add("MagicThrow_" + MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_MagicThrow_MMagic"));

        //эти ролики берет для замены на свои (т.е это оригинальный аниматор)
        //FDarkElf
        _raceCacheFDarkElf.Add("CastLong_" + FDarkElf, new SkillClips(FOLDER_FDarkElf, "FDarkElf_m001_b.ao_CastLong_FDarkElf"));
        _raceCacheFDarkElf.Add("CastMid_" + FDarkElf, new SkillClips(FOLDER_FDarkElf, "FDarkElf_m001_b.ao_CastMid_FDarkElf"));
        _raceCacheFDarkElf.Add("CastEnd_" + FDarkElf, new SkillClips(FOLDER_FDarkElf, "FDarkElf_m001_b.ao_CastEnd_FDarkElf"));
        _raceCacheFDarkElf.Add("MagicShot_" + FDarkElf, new SkillClips(FOLDER_FDarkElf, "FDarkElf_m001_b.ao_MagicShot_A_FDarkElf"));
        _raceCacheFDarkElf.Add("MagicThrow_" + FDarkElf, new SkillClips(FOLDER_FDarkElf, "FDarkElf_m001_b.ao_MagicShot_A_FDarkElf"));


    }

    public static void LoadRaceAnimations(string raceName)
    {
        if(MMagic == raceName)
        {
            foreach (var item in _raceCacheMMagic)
            {
                _raceCacheMMagic[item.Key].Load();
            }
        }
    }

    public static AnimationClip GetOverrideClip(string overrideAnimName, string raceName)
    {
        if (MMagic == raceName)
        {
            string key = overrideAnimName + "_" + raceName;
            if (_raceCacheMMagic.ContainsKey(key))
            {
                return _raceCacheMMagic[key].GetClip();
            }
        }
        return null;
    }

    public static string GetAnimationClipName(string trigerName, string raceName)
    {
        string slotTriggerName = ResolveOverrideSlotTriggerName(trigerName);
        if (FDarkElf == raceName)
        {
            string key = slotTriggerName + "_" + raceName;

            if (_raceCacheFDarkElf.ContainsKey(key))
            {

                return _raceCacheFDarkElf[key].GetName();
            }
        }
        return "";
    }

    /// <summary>
    /// Maps dedicated 2-phase animator triggers to base override slots (CastEnd / MagicShot).
    /// Long-cast triggers also map to the same slots because clip events use CastMid/CastEnd/MagicShot.
    /// </summary>
    public static string ResolveOverrideSlotTriggerName(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            return triggerName;
        }

        switch (triggerName)
        {
            case "CastEnd2P":
                return "CastEnd";
            case "MagicShot2P":
                return "MagicShot";
            case "CastMidLong":
                return "CastMid";
            case "CastEndLong":
                return "CastEnd";
            case "MagicShotLong":
                return "MagicShot";
            default:
                return triggerName;
        }
    }

    /// <summary>
    /// Имя из OnAnimationComplete на клипе (CastMid/CastEnd/MagicShot), не ключ override (CastLong/MagicNoTarget).
    /// </summary>
    public static string ResolveOverrideCompletionEventName(string triggerOrOverrideKey)
    {
        if (string.IsNullOrEmpty(triggerOrOverrideKey))
        {
            return triggerOrOverrideKey;
        }

        string slotName = ResolveOverrideSlotTriggerName(triggerOrOverrideKey);
        if (!string.Equals(slotName, triggerOrOverrideKey, StringComparison.Ordinal))
        {
            return slotName;
        }

        switch (triggerOrOverrideKey)
        {
            case "CastLong":
                return "CastMid";
            case "MagicNoTarget":
            case "MagicNotarget":
                return "MagicShot";
            default:
                return triggerOrOverrideKey;
        }
    }

}
