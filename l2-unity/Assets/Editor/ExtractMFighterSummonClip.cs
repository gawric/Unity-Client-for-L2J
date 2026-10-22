using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies the pet-summon MagicThrow take out of MFighter_m000_b_ao_full.fbx.
/// Unity.exe -batchmode -nographics -quit -executeMethod ExtractMFighterSummonClip.ExtractSummonCli
/// </summary>
public static class ExtractMFighterSummonClip
{
    const string FbxPath =
        "Assets/Resources/Data/Animations/Fighter/MFighter/Models/MFighter_m000_b_ao_full.fbx";
    const string DestPath =
        "Assets/Resources/Data/Animations/Fighter/MFighter/Clips/MFighter_m000_b.ao_MagicThrow_MFighter.anim";

    public static void ExtractSummonCli()
    {
        try
        {
            if (!Extract())
                EditorApplication.Exit(1);
        }
        catch (Exception ex)
        {
            Debug.LogError("[SUMMON_CLIP] " + ex);
            EditorApplication.Exit(1);
        }
    }

    static bool Extract()
    {
        AnimationClip source = FindMagicThrow(AssetDatabase.LoadAllAssetsAtPath(FbxPath));
        if (source == null)
        {
            Debug.Log("[SUMMON_CLIP] MagicThrow is not a sub-asset yet. Importing that take only.");
            source = ImportMagicThrowTake();
        }

        if (source == null)
        {
            Debug.LogError("[SUMMON_CLIP] MagicThrow_MFighter was not found in " + FbxPath);
            return false;
        }

        var copy = new AnimationClip();
        EditorUtility.CopySerialized(source, copy);
        copy.name = "MFighter_m000_b.ao_MagicThrow_MFighter";

        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(DestPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(DestPath);

        AssetDatabase.CreateAsset(copy, DestPath);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "[SUMMON_CLIP] wrote " + DestPath +
            " length=" + copy.length.ToString("F3") +
            " source=" + source.name);
        return copy.length > 0.01f;
    }

    static AnimationClip ImportMagicThrowTake()
    {
        var importer = UnityEditor.AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("[SUMMON_CLIP] ModelImporter missing");
            return null;
        }

        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        ModelImporterClipAnimation take = null;
        if (defaults != null)
        {
            for (int i = 0; i < defaults.Length; i++)
            {
                if (IsBaseMagicThrow(defaults[i].name))
                {
                    take = defaults[i];
                    break;
                }
            }
        }

        if (take == null)
        {
            int shown = 0;
            if (defaults != null)
            {
                for (int i = 0; i < defaults.Length && shown < 30; i++)
                {
                    if (defaults[i].name.IndexOf("MagicThrow", StringComparison.Ordinal) < 0 &&
                        defaults[i].name.IndexOf("Cast", StringComparison.Ordinal) < 0)
                        continue;
                    Debug.Log("[SUMMON_CLIP] take " + defaults[i].name);
                    shown++;
                }
            }
            Debug.LogError("[SUMMON_CLIP] no MagicThrow take among " + (defaults != null ? defaults.Length : 0));
            return null;
        }

        ModelImporterClipAnimation[] previous = importer.clipAnimations;
        importer.clipAnimations = new[] { take };
        importer.SaveAndReimport();
        AnimationClip clip = FindMagicThrow(AssetDatabase.LoadAllAssetsAtPath(FbxPath));
        importer.clipAnimations = previous ?? Array.Empty<ModelImporterClipAnimation>();
        importer.SaveAndReimport();
        return clip;
    }

    static AnimationClip FindMagicThrow(UnityEngine.Object[] assets)
    {
        if (assets == null)
            return null;

        int clips = 0;
        AnimationClip found = null;
        for (int i = 0; i < assets.Length; i++)
        {
            var clip = assets[i] as AnimationClip;
            if (clip == null)
                continue;
            clips++;
            if (IsBaseMagicThrow(clip.name))
                found = clip;
        }

        Debug.Log("[SUMMON_CLIP] fbx clips=" + clips + " magicThrow=" + (found != null ? found.name : "none"));
        return found;
    }

    static bool IsBaseMagicThrow(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name == "MagicThrow_MFighter" ||
               name.EndsWith("|MagicThrow_MFighter", StringComparison.Ordinal) ||
               name.EndsWith(".ao_MagicThrow_MFighter", StringComparison.Ordinal);
    }
}
