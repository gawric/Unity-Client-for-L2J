using UnityEngine;

public sealed class ItemDropClickAreaService
{
    const float ClickRadiusMin = 0.05f;
    const float ClickPadMeters = 0.05f;

    readonly ItemDropLayerService _layers;

    public ItemDropClickAreaService(ItemDropLayerService layers)
    {
        _layers = layers;
    }

    public void Refresh(ItemEntity entity)
    {
        if (entity == null)
            return;
        EnsureClickCollider(entity);
        _layers.ApplyDropItemLayer(entity.gameObject);
    }

    public void SitOnGround(ItemEntity item)
    {
        if (item == null)
            return;
        Transform dropMesh = item.transform.Find("DropMesh");
        if (dropMesh != null)
            SitMeshOnGround(dropMesh.gameObject);
        else
            SitMeshOnGround(item.gameObject);
    }

    public void SitMeshOnGround(GameObject visual)
    {
        if (visual == null)
            return;

        MeshFilter filter = visual.GetComponentInChildren<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
            return;

        Bounds world = TransformAabb(filter.transform, mesh.bounds);
        Transform anchor = visual.transform.parent != null ? visual.transform.parent : visual.transform;
        visual.transform.position += Vector3.up * (anchor.position.y - world.min.y);
    }

    void EnsureClickCollider(ItemEntity entity)
    {
        Transform existing = entity.transform.Find("ClickArea");
        GameObject clickGo = existing != null ? existing.gameObject : new GameObject("ClickArea");
        if (existing == null)
            clickGo.transform.SetParent(entity.transform, false);

        SphereCollider sphere = clickGo.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = clickGo.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        FitClickArea(entity, clickGo.transform, sphere);
    }

    void FitClickArea(ItemEntity entity, Transform clickGo, SphereCollider sphere)
    {
        if (!TryGetDropVisualBounds(entity.transform, out Bounds worldBounds))
        {
            clickGo.localPosition = Vector3.zero;
            sphere.center = Vector3.zero;
            sphere.radius = WorldToLocalRadius(clickGo, ClickRadiusMin);
            return;
        }

        clickGo.localPosition = entity.transform.InverseTransformPoint(worldBounds.center);
        float visualRadius = Mathf.Max(worldBounds.extents.x, worldBounds.extents.y, worldBounds.extents.z);
        sphere.center = Vector3.zero;
        sphere.radius = WorldToLocalRadius(clickGo, visualRadius + ClickPadMeters);
    }

    static float WorldToLocalRadius(Transform clickGo, float worldRadius)
    {
        float lossy = Mathf.Max(0.001f, clickGo.lossyScale.x);
        return Mathf.Max(ClickRadiusMin, worldRadius) / lossy;
    }

    bool TryGetDropVisualBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        Transform dropMesh = root.Find("DropMesh");
        Transform measure = dropMesh != null ? dropMesh : root;
        bool any = false;

        MeshFilter[] filters = measure.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null || _layers.IsFxTransform(filter.transform))
                continue;
            Encapsulate(ref bounds, ref any, TransformAabb(filter.transform, filter.sharedMesh.bounds));
        }

        SkinnedMeshRenderer[] skins = measure.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            SkinnedMeshRenderer skin = skins[i];
            if (skin == null || skin.sharedMesh == null || _layers.IsFxTransform(skin.transform))
                continue;
            Encapsulate(ref bounds, ref any, TransformAabb(skin.transform, skin.sharedMesh.bounds));
        }

        return any;
    }

    static void Encapsulate(ref Bounds bounds, ref bool any, Bounds add)
    {
        if (!any)
        {
            bounds = add;
            any = true;
            return;
        }

        bounds.Encapsulate(add);
    }

    static Bounds TransformAabb(Transform t, Bounds localBounds)
    {
        Vector3 center = t.TransformPoint(localBounds.center);
        Vector3 ext = localBounds.extents;
        Vector3 axisX = t.TransformVector(new Vector3(ext.x, 0f, 0f));
        Vector3 axisY = t.TransformVector(new Vector3(0f, ext.y, 0f));
        Vector3 axisZ = t.TransformVector(new Vector3(0f, 0f, ext.z));
        Vector3 worldExt = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(center, worldExt * 2f);
    }
}
