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
    private int _maxSizePool = 2;
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

        Transform parentTag = GetParent(tag , pools);
        Queue<GameObject> prefabPool = poolDictionary[tag][prefab];


        int availableSpace = _maxSizePool - prefabPool.Count;
        int objectsToAdd = Mathf.Min(count, availableSpace);

        if (objectsToAdd <= 0)
        {
            Debug.Log($"Pool for {prefab.name} has reached maximum size ({_maxSizePool}). Cannot add {count} objects.");
            return;
        }


        for (int i = 0; i < objectsToAdd; i++)
        {
            GameObject obj = CopyObject(prefab, parentTag, poolParent);
            obj.SetActive(false);
            prefabPool.Enqueue(obj);
        }

        Debug.Log($"ObjectPoolManager->Added {objectsToAdd} objects to pool for {prefab.name}. Total count: {prefabPool.Count}/{_maxSizePool}");
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
            GameObject newObj = Instantiate(prefab, poolParent);
            newObj.SetActive(false);
            objectPool.Enqueue(newObj);
            //Debug.Log($"ObjectPoolManager-> Created new {prefab.name} instance for pool {tag} (pool was empty)");
        }

  
        GameObject objectToSpawn = objectPool.Dequeue();
        //string spawnMaterialInfo = GetMaterialInfo(objectToSpawn);
        //Debug.Log($"ObjectPoolManager-> Spawned {objectToSpawn.name} from pool {tag}. Remaining objects: {objectPool.Count}. Material: {spawnMaterialInfo}");

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
            //Debug.Log($"Pool for {prefab.name} in {tag} has reached max size ({maxSize}). Destroying excess object.");
            Destroy(objectToReturn);
        }
        else
        {
            objectPool.Enqueue(objectToReturn);
            //Debug.Log($"Returned {objectToReturn.name} to pool {tag}. Current count: {objectPool.Count}/{maxSize}");
        }

        return true;
    }



 
    private string GetMaterialInfo(GameObject obj)
    {
        if (obj == null) return "";

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return " (No Renderer found)";

        if (renderer.materials.Length == 0) return " (No materials)";

        if (renderer.materials.Length == 1)
        {
            return $" (Material: {renderer.material.name})";
        }

        string materialsList = string.Join(", ", renderer.materials.Select(m => m.name));
        return $" (Materials: {materialsList})";
    }


}



public enum ObjectType
{
    Weapon,
    Armor,
    Face,
}


