using UnityEngine;

public interface IPoolManager 
{
    public void AddPrefabToPool(ObjectType tag, GameObject prefab, int count = 2);
    public GameObject SpawnFromPool(ObjectType tag, GameObject specificPrefab = null);
    public bool ReturnToPool(ObjectType tag, GameObject objectToReturn);
}
