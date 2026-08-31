using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves LineageEffect.* names to deco prefabs under Resources/Data/Effects/deco.
/// Default: one L2Particle (deco/u_npc_id_buff or deco/u_npc_id_buff/u_npc_id_buff).
/// Skill composites are ignored. Optional authored splits use _feet / _oh in the piece name.
/// </summary>
public sealed class NpcDecoCatalog
{
    public const string ResourcesFolder = "Data/Effects/deco";

    public bool TryLoadPieces(string decoEffectName, List<NpcDecoPiece> pieces)
    {
        pieces.Clear();
        if (string.IsNullOrWhiteSpace(decoEffectName))
            return false;

        string shortName = ShortName(decoEffectName);
        string folder = ResourcesFolder + "/" + shortName;
        Object[] folderAssets = Resources.LoadAll(folder, typeof(GameObject));
        if (folderAssets != null && folderAssets.Length > 0)
        {
            for (int i = 0; i < folderAssets.Length; i++)
            {
                GameObject go = folderAssets[i] as GameObject;
                if (go == null)
                    continue;

                if (go.GetComponent<CompositePrefabEffect>() != null)
                    continue;

                BaseEffect effect = go.GetComponent<BaseEffect>();
                if (effect == null)
                    continue;

                pieces.Add(new NpcDecoPiece
                {
                    Prefab = effect,
                    Attach = NpcDecoAttachment.FromPieceName(go.name),
                    Label = go.name
                });
            }

            if (pieces.Count > 0)
                return true;
        }

        BaseEffect single = LoadAt(ResourcesFolder + "/" + shortName);
        if (single == null)
            single = LoadAt(folder + "/" + shortName);
        if (single == null)
            return false;

        pieces.Add(new NpcDecoPiece
        {
            Prefab = single,
            Attach = NpcDecoAttachment.FromPieceName(single.name),
            Label = single.name
        });
        return true;
    }

    public static string ShortName(string decoEffectName)
    {
        if (string.IsNullOrWhiteSpace(decoEffectName))
            return string.Empty;

        int dot = decoEffectName.LastIndexOf('.');
        return dot >= 0 && dot < decoEffectName.Length - 1
            ? decoEffectName.Substring(dot + 1)
            : decoEffectName;
    }

    static BaseEffect LoadAt(string resourcesPath)
    {
        BaseEffect typed = Resources.Load<BaseEffect>(resourcesPath);
        if (typed != null)
            return typed;

        GameObject go = Resources.Load<GameObject>(resourcesPath);
        return go != null ? go.GetComponent<BaseEffect>() : null;
    }
}
