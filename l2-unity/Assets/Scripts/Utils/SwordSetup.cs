using UnityEngine;

public class SwordSetup : MonoBehaviour
{

    public Transform swordBase;
    public Transform swordTip;

    public void SetupPoints()
    {
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null) return;

        swordBase = CreateOrGetPoint("Sword_Tip");
        swordTip = CreateOrGetPoint("Sword_Base");

        Bounds bounds = meshFilter.sharedMesh.bounds;

    
        float sizeX = bounds.size.x;
        float sizeY = bounds.size.y;
        float sizeZ = bounds.size.z;

        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;


        if (sizeX > sizeY && sizeX > sizeZ)
        {
        
            minPoint = new Vector3(bounds.min.x, 0, 0);
            maxPoint = new Vector3(bounds.max.x, 0, 0);
            Debug.Log("Определена ориентация: Горизонтально (X)");
        }
        else if (sizeZ > sizeX && sizeZ > sizeY)
        {

            minPoint = new Vector3(0, 0, bounds.min.z);
            maxPoint = new Vector3(0, 0, bounds.max.z);
            Debug.Log("Определена ориентация: Горизонтально (Z)");
        }
        else
        {
  
            minPoint = new Vector3(0, bounds.min.y, 0);
            maxPoint = new Vector3(0, bounds.max.y, 0);
            Debug.Log("Определена ориентация: Вертикально (Y)");
        }

        swordBase.localPosition = minPoint;
        swordTip.localPosition = maxPoint;
    }

    private Transform CreateOrGetPoint(string pointName)
    {
        Transform existing = transform.Find(pointName);
        if (existing != null) return existing;

        GameObject newPoint = new GameObject(pointName);
        newPoint.transform.SetParent(this.transform);
        return newPoint.transform;
    }
}
