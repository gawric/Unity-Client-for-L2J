using UnityEngine;

public sealed class ItemSpawner
{
    readonly ItemDropPresentationService _dropPresentation;
    readonly ItemDropVisualService _visuals;

    public ItemSpawner(ItemDropPresentationService dropPresentation, ItemDropVisualService visuals)
    {
        _dropPresentation = dropPresentation;
        _visuals = visuals;
    }

    public ItemEntity Spawn(
        int objectId,
        int itemId,
        int count,
        bool stackable,
        Vector3 unityPos,
        int dropperCharObjId,
        IWorldSpawnContext world)
    {
        if (world == null)
            return null;

        GameObject go = new GameObject("Item_" + itemId + "_" + objectId);
        Transform parent = world.ItemsContainer;
        if (parent != null)
            go.transform.SetParent(parent, false);

        unityPos.y = world.GetGroundHeight(unityPos);

        ItemEntity entity = go.AddComponent<ItemEntity>();
        entity.Setup(objectId, itemId, count, stackable, unityPos);
        _visuals.AttachVisual(entity, itemId, dropperCharObjId);
        go.AddComponent<ItemPickupMotion>();

        world.RegisterItem(entity);
        _dropPresentation?.PlayDrop(entity, unityPos, dropperCharObjId);
        return entity;
    }
}
