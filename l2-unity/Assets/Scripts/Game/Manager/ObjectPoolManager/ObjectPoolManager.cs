using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private Transform poolParent; // Родительский объект для пулов



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

        SetPoolLimit(ObjectType.Arrow , 25);
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

        Transform parentTag = GetParent(tag, pools);
        Queue<GameObject> prefabPool = poolDictionary[tag][prefab];

        int availableSpace = currentLimit - prefabPool.Count;
        int objectsToAdd = Mathf.Min(count, availableSpace);

        if (objectsToAdd <= 0)
        {
            Debug.Log($"Пул для {prefab.name} достиг максимального размера ({currentLimit}). Невозможно добавить {count} объектов.");
            return;
        }

        for (int i = 0; i < objectsToAdd; i++)
        {
            if (GetCreateCount(prefab) >= currentLimit) break;

            GameObject obj = CopyObject(prefab, parentTag, poolParent);
            obj.SetActive(false);
            prefabPool.Enqueue(obj);
            Plus1Create(prefab);
        }

        Debug.Log($"ObjectPoolManager->Добавлено {objectsToAdd} объектов в пул для {prefab.name}. Общее количество: {prefabPool.Count}/{currentLimit} Общий размер: {poolDictionary[ObjectType.Armor].Count}");
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

        if (objectPool.Count == 0)
        {
            GameObject newObj = Instantiate(prefab, poolParent);
            newObj.SetActive(false);
            objectPool.Enqueue(newObj);
            Plus1Create(prefab);
            Debug.LogError($"ObjectPoolManager->SpawnFromPool: Критическая ошибка. Object pooling перестал работать; все объекты теперь будут уничтожаться Unity и создаваться через Instantiate.");
            return newObj;
        }

        GameObject objectToSpawn = objectPool.Dequeue();
        return objectToSpawn;
    }

    public bool ReturnToPool(ObjectType tag, GameObject objectToReturn)
    {
        if (!ValidatePool(tag) || !FindMatchingPrefab(tag, objectToReturn, out GameObject prefab))
        {
            return false;
        }

        if (_maxSizePool <= 0)
        {
            return false;
        }

        if (!PrepareObjectForReturn(objectToReturn, tag))
        {
            return false;
        }

        return HandlePoolReturn(tag, prefab, objectToReturn, _maxSizePool);
    }

    private bool PrepareObjectForReturn(GameObject objectToReturn, ObjectType tag)
    {
        var parent = GetParent(tag, pools);
        if (parent == null)
        {
            return false;
        }

        objectToReturn.transform.SetParent(parent);
        ResetPosition(objectToReturn);
        objectToReturn.SetActive(false);
        return true;
    }

    private bool HandlePoolReturn(ObjectType tag, GameObject prefab, GameObject objectToReturn, int maxSize)
    {
        Queue<GameObject> objectPool = poolDictionary[tag][prefab];
        int currentLimit = objectTypePoolLimits[tag];

        if (objectPool.Count >= currentLimit)
        {
            Debug.LogError($"ObjectPoolManager->HandlePoolReturn: Объект уничтожен через Destroy Unity!");
            Destroy(objectToReturn);
        }
        else
        {
            objectPool.Enqueue(objectToReturn);
        }

        return true;
    }
}

public enum ObjectType
{
    Weapon,    // Оружие
    Armor,     // Броня
    Face,      // Лицо
    Arrow      // Стрела
}
