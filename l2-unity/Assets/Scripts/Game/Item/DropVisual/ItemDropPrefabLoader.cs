using UnityEngine;

public sealed class ItemDropPrefabLoader
{
    const string CoinPileModel = "LineageEffectsStaticmeshes.coin01";
    const string CoinSingleModel = "LineageEffectsStaticmeshes.coin00";
    const string PotionFallbackModel = "LineageEffectsStaticmeshes.etcpotion00";

    readonly ItemDropGrpCatalog _grp;
    readonly ModelTable _models;

    public ItemDropPrefabLoader(ItemDropGrpCatalog grp, ModelTable models)
    {
        _grp = grp;
        _models = models;
    }

    public GameObject Resolve(int itemId)
    {
        ModelTable models = _models != null ? _models : ModelTable.Instance;
        Abstractgrp grp = _grp.ResolveGrp(itemId);
        if (grp != null && !string.IsNullOrEmpty(grp.DropModel) &&
            !grp.DropModel.Equals("None", System.StringComparison.OrdinalIgnoreCase))
        {
            GameObject drop = models.GetOrLoadModel(grp.DropModel);
            if (drop != null)
                return drop;

            GameObject coinFallback = LoadCoinDropFallback(models, grp.DropModel);
            if (coinFallback != null)
                return coinFallback;

            GameObject propFallback = LoadDropPropFallback(models, grp.DropModel);
            if (propFallback != null)
                return propFallback;
        }

        string equipModel = _grp.ResolveEquipModel(grp);
        if (!string.IsNullOrEmpty(equipModel))
        {
            GameObject equip = models.GetOrLoadModel(equipModel);
            if (equip != null)
                return equip;
        }

        if (_grp.IsHerb(itemId))
            return models.GetOrLoadModel(PotionFallbackModel);

        return null;
    }

    public static bool IsCoinPrefab(GameObject prefab)
    {
        string name = prefab.name;
        return name.IndexOf("coin00", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("coin01", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsFxDropPropPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return prefab.name.IndexOf("etcpotion", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsDropItemsPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        string name = prefab.name;
        if (name.IndexOf("coin00", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("coin01", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        return name.StartsWith("drop_", System.StringComparison.OrdinalIgnoreCase);
    }

    static GameObject LoadDropPropFallback(ModelTable models, string dropModel)
    {
        if (string.IsNullOrEmpty(dropModel))
            return null;

        string lower = dropModel.ToLowerInvariant();
        if (lower.IndexOf("dropitems", System.StringComparison.Ordinal) < 0)
            return null;

        if (lower.IndexOf("mfighter", System.StringComparison.Ordinal) >= 0 ||
            lower.IndexOf("ffighter", System.StringComparison.Ordinal) >= 0 ||
            lower.IndexOf("mmagic", System.StringComparison.Ordinal) >= 0 ||
            lower.IndexOf("fmagic", System.StringComparison.Ordinal) >= 0 ||
            lower.IndexOf("mdarkelf", System.StringComparison.Ordinal) >= 0 ||
            lower.IndexOf("fdarkelf", System.StringComparison.Ordinal) >= 0)
            return null;

        return models.GetOrLoadModel(PotionFallbackModel);
    }

    static GameObject LoadCoinDropFallback(ModelTable models, string dropModel)
    {
        if (!ItemDropGrpCatalog.IsCoinDropModel(dropModel))
            return null;

        GameObject pile = models.GetOrLoadModel(CoinPileModel);
        if (pile != null)
            return pile;

        return models.GetOrLoadModel(CoinSingleModel);
    }
}
