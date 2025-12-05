using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ObjectPoolManager : AbstractPoolManager , IPoolManager
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
    //mix size pool >=3
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

        SetupPoolHierarchy(pools , poolParent);

        Debug.Log($"Create Pool Objects Success Size: {poolDictionary.Count}");
    }


    public void AddPrefabToPool(ObjectType tag, GameObject prefab, int count = 2)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab cannot be null!");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning("Count must be greater than 0!");
            return;
        }



        ValidateAndCreateDictionary(tag, prefab);

        ValidAndCreateQueue(tag, prefab);

        if (GetCreateCount(prefab) >= _maxSizePool)
        {
            return;
        }

        Transform parentTag = GetParent(tag , pools);
        Queue<GameObject> prefabPool = poolDictionary[tag][prefab];

        int availableSpace = _maxSizePool - prefabPool.Count;
        int objectsToAdd = Mathf.Min(count, availableSpace);

        if (objectsToAdd <= 0)
        {
            Debug.Log($"Pool for {prefab.name} has reached maximum size ({_maxSizePool}). Cannot add {count} objects.");
            return;
        }

       // Debug.Log($"Prefab_name for {prefab.name} pool_name   has reached maximum size ({prefabPool.ToString()}). Cannot add {count} objects.");

        for (int i = 0; i < objectsToAdd; i++)
        {
            if (GetCreateCount(prefab) >= _maxSizePool) break;

            GameObject obj = CopyObject(prefab, parentTag, poolParent);
            obj.SetActive(false);
            prefabPool.Enqueue(obj);
            Plus1Create(prefab);


           // if (prefab.name.IndexOf("MFighter_m001_u") > -1)
           // {
            //    Debug.LogWarning("Test add 1 " + i + " size: " + GetCreateCount(prefab));
           // }

        }

        Debug.Log($"ObjectPoolManager->Added {objectsToAdd} objects to pool for {prefab.name}. Total count: {prefabPool.Count}/{_maxSizePool} All Size: {poolDictionary[ObjectType.Armor].Count}");
    }

    public GameObject SpawnFromPool(ObjectType tag, GameObject specificPrefab = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        Dictionary<GameObject, Queue<GameObject>> prefabPools = poolDictionary[tag];
        GameObject prefab = specificPrefab ?? tagToPrefabMap[tag];

        if (!prefabPools.ContainsKey(prefab))
        {
            Debug.LogWarning($"Prefab not found in pool {tag}");
            return null;
        }

        Queue<GameObject> objectPool = prefabPools[prefab];


        if (objectPool.Count == 0)
        {
            //Transform parentTag = GetParent(tag, pools);
            GameObject newObj = Instantiate(prefab, poolParent);
            newObj.SetActive(false);
            objectPool.Enqueue(newObj);
            Plus1Create(prefab);
            Debug.LogError($"ObjectPoolManager->SpawnFromPool: Critical bug. Object pooling has stopped working; all objects will now be destroyed by Unity and created via Instentian.");
            return newObj;
        }



        GameObject objectToSpawn = objectPool.Dequeue();

        //if (prefab.name.IndexOf("MFighter_m001_u") > -1)
        //{
        //    Debug.LogWarning("Test get 1 " + objectPool.Count);
        //}


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
        var parent = GetParent(tag , pools);
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

        if (objectPool.Count >= maxSize)
        {
            //if (prefab.name.IndexOf("MFighter_m001_u") > -1)
            //{
            //    Debug.LogWarning("Test return destroy 1 " + objectPool.Count);
            //}
            Debug.LogError($"ObjectPoolManager->HandlePoolReturn: Destroyed the object via Destroy Unity!");
            Destroy(objectToReturn);
        }
        else
        {
            objectPool.Enqueue(objectToReturn);

           // if (prefab.name.IndexOf("MFighter_m001_u") > -1)
           // {
           //     Debug.LogWarning("Test return 1 " + objectPool.Count);
           // }

        }

        return true;
    }

}



public enum ObjectType
{
    Weapon,
    Armor,
    Face,
}


