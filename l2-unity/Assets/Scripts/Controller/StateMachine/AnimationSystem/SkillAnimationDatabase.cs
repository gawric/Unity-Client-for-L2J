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
        _raceCacheMMagic.Add("CastMid_"+ MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_CastMid_MMagic"));
        _raceCacheMMagic.Add("CastEnd_"+ MMagic, new SkillClips (FOLDER_MMagic, "MMagic_M000_b.ao_CastEnd_MMagic"));
        _raceCacheMMagic.Add("MagicShot_" + MMagic, new SkillClips (FOLDER_MMagic, "MMagic_M000_b.ao_MagicShot_MMagic"));
        _raceCacheMMagic.Add("MagicThrow_" + MMagic, new SkillClips(FOLDER_MMagic, "MMagic_M000_b.ao_MagicThrow_MMagic"));

        //эти ролики берет для замены на свои (т.е это оригинальный аниматор)
        //FDarkElf
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
        if (FDarkElf == raceName)
        {
            string key = trigerName + "_" + raceName;

            if (_raceCacheFDarkElf.ContainsKey(key))
            {

                return _raceCacheFDarkElf[key].GetName();
            }
        }
        return "";
    }

}
