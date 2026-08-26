using UnityEngine;

public sealed class ItemDropWeaponAligner
{
    readonly ItemDropGrpCatalog _grp;

    public ItemDropWeaponAligner(ItemDropGrpCatalog grp)
    {
        _grp = grp;
    }

    public bool AlignBladeAlong(Transform root, Vector3 worldDir)
    {
        if (worldDir.sqrMagnitude < 1e-12f)
            return false;
        if (!TryGetBladeMarkers(root, out Transform bladeBase, out Transform bladeTip))
            return false;

        Vector3 blade = bladeTip.position - bladeBase.position;
        if (blade.sqrMagnitude < 1e-12f)
            return false;

        root.rotation = Quaternion.FromToRotation(blade.normalized, worldDir.normalized) * root.rotation;
        return true;
    }

    public bool AlignBladeTipDown(Transform root, Vector3 throwDir)
    {
        Vector3 desired = Vector3.down;
        Vector3 flat = new Vector3(throwDir.x, 0f, throwDir.z);
        if (flat.sqrMagnitude > 0.0001f)
            desired = (Vector3.down + flat.normalized * 0.45f).normalized;
        return AlignBladeAlong(root, desired);
    }

    public void PlantStuckInGround(ItemEntity item, Vector3 landPos)
    {
        if (item == null)
            return;

        item.transform.position = landPos;

        if (TryGetBladeMarkers(item.transform, out Transform bladeBase, out Transform bladeTip))
        {
            float bladeLen = Vector3.Distance(bladeBase.position, bladeTip.position);
            float bury = Mathf.Clamp(bladeLen * 0.22f, 0.04f, 0.22f);
            float dy = (landPos.y - bury) - bladeTip.position.y;
            item.transform.position += Vector3.up * dy;
            return;
        }

        Renderer renderer = item.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return;

        Bounds bounds = renderer.bounds;
        float height = Mathf.Max(bounds.size.y, 0.01f);
        float fallbackBury = Mathf.Clamp(height * 0.22f, height * 0.08f, height * 0.35f);
        float fallbackDy = landPos.y - bounds.min.y - fallbackBury;
        item.transform.position += Vector3.up * fallbackDy;
    }

    public void ScaleWeapon(GameObject visual, int itemId)
    {
        if (visual == null)
            return;

        float current = 0f;
        string currentSrc = "none";
        if (TryGetBladeMarkers(visual.transform, out Transform bladeBase, out Transform bladeTip))
        {
            current = Vector3.Distance(bladeBase.position, bladeTip.position);
            currentSrc = $"blade:{bladeBase.name}->{bladeTip.name}";
        }
        if (current < 0.0001f)
        {
            current = LongestMeshAxis(visual);
            currentSrc = "meshAxis";
        }
        if (current < 0.0001f)
        {
            Debug.LogWarning($"[DropItemScale] SCALE skip itemId={itemId} current=0 src={currentSrc}");
            return;
        }

        float equipped = TryEquippedWeaponWorldLength(itemId, out string equippedSrc);
        float defaultLen = DefaultWeaponWorldLength(itemId);
        float target = equipped > 0.05f ? equipped * 0.8f : defaultLen;
        target /= 3f;
        target *= 1.3f;
        Vector3 scaleBefore = visual.transform.localScale;
        float mul = target / current;
        bool skip = current >= target * 0.55f;
        if (!skip)
        {
            mul = Mathf.Clamp(mul, 0.25f, 64f);
            visual.transform.localScale *= mul;
        }

        Debug.Log(
            $"[DropItemScale] SCALE itemId={itemId} prefab='{visual.name}' " +
            $"current={current:F3}({currentSrc}) equipped={equipped:F3}({equippedSrc}) " +
            $"default={defaultLen:F3} target={target:F3} skip={skip} mul={mul:F2} " +
            $"scale {scaleBefore} → {visual.transform.localScale} " +
            $"worldAxis={LongestMeshAxis(visual):F3}");
    }

    public static void ScaleProp(GameObject visual, float targetMeters)
    {
        if (visual == null || targetMeters < 0.01f)
            return;

        float current = LongestMeshAxis(visual);
        if (current < 0.0001f)
            return;
        if (current >= targetMeters * 0.55f)
            return;

        visual.transform.localScale *= targetMeters / current;
    }

    /// <summary>
    /// UKX drop bottles lie on X/Z. Rotate the longest mesh axis to world up
    /// with the pivot-near end as the bottom (neck up).
    /// </summary>
    public static void StandUpright(GameObject visual)
    {
        if (visual == null)
            return;

        MeshFilter filter = visual.GetComponentInChildren<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
            return;

        Bounds bounds = mesh.bounds;
        Vector3 size = bounds.size;
        int axis = 1;
        if (size.x >= size.y && size.x >= size.z)
            axis = 0;
        else if (size.z >= size.y && size.z >= size.x)
            axis = 2;

        Vector3 localUp = axis == 0 ? Vector3.right : axis == 2 ? Vector3.forward : Vector3.up;
        if (Mathf.Abs(bounds.max[axis]) < Mathf.Abs(bounds.min[axis]))
            localUp = -localUp;

        visual.transform.localRotation = Quaternion.FromToRotation(localUp, Vector3.up) *
                                         visual.transform.localRotation;
    }

    float DefaultWeaponWorldLength(int itemId)
    {
        Abstractgrp grp = _grp.ResolveGrp(itemId);
        int dropHeightUu = grp != null && grp.DropHeight > 0 ? grp.DropHeight : 12;
        float fromHeight = dropHeightUu * VectorUtils.L2UuToUnity * 4f * 0.8f;
        return Mathf.Clamp(fromHeight, 0.44f, 1.0f);
    }

    static float TryEquippedWeaponWorldLength(int itemId, out string source)
    {
        source = "none";
        PlayerEntity player = PlayerEntity.Instance;
        if (player == null)
            return 0f;

        Gear gear = player.GetComponentInChildren<Gear>(true);
        if (gear == null)
            return 0f;

        Transform bone = gear.GetTransformRightHandBone();
        if (bone == null)
            return 0f;

        string idToken = itemId.ToString();
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);
            if (child.name.IndexOf(idToken, System.StringComparison.Ordinal) < 0)
                continue;
            float len = LongestMeshAxis(child.gameObject);
            if (len > 0.05f)
            {
                source = $"equippedChild:'{child.name}'";
                return len;
            }
        }

        source = "notEquipped";
        return 0f;
    }

    static bool TryGetBladeMarkers(Transform root, out Transform bladeBase, out Transform bladeTip)
    {
        bladeBase = null;
        bladeTip = null;
        if (root == null)
            return false;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string name = all[i].name;
            if (IsBladeTipName(name))
                bladeTip = all[i];
            else if (IsBladeBaseName(name))
                bladeBase = all[i];
        }

        return bladeBase != null && bladeTip != null && bladeBase != bladeTip;
    }

    static bool IsBladeTipName(string name)
    {
        return name.EndsWith("Tip", System.StringComparison.OrdinalIgnoreCase) ||
               name.IndexOf("_Tip", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsBladeBaseName(string name)
    {
        return name.EndsWith("Base", System.StringComparison.OrdinalIgnoreCase) ||
               name.IndexOf("_Base", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static float LongestMeshAxis(GameObject go)
    {
        float longest = 0f;
        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;
            Vector3 size = Vector3.Scale(mesh.bounds.size, filters[i].transform.lossyScale);
            longest = Mathf.Max(longest, size.x, size.y, size.z);
        }

        SkinnedMeshRenderer[] skins = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            Mesh mesh = skins[i].sharedMesh;
            if (mesh == null)
                continue;
            Vector3 size = Vector3.Scale(mesh.bounds.size, skins[i].transform.lossyScale);
            longest = Mathf.Max(longest, size.x, size.y, size.z);
        }

        return longest;
    }
}
