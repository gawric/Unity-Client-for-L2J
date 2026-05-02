using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ObjectPoolManager;

public abstract class  AbstractPoolManager : MonoBehaviour
{
    protected Dictionary<ObjectType, Dictionary<GameObject, Queue<GameObject>>> poolDictionary;
    protected Dictionary<ObjectType, GameObject> tagToPrefabMap;
    protected Dictionary<GameObject, int> createdInstancesTracker;
    protected Dictionary<ObjectType, int> objectTypePoolLimits = new Dictionary<ObjectType, int>();

    protected void SetupPoolHierarchy(List<Pool> pools , Transform poolParent)
    {
        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogWarning($"Prefab for tag {pool.tag} is not set!");
                continue;
            }


            if (!poolDictionary.ContainsKey(pool.tag))
            {
                poolDictionary[pool.tag] = new Dictionary<GameObject, Queue<GameObject>>();
                tagToPrefabMap[pool.tag] = pool.prefab;
            }


            if (!poolDictionary[pool.tag].ContainsKey(pool.prefab))
            {
                poolDictionary[pool.tag][pool.prefab] = new Queue<GameObject>();
            }


            Queue<GameObject> prefabPool = poolDictionary[pool.tag][pool.prefab];
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, poolParent);
                pool.usePrefab = obj;
                obj.SetActive(false);
                //prefabPool.Enqueue(obj);
            }
        }
    }

    public void SetPoolLimit(ObjectType type, int maxSize)
    {
        if (maxSize <= 0)
        {
            Debug.LogWarning($"Лимит пула для {type} должен быть больше 0!");
            return;
        }

        objectTypePoolLimits[type] = maxSize;
        Debug.Log($"Установлен лимит пула для {type} равный {maxSize}");
    }

    protected void ValidateAndCreateDictionary(ObjectType tag, GameObject prefab)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            poolDictionary[tag] = new Dictionary<GameObject, Queue<GameObject>>();
            tagToPrefabMap[tag] = prefab;
        }


    }

    protected void ValidAndCreateQueue(ObjectType tag, GameObject prefab)
    {
        if (!poolDictionary[tag].ContainsKey(prefab))
        {
            poolDictionary[tag][prefab] = new Queue<GameObject>();
        }
    }

    protected bool ValidatePool(ObjectType tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return false;
        }
        return true;
    }

    protected bool FindMatchingPrefab(ObjectType tag, GameObject objectToReturn, out GameObject prefab)
    {
        prefab = null;
        Dictionary<GameObject, Queue<GameObject>> prefabPools = poolDictionary[tag];

        foreach (var kvp in prefabPools)
        {
            if (objectToReturn.name.StartsWith(kvp.Key.name))
            {
                prefab = kvp.Key;
                return true;
            }
        }

        Debug.LogWarning($"Could not find matching prefab for object {objectToReturn.name} in pool {tag}");
        return false;
    }

    protected void ResetPosition(GameObject objectToReturn)
    {
        objectToReturn.transform.localPosition = Vector3.zero;
        objectToReturn.transform.localRotation = Quaternion.identity;
        objectToReturn.transform.localScale = Vector3.one;
    }

    protected GameObject CopyObject(GameObject prefab, Transform parentTag, Transform poolParent)
    {
        if (parentTag == null)
        {
            return Instantiate(prefab, poolParent);
        }
        else
        {
            return Instantiate(prefab, parentTag);
        }
        //return (parentTag) ? Instantiate(prefab, parentTag) : Instantiate(prefab, poolParent);
    }
    protected Transform GetParent(ObjectType tag , List<Pool> pools)
    {
        for (int b = 0; b < pools.Count; b++)
        {
            Pool pollParent = pools[b];

            if (pollParent.tag == tag)
            {
                return pollParent.usePrefab.transform;
            }
        }

        return null;
    }

    protected void Plus1Create(GameObject prefab)
    {
        if (createdInstancesTracker.ContainsKey(prefab))
        {
            int countCreate = createdInstancesTracker[prefab];
            createdInstancesTracker[prefab] = countCreate + 1;
        }
        else
        {
            createdInstancesTracker.Add(prefab , 1);
        }
    }

    protected int GetCreateCount(GameObject prefab)
    {

        if (createdInstancesTracker.ContainsKey(prefab))
        {
            return createdInstancesTracker[prefab];
        }

        return 0;
    }


}
