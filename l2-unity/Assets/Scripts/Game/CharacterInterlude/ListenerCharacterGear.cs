using UnityEngine;
using System.Collections.Generic;

public class ListenerCharacterGear : MonoBehaviour
{

    public static ListenerCharacterGear Instance { get; private set; }


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

    private void OnEnable()
    {
    
        EventBus.Instance.OnEquipped += HandleItemEquipped;
        EventBus.Instance.OnUnEquipped += HandleItemUnequipped;
    }

    private void OnDisable()
    {
     
        EventBus.Instance.OnEquipped -= HandleItemEquipped;
        EventBus.Instance.OnUnEquipped -= HandleItemUnequipped;
    }

    private void HandleItemEquipped(ItemInstance item , int objectId)
    {
        Entity entity = World.Instance.GetEntityNoLockSync(objectId);
        entity?.EquipAndDetermineType(item, objectId);

    }

    private void HandleItemUnequipped(ItemInstance item, int objectId)
    {
        Entity entity = World.Instance.GetEntityNoLockSync(objectId);
        entity?.UnequipAndDetermineType(item);
    }

    



}
