using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : AbstractPoolManager, IPoolManager
{
    [System.Serializable]
    public class Pool
    {
        public ObjectType tag;
        public GameObject prefab;
        public int size;
        public GameObject usePrefab;
    }

    public List<Pool> pools;
    // Минимальный размер пула >= 3
    private int _maxSizePool = 3;
    [SerializeField] private Transform poolParent;

    #region Singleton
    public static IPoolManager Instance;

    private void Awake()
    {
        Instance = this;
    }
    #endregion

    void Start()
    {
        poolDictionary = new Dictionary<ObjectType, Dictionary<GameObject, Queue<GameObject>>>();
        tagToPrefabMap = new Dictionary<ObjectType, GameObject>();
        createdInstancesTracker = new Dictionary<GameObject, int>();
        objectTypePoolLimits = new Dictionary<ObjectType, int>();

        foreach (ObjectType type in System.Enum.GetValues(typeof(ObjectType)))
        {
            objectTypePoolLimits[type] = _maxSizePool;
        }

        SetPoolLimit(ObjectType.Arrow, 25);
        // Shared meshes (e.g. many a_guard_*) need headroom — limit=2 destroyed returns on leave-city.
        SetPoolLimit(ObjectType.Npc, 32);
        // Same headroom as Npc/guards; can tune later.
        SetPoolLimit(ObjectType.Monster, 32);
        SetupPoolHierarchy(pools, poolParent);
        Debug.Log($"Создание пула объектов успешно. Размер: {poolDictionary.Count}");
    }

    public void AddPrefabToPool(ObjectType tag, GameObject prefab, int count = 2)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Префаб не может быть null!");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning("Количество должно быть больше 0!");
            return;
        }

        ValidateAndCreateDictionary(tag, prefab);
        ValidAndCreateQueue(tag, prefab);

        int currentLimit = objectTypePoolLimits[tag];

        if (GetCreateCount(prefab) >= currentLimit)
        {
            return;
        }

        Transform parentTag = EnsurePoolSlot(tag, pools, poolParent);
        Queue<GameObject> prefabPool = poolDictionary[tag][prefab];

        int availableSpace = currentLimit - prefabPool.Count;
        int objectsToAdd = Mathf.Min(count, availableSpace);

        if (objectsToAdd <= 0)
        {
            Debug.Log(
                $"Пул для {prefab.name} достиг максимального размера ({currentLimit}). " +
                $"Невозможно добавить {count} объектов.");
            return;
        }

        for (int i = 0; i < objectsToAdd; i++)
        {
            if (GetCreateCount(prefab) >= currentLimit)
            {
                break;
            }

            GameObject obj = CopyObject(tag, prefab, parentTag, poolParent);
            obj.SetActive(false);
            prefabPool.Enqueue(obj);
            Plus1Create(prefab);
        }

        Debug.Log(
            $"[ObjectPool] Add {objectsToAdd} → {tag}/{prefab.name} " +
            $"queue={prefabPool.Count}/{currentLimit} created={GetCreateCount(prefab)}");
    }

    public GameObject SpawnFromPool(ObjectType tag, GameObject specificPrefab = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Пул с тегом {tag} не существует.");
            return null;
        }

        Dictionary<GameObject, Queue<GameObject>> prefabPools = poolDictionary[tag];
        GameObject prefab = specificPrefab ?? tagToPrefabMap[tag];

        if (!prefabPools.ContainsKey(prefab))
        {
            Debug.LogWarning($"Префаб не найден в пуле {tag}");
            return null;
        }

        Queue<GameObject> objectPool = prefabPools[prefab];
        int queueBefore = objectPool.Count;

        if (queueBefore == 0)
        {
            // Grow by one and hand out — do NOT enqueue (old bug: same GO in queue + in use).
            Transform parentTag = EnsurePoolSlot(tag, pools, poolParent);
            GameObject newObj = CopyObject(tag, prefab, parentTag, poolParent);
            newObj.SetActive(false);
            Plus1Create(prefab);
            Debug.Log(
                $"[ObjectPool] Spawn GREW {tag}/{prefab.name} " +
                $"queue=0/{objectTypePoolLimits[tag]} created={GetCreateCount(prefab)}");
            return newObj;
        }

        GameObject objectToSpawn = objectPool.Dequeue();
        EnsurePooledMeta(objectToSpawn, tag, prefab);
        Debug.Log(
            $"[ObjectPool] Spawn REUSED {tag}/{prefab.name} " +
            $"queue={objectPool.Count}/{objectTypePoolLimits[tag]}");
        return objectToSpawn;
    }

    public bool ReturnToPool(ObjectType tag, GameObject objectToReturn)
    {
        if (objectToReturn == null)
        {
            Debug.LogWarning($"[ObjectPool] Return REJECT tag={tag} go=null");
            return false;
        }

        if (!ValidatePool(tag))
        {
            Debug.LogWarning($"[ObjectPool] Return REJECT id-go={objectToReturn.name} no pool tag={tag}");
            return false;
        }

        if (!FindMatchingPrefab(tag, objectToReturn, out GameObject prefab))
        {
            Debug.LogWarning(
                $"[ObjectPool] Return REJECT FindMatchingPrefab FAIL go={objectToReturn.name} tag={tag}");
            return false;
        }

        if (_maxSizePool <= 0)
        {
            Debug.LogWarning($"[ObjectPool] Return REJECT maxSizePool<=0 go={objectToReturn.name}");
            return false;
        }

        if (!PrepareObjectForReturn(objectToReturn, tag))
        {
            Debug.LogWarning($"[ObjectPool] Return REJECT Prepare FAIL go={objectToReturn.name}");
            return false;
        }

        return HandlePoolReturn(tag, prefab, objectToReturn, _maxSizePool);
    }

    private bool PrepareObjectForReturn(GameObject objectToReturn, ObjectType tag)
    {
        Transform parent = EnsurePoolSlot(tag, pools, poolParent);
        if (parent == null)
        {
            return false;
        }

        objectToReturn.transform.SetParent(parent);
        ResetPosition(objectToReturn);

        PooledInstance pooled = objectToReturn.GetComponent<PooledInstance>();
        if (pooled != null &&
            (tag == ObjectType.Npc || tag == ObjectType.Monster))
        {
            // DeadManager fades via material instances — restore shared mats for next spawn.
            pooled.RestoreSharedMaterials();
        }

        objectToReturn.SetActive(false);
        return true;
    }

    private bool HandlePoolReturn(ObjectType tag, GameObject prefab, GameObject objectToReturn, int maxSize)
    {
        Queue<GameObject> objectPool = poolDictionary[tag][prefab];
        int currentLimit = objectTypePoolLimits[tag];

        if (objectPool.Count >= currentLimit)
        {
            Debug.LogWarning(
                $"[ObjectPool] Return DROP(destroy) {tag}/{objectToReturn.name} " +
                $"queueFull={objectPool.Count}/{currentLimit}");
            Minus1Create(prefab);
            Destroy(objectToReturn);
        }
        else
        {
            objectPool.Enqueue(objectToReturn);
            Debug.Log(
                $"[ObjectPool] Return OK {tag}/{prefab.name} " +
                $"queue={objectPool.Count}/{currentLimit}");
        }

        return true;
    }
}

public enum ObjectType
{
    Weapon,
    Armor,
    Face,
    Arrow,
    /// <summary>Whole city NPC prefab.</summary>
    Npc,
    /// <summary>Whole monster prefab (leave-range + post-death corpse cleanup).</summary>
    Monster
}
