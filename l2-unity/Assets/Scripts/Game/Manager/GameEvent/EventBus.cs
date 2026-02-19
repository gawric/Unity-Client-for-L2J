using System;
using UnityEngine;
using static UnityEditor.Progress;


public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }
    public event Action<Entity, int> OnLevelUp;
    public event Action<ItemInstance , int> OnEquipped;
    public event Action<ItemInstance, int> OnWeaponChanged;
    public event Action<ItemInstance , int> OnUnEquipped;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void Equipped(ItemInstance item , int objectId)
    {
        OnEquipped?.Invoke(item , objectId);
    }


    public void UnEquipped(ItemInstance item, int objectId)
    {
       OnUnEquipped?.Invoke(item , objectId);
    }

    public void LevelUp(Entity entity , int objectID)
    {
        if(entity != null)
        {
            OnLevelUp?.Invoke(entity, objectID);
            Debug.Log("EventBus>event LevelUp " + entity.name);
        }
    }
}