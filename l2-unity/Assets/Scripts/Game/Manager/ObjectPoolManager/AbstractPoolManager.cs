using System.Collections.Generic;
using UnityEngine;
using static ObjectPoolManager;

public abstract class AbstractPoolManager : MonoBehaviour
{
    protected Dictionary<ObjectType, Dictionary<GameObject, Queue<GameObject>>> poolDictionary;
    protected Dictionary<ObjectType, GameObject> tagToPrefabMap;
    protected Dictionary<GameObject, int> createdInstancesTracker;
    protected Dictionary<ObjectType, int> objectTypePoolLimits = new Dictionary<ObjectType, int>();

    protected void SetupPoolHierarchy(List<Pool> pools, Transform poolParent)
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
        if (objectToReturn == null)
        {
            return false;
        }

        PooledInstance pooled = objectToReturn.GetComponent<PooledInstance>();
        if (pooled != null &&
            pooled.SourcePrefab != null &&
            pooled.Type == tag &&
            poolDictionary.ContainsKey(tag) &&
            poolDictionary[tag].ContainsKey(pooled.SourcePrefab))
        {
            prefab = pooled.SourcePrefab;
            return true;
        }

        Dictionary<GameObject, Queue<GameObject>> prefabPools = poolDictionary[tag];
        foreach (KeyValuePair<GameObject, Queue<GameObject>> kvp in prefabPools)
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

        PooledInstance pooled = objectToReturn.GetComponent<PooledInstance>();
        if (pooled != null &&
            (pooled.Type == ObjectType.Npc || pooled.Type == ObjectType.Monster))
        {
            // L2 character meshes often ship with non-1 scale — don't flatten them.
            objectToReturn.transform.localScale = pooled.OriginalLocalScale;
        }
        else
        {
            objectToReturn.transform.localScale = Vector3.one;
        }
    }

    protected GameObject CopyObject(
        ObjectType tag,
        GameObject prefab,
        Transform parentTag,
        Transform poolParent)
    {
        bool prefabWasActive = prefab != null && prefab.activeSelf;
        if (prefabWasActive)
        {
            prefab.SetActive(false);
        }

        GameObject obj = parentTag != null
            ? Instantiate(prefab, parentTag)
            : Instantiate(prefab, poolParent);

        if (prefabWasActive)
        {
            prefab.SetActive(true);
        }

        EnsurePooledMeta(obj, tag, prefab);
        SanitizeCharacterControllerStepOffset(obj);
        return obj;
    }

    /// <summary>Legacy gear path — Weapon/Armor/Arrow; preserves old signature.</summary>
    protected GameObject CopyObject(GameObject prefab, Transform parentTag, Transform poolParent)
    {
        return CopyObject(ObjectType.Weapon, prefab, parentTag, poolParent);
    }

    protected void EnsurePooledMeta(GameObject instance, ObjectType tag, GameObject sourcePrefab)
    {
        if (instance == null)
        {
            return;
        }

        PooledInstance pooled = instance.GetComponent<PooledInstance>();
        if (pooled == null)
        {
            pooled = instance.AddComponent<PooledInstance>();
            pooled.OriginalLocalScale = instance.transform.localScale;
        }

        pooled.Type = tag;
        pooled.SourcePrefab = sourcePrefab;
        if (tag == ObjectType.Npc || tag == ObjectType.Monster)
        {
            pooled.CaptureSharedMaterials();
        }
    }

    protected static void SanitizeCharacterControllerStepOffset(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterController cc = controllers[i];
            if (cc != null)
            {
                cc.stepOffset = 0f;
            }
        }
    }

    /// <summary>
    /// Scene pools list may not define Npc yet — create a hidden slot under poolParent.
    /// </summary>
    protected Transform EnsurePoolSlot(ObjectType tag, List<Pool> pools, Transform poolParent)
    {
        Transform existing = GetParent(tag, pools);
        if (existing != null)
        {
            return existing;
        }

        if (pools == null)
        {
            return poolParent;
        }

        GameObject slot = new GameObject($"_PoolSlot_{tag}");
        slot.SetActive(false);
        if (poolParent != null)
        {
            slot.transform.SetParent(poolParent);
        }

        pools.Add(new Pool
        {
            tag = tag,
            prefab = null,
            size = 0,
            usePrefab = slot
        });

        return slot.transform;
    }

    protected Transform GetParent(ObjectType tag, List<Pool> pools)
    {
        if (pools == null)
        {
            return null;
        }

        for (int b = 0; b < pools.Count; b++)
        {
            Pool pollParent = pools[b];
            if (pollParent.tag == tag && pollParent.usePrefab != null)
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
            createdInstancesTracker.Add(prefab, 1);
        }
    }

    protected void Minus1Create(GameObject prefab)
    {
        if (prefab == null || !createdInstancesTracker.ContainsKey(prefab))
        {
            return;
        }

        int count = createdInstancesTracker[prefab] - 1;
        if (count <= 0)
        {
            createdInstancesTracker.Remove(prefab);
        }
        else
        {
            createdInstancesTracker[prefab] = count;
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
