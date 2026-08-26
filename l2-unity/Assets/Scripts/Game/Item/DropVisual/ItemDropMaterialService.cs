using UnityEngine;

public sealed class ItemDropMaterialService
{
    const string CoinTex0Resource = "Data/SysTextures/LineageEffectsTextures/cbui24";
    const string CoinTex1Resource = "Data/SysTextures/LineageEffectsTextures/cbui25";
    const string DropMaskedShaderName = "L2/Items/DropMeshMasked";

    readonly ItemTable _items;

    public ItemDropMaterialService(ItemTable items)
    {
        _items = items;
    }

    public void ApplyCoin(GameObject visual)
    {
        Texture2D tex0 = Resources.Load<Texture2D>(CoinTex0Resource);
        Texture2D tex1 = Resources.Load<Texture2D>(CoinTex1Resource);
        ApplyDropMaskedTextures(visual, tex0, tex1);
    }

    public void ApplyDropItems(GameObject visual, int itemId, Abstractgrp grp)
    {
        Material source = LoadGrpMaterial(grp != null ? grp.DropTexture : null);
        if (source == null)
            source = LoadArmorDropMaterial(itemId, grp);
        if (source != null)
        {
            ApplyClonedMaterials(visual, source);
            return;
        }

        Texture2D albedo = LoadGrpAlbedo(grp != null ? grp.DropTexture : null);
        if (albedo == null)
            albedo = LoadArmorDropAlbedo(itemId, grp);
        if (albedo == null)
            albedo = LoadItemIcon(grp);
        ApplyUrpLit(visual, albedo);
    }

    public void ApplyPropFallback(GameObject visual, Abstractgrp grp)
    {
        Texture2D icon = LoadItemIcon(grp);
        if (icon == null)
            icon = Resources.Load<Texture2D>(CoinTex0Resource);
        ApplyUrpLit(visual, icon);
    }

    static void ApplyDropMaskedTextures(GameObject visual, Texture2D tex0, Texture2D tex1)
    {
        if (visual == null)
            return;

        Shader shader = Shader.Find(DropMaskedShaderName);
        if (shader == null || tex0 == null)
            return;

        MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            MeshRenderer renderer = renderers[r];
            int slots = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 1;
            if (slots < 1)
                slots = 1;

            Material[] mats = new Material[slots];
            for (int i = 0; i < slots; i++)
            {
                mats[i] = new Material(shader);
                mats[i].name = "DropCoin_" + i;
                mats[i].SetTexture("_MainTex", i == 0 || tex1 == null ? tex0 : tex1);
                mats[i].SetTextureScale("_MainTex", Vector2.one);
                mats[i].SetTextureOffset("_MainTex", Vector2.zero);
                mats[i].SetVector("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
                mats[i].renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            renderer.sharedMaterials = mats;
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        }
    }

    static void ApplyUrpLit(GameObject visual, Texture2D albedo)
    {
        if (visual == null || albedo == null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            return;

        Material source = new Material(shader);
        source.name = "DropItemLit";
        if (source.HasProperty("_BaseMap"))
            source.SetTexture("_BaseMap", albedo);
        if (source.HasProperty("_MainTex"))
            source.SetTexture("_MainTex", albedo);
        if (source.HasProperty("_BaseColor"))
            source.SetColor("_BaseColor", Color.white);
        if (source.HasProperty("_Color"))
            source.SetColor("_Color", Color.white);
        if (source.HasProperty("_Cull"))
            source.SetFloat("_Cull", 0f);
        ApplyClonedMaterials(visual, source);
        Object.Destroy(source);
    }

    static void ApplyClonedMaterials(GameObject visual, Material source)
    {
        if (visual == null || source == null)
            return;

        MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            MeshRenderer renderer = renderers[r];
            int slots = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 1;
            if (slots < 1)
                slots = 1;

            Material[] mats = new Material[slots];
            for (int i = 0; i < slots; i++)
            {
                mats[i] = new Material(source);
                mats[i].name = source.name + "_" + i;
                if (mats[i].HasProperty("_Cull"))
                    mats[i].SetFloat("_Cull", 0f);
            }

            renderer.sharedMaterials = mats;
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        }
    }

    Texture2D LoadArmorDropAlbedo(int itemId, Abstractgrp grp)
    {
        Armorgrp armor = ResolveArmorgrp(itemId, grp);
        if (armor != null && armor.FirstTexture != null)
        {
            int magic = (int)CharacterRaceAnimation.MMagic;
            if (magic >= 0 && magic < armor.FirstTexture.Length)
            {
                Texture2D magicTex = LoadGrpAlbedo(armor.FirstTexture[magic]);
                if (magicTex != null)
                    return magicTex;
            }

            for (int i = 0; i < armor.FirstTexture.Length; i++)
            {
                Texture2D tex = LoadGrpAlbedo(armor.FirstTexture[i]);
                if (tex != null)
                    return tex;
            }
        }

        Texture2D dropTex = LoadGrpAlbedo(grp != null ? grp.DropTexture : null);
        if (dropTex != null)
            return dropTex;
        return LoadItemIcon(grp);
    }

    Material LoadArmorDropMaterial(int itemId, Abstractgrp grp)
    {
        Armorgrp armor = ResolveArmorgrp(itemId, grp);
        if (armor == null || armor.FirstTexture == null)
            return LoadGrpMaterial(grp != null ? grp.DropTexture : null);

        int magic = (int)CharacterRaceAnimation.MMagic;
        if (magic >= 0 && magic < armor.FirstTexture.Length)
        {
            Material mat = LoadGrpMaterial(armor.FirstTexture[magic]);
            if (mat != null)
                return mat;
        }

        for (int i = 0; i < armor.FirstTexture.Length; i++)
        {
            Material mat = LoadGrpMaterial(armor.FirstTexture[i]);
            if (mat != null)
                return mat;
        }

        return LoadGrpMaterial(grp != null ? grp.DropTexture : null);
    }

    Armorgrp ResolveArmorgrp(int itemId, Abstractgrp grp)
    {
        Armorgrp armor = grp as Armorgrp;
        if (armor != null)
            return armor;

        ItemTable items = _items != null ? _items : ItemTable.Instance;
        if (items == null)
            return null;
        Armor item = items.GetArmor(itemId);
        return item != null ? item.Armorgrp : null;
    }

    static Texture2D LoadGrpAlbedo(string grpName)
    {
        if (string.IsNullOrEmpty(grpName) ||
            grpName.Equals("None", System.StringComparison.OrdinalIgnoreCase))
            return null;

        string[] parts = grpName.Split('.');
        string pkg = parts.Length >= 2 ? parts[0] : string.Empty;
        string asset = parts.Length >= 2 ? parts[1] : parts[0];
        if (string.IsNullOrEmpty(asset))
            return null;

        string[] folders =
        {
            string.IsNullOrEmpty(pkg) ? null : "Data/SysTextures/" + pkg + "/",
            "Data/SysTextures/DropItemsTex/",
            "Data/SysTextures/MMagic/",
            "Data/SysTextures/MFighter/"
        };

        string[] names = BuildAlbedoNames(asset);
        for (int i = 0; i < folders.Length; i++)
        {
            if (string.IsNullOrEmpty(folders[i]))
                continue;
            for (int n = 0; n < names.Length; n++)
            {
                Texture2D tex = Resources.Load<Texture2D>(folders[i] + names[n]);
                if (tex != null)
                    return tex;
            }
        }

        return null;
    }

    static string[] BuildAlbedoNames(string asset)
    {
        var names = new System.Collections.Generic.List<string>(6);
        names.Add(asset);
        names.Add(asset + "_sh");
        names.Add(asset + "_sp");
        names.Add(asset + "_ori");
        if (asset.EndsWith("_t00", System.StringComparison.OrdinalIgnoreCase))
        {
            string stem = asset.Substring(0, asset.Length - 4);
            names.Add(stem);
            names.Add(stem + "_sh");
            names.Add(stem + "_ori");
            names.Add(stem + "_sp");
        }
        return names.ToArray();
    }

    static Material LoadGrpMaterial(string grpName)
    {
        if (string.IsNullOrEmpty(grpName) ||
            grpName.Equals("None", System.StringComparison.OrdinalIgnoreCase))
            return null;

        string[] parts = grpName.Split('.');
        string pkg = parts.Length >= 2 ? parts[0] : string.Empty;
        string asset = parts.Length >= 2 ? parts[1] : parts[0];
        if (string.IsNullOrEmpty(asset))
            return null;

        if (!string.IsNullOrEmpty(pkg))
        {
            Material mat = Resources.Load<Material>("Data/SysTextures/" + pkg + "/Materials/" + asset);
            if (mat != null)
                return mat;
        }

        Material dropTex = Resources.Load<Material>("Data/SysTextures/DropItemsTex/Materials/" + asset);
        if (dropTex != null)
            return dropTex;

        Material magic = Resources.Load<Material>("Data/SysTextures/MMagic/Materials/" + asset);
        if (magic != null)
            return magic;
        return Resources.Load<Material>("Data/SysTextures/MFighter/Materials/" + asset);
    }

    static Texture2D LoadItemIcon(Abstractgrp grp)
    {
        if (grp == null || string.IsNullOrEmpty(grp.Icon))
            return null;

        string name = grp.Icon;
        int dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1)
            name = name.Substring(dot + 1);

        Texture2D tex = Resources.Load<Texture2D>("Data/SysTextures/Icon/" + name);
        if (tex != null)
            return tex;
        return Resources.Load<Texture2D>("Data/SysTextures/Icon/" + name + " 1");
    }
}
