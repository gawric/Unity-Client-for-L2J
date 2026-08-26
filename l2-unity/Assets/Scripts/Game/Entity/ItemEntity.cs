using UnityEngine;

/// <summary>Ground item entity (EntityType.Item). Click → ClickAction for server pickup.</summary>
public sealed class ItemEntity : Entity
{
    [SerializeField] private int _itemId;
    [SerializeField] private int _count = 1;
    [SerializeField] private bool _stackable;

    public int ItemId => _itemId;
    public int Count => _count;
    public bool Stackable => _stackable;

    public void Setup(int objectId, int itemId, int count, bool stackable, Vector3 unityPos)
    {
        _itemId = itemId;
        _count = count;
        _stackable = stackable;

        if (Identity == null)
            Identity = new EntityIdentity();

        Identity.EntityType = EntityType.Item;
        Identity.Id = objectId;
        Identity.Name = ResolveItemName(itemId);
        ApplyWorldPos(unityPos);
        EntityLoaded = true;
    }

    public void UpdateGround(Vector3 unityPos, int count, bool stackable)
    {
        _count = count;
        _stackable = stackable;
        ApplyWorldPos(unityPos);
    }

    void ApplyWorldPos(Vector3 unityPos)
    {
        if (Identity != null)
            Identity.Position = unityPos;
        transform.position = unityPos;
    }

    static string ResolveItemName(int itemId)
    {
        ItemName name = ItemNameTable.Instance != null
            ? ItemNameTable.Instance.GetItemName(itemId)
            : null;
        return name != null && !string.IsNullOrEmpty(name.Name) ? name.Name : ("Item_" + itemId);
    }
}
