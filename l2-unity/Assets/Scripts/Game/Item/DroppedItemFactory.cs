using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Builds the world representation of an item lying on the ground.
///
/// Retail Interlude renders drops with the meshes referenced by the "drop_texture" field of
/// *grp.dat (the "dropitems"/"dropitemstex" packages, already parsed into Abstractgrp.DropModel
/// and Abstractgrp.DropTexture). Those packages are not imported into Resources at all yet (no
/// sack, no coin, no scroll drop mesh), so the resolution order is:
///   1. weapon / shield  -> the mesh held in hand (Weapongrp.Model)
///   2. etc item         -> the mesh held in hand (EtcItemgrp.Model), arrows, torches, ...
///   3. everything else  -> a generic procedural sack, standing in for "dropitems.drop_sack_m00"
///      (armors are race dependent skinned meshes and need a rig, adena / potions / materials
///       have no held mesh at all - retail itself draws most of these as the same generic sack).
/// Once the dropitems package is imported, only <see cref="FindMeshPrefab"/> has to be changed.
/// </summary>
public static class DroppedItemFactory
{
    // Lift the drop slightly above the ground to avoid z-fighting with the terrain.
    private const float GROUND_OFFSET = 0.02f;
    // Longest side under which a model is considered to still be at its raw authoring scale.
    private const float RAW_MESH_SIZE = 0.05f;
    // Factor between the raw model scale and the in game one.
    private const float MESH_SCALE = 100f;
    // Longest side a drop may take once scaled.
    private const float MAX_DROP_SIZE = 1f;

    private static Material _sackMaterial;
    private static Material _tieMaterial;

    /// <summary>
    /// Instantiates the model of <paramref name="itemId"/> at <paramref name="position"/>.
    /// The returned object is never null, a generic sack is used when no mesh is known.
    /// </summary>
    public static GameObject Create(int itemId, Vector3 position, Transform parent)
    {
        GameObject prefab = GetPrefab(itemId);

        GameObject go = prefab != null
            ? CreateFromMesh(prefab, position)
            : CreateGenericSack(position);

        // The held-mesh prefabs and the primitives above have no collider of their own, so nothing
        // would ever be raycast-hit by ClickManager - hover tooltips and future pickup-on-click need
        // a trigger to find the item under the cursor.
        AddHoverCollider(go);

        if (parent != null)
        {
            go.transform.SetParent(parent, true);
        }

        return go;
    }

    /// <summary>
    /// Height of <paramref name="go"/> above its own pivot (which sits at ground level, see
    /// PlaceOnGround) - used to anchor the hover tooltip above the model instead of at its feet.
    /// </summary>
    public static float GetVisualHeight(GameObject go)
    {
        return TryGetWorldBounds(go, out Bounds bounds) ? bounds.max.y - go.transform.position.y : 0f;
    }

    public static string GetItemName(int itemId)
    {
        ItemName itemName = ItemNameTable.Instance.GetItemName(itemId);
        return itemName != null && !string.IsNullOrEmpty(itemName.Name) ? itemName.Name : $"Item {itemId}";
    }

    private static GameObject GetPrefab(int itemId)
    {
        try
        {
            return FindMeshPrefab(itemId);
        }
        catch (Exception e)
        {
            // A missing model must not swallow the drop, the generic sack is used instead.
            Debug.LogWarning($"DroppedItemFactory - Can't resolve the mesh of item {itemId} - {e.Message}");
            return null;
        }
    }

    private static GameObject FindMeshPrefab(int itemId)
    {
        Weapon weapon = ItemTable.Instance.GetWeapon(itemId);
        if (weapon != null && HasMesh(weapon.Weapongrp.Model))
        {
            GameObject weaponModel = ModelTable.Instance.GetWeapon(weapon.Weapongrp.Model);
            if (weaponModel != null)
            {
                return weaponModel;
            }
        }

        EtcItem etcItem = ItemTable.Instance.GetEtcItem(itemId);
        if (etcItem != null && HasMesh(etcItem.EtcItemgrp.Model))
        {
            GameObject etcModel = ModelTable.Instance.GetEtcItem(etcItem.EtcItemgrp.Model);
            if (etcModel != null)
            {
                return etcModel;
            }
        }

        return null;
    }

    // mesh={[None]} is written for every item that is not held in hand.
    private static bool HasMesh(string model)
    {
        return !string.IsNullOrEmpty(model)
            && !model.Equals("None", StringComparison.OrdinalIgnoreCase)
            && model.Contains(".");
    }

    private static GameObject CreateFromMesh(GameObject prefab, Vector3 position)
    {
        GameObject go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        go.SetActive(true);

        NormalizeScale(go);

        // The models are authored lying along their local X axis, the identity rotation already
        // lays them flat. Only the facing is randomized so that several drops are not aligned.
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        PlaceOnGround(go, position.y);

        return go;
    }

    /// <summary>
    /// Held models keep the raw scale of the mesh on their root, the in game size comes from the
    /// bone they are attached to. A few prefabs have that factor baked in already, so the scale is
    /// derived from the instantiated bounds instead of being assumed.
    /// </summary>
    private static void NormalizeScale(GameObject go)
    {
        if (!TryGetWorldBounds(go, out Bounds bounds))
        {
            return;
        }

        float size = MaxDimension(bounds);
        if (size <= Mathf.Epsilon)
        {
            return;
        }

        if (size < RAW_MESH_SIZE)
        {
            go.transform.localScale *= MESH_SCALE;
            size *= MESH_SCALE;
        }

        // Keep the biggest models (polearms, banners) from covering the whole ground around them.
        if (size > MAX_DROP_SIZE)
        {
            go.transform.localScale *= MAX_DROP_SIZE / size;
        }
    }

    private static void PlaceOnGround(GameObject go, float groundY)
    {
        if (!TryGetWorldBounds(go, out Bounds bounds))
        {
            return;
        }

        go.transform.position += Vector3.up * (groundY + GROUND_OFFSET - bounds.min.y);
    }

    private static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static float MaxDimension(Bounds bounds)
    {
        return Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
    }

    /// <summary>
    /// A sphere ignores the item's rotation, unlike a box it never has to be re-fitted to the
    /// local axes of a Y-rotated drop.
    /// </summary>
    private static void AddHoverCollider(GameObject go)
    {
        if (!TryGetWorldBounds(go, out Bounds bounds))
        {
            return;
        }

        // SphereCollider.radius is a local-space value that Unity scales back up by the transform's
        // lossyScale - held-mesh drops sit on a root whose scale is ~100, so a radius taken directly
        // from the (world-space) bounds would balloon into a many-meters-wide hitbox once applied.
        float scale = Mathf.Max(go.transform.lossyScale.x, go.transform.lossyScale.y, go.transform.lossyScale.z);
        float worldRadius = Mathf.Max(MaxDimension(bounds) * 0.5f, 0.05f);

        SphereCollider collider = go.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.center = go.transform.InverseTransformPoint(bounds.center);
        collider.radius = worldRadius / Mathf.Max(scale, 0.0001f);
    }

    /// <summary>
    /// Stand-in for retail's "dropitems.drop_sack_m00" - a lumpy cloth sack tied at the neck,
    /// built from primitives since no mesh asset is imported for it. Used for anything without
    /// its own held mesh: armor, adena, potions, scrolls, materials.
    /// </summary>
    private static GameObject CreateGenericSack(Vector3 position)
    {
        GameObject root = new GameObject("GenericSack");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Main body plus a couple of off-center lumps so the silhouette isn't a perfect sphere.
        CreateSackPart(root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.10f, 0f), new Vector3(0.16f, 0.13f, 0.16f), GetSackMaterial());
        CreateSackPart(root.transform, PrimitiveType.Sphere, new Vector3(0.05f, 0.075f, 0.02f), new Vector3(0.10f, 0.09f, 0.11f), GetSackMaterial());
        CreateSackPart(root.transform, PrimitiveType.Sphere, new Vector3(-0.04f, 0.085f, -0.03f), new Vector3(0.09f, 0.08f, 0.10f), GetSackMaterial());

        // Gathered neck and the cord tying it shut.
        CreateSackPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.185f, 0f), new Vector3(0.06f, 0.05f, 0.06f), GetSackMaterial());
        CreateSackPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.165f, 0f), new Vector3(0.07f, 0.012f, 0.07f), GetTieMaterial());

        PlaceOnGround(root, position.y);

        return root;
    }

    private static void CreateSackPart(Transform parent, PrimitiveType primitive, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = primitive.ToString();

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material GetSackMaterial()
    {
        if (_sackMaterial == null)
        {
            _sackMaterial = CreateOpaqueMaterial("DroppedItemSack", new Color(0.42f, 0.31f, 0.19f));
        }

        return _sackMaterial;
    }

    private static Material GetTieMaterial()
    {
        if (_tieMaterial == null)
        {
            _tieMaterial = CreateOpaqueMaterial("DroppedItemSackTie", new Color(0.24f, 0.16f, 0.09f));
        }

        return _tieMaterial;
    }

    private static Material CreateOpaqueMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }
}
