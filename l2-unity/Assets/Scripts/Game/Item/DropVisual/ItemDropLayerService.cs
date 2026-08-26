using UnityEngine;

public sealed class ItemDropLayerService
{
    public const string DropItemLayerName = "DropItem";
    int _dropItemLayer = int.MinValue;

    public int Layer
    {
        get
        {
            if (_dropItemLayer == int.MinValue)
            {
                _dropItemLayer = LayerMask.NameToLayer(DropItemLayerName);
                if (_dropItemLayer < 0)
                    _dropItemLayer = LayerMask.NameToLayer("EntityClick");
            }

            return _dropItemLayer;
        }
    }

    public LayerMask Mask
    {
        get
        {
            int layer = Layer;
            return layer >= 0 ? (1 << layer) : 0;
        }
    }

    public void ApplyDropItemLayer(GameObject root)
    {
        int layer = Layer;
        if (layer < 0 || root == null)
            return;
        SetLayerSkipFx(root, layer);
        DisablePhysicalDropMeshColliders(root.transform);
    }

    public void ApplyIgnoreRaycastLayer(GameObject root)
    {
        if (root == null)
            return;
        int layer = LayerMask.NameToLayer("Ignore Raycast");
        if (layer < 0)
            layer = LayerMask.NameToLayer("SkillEffect");
        if (layer < 0)
            return;
        SetLayerAll(root, layer);
    }

    public bool IsFxTransform(Transform t)
    {
        while (t != null)
        {
            if (IsFxNode(t.name))
                return true;
            t = t.parent;
        }

        return false;
    }

    static bool IsFxNode(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.IndexOf("HitPointProxy", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("e_u056", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("DropGroundGlow", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Coin_RuntimeClone", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void SetLayerSkipFx(GameObject go, int layer)
    {
        if (go == null)
            return;
        if (IsFxNode(go.name))
        {
            ApplyIgnoreRaycastLayer(go);
            return;
        }

        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerSkipFx(t.GetChild(i).gameObject, layer);
    }

    static void SetLayerAll(GameObject go, int layer)
    {
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerAll(t.GetChild(i).gameObject, layer);
    }

    static void DisablePhysicalDropMeshColliders(Transform root)
    {
        Transform dropMesh = root.Find("DropMesh");
        if (dropMesh == null)
            return;

        Collider[] colliders = dropMesh.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }
}
