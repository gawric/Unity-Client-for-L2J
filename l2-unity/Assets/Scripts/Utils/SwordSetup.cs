using UnityEngine;

public class SwordSetup : MonoBehaviour
{

    public Transform swordBase;
    public Transform swordTip;

    public void SetupPoints()
    {
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        // Parent markers to the mesh transform and write mesh-local bounds there.
        // Parenting to weapon root while using mesh-local coords under-spans the blade.
        Transform meshXf = meshFilter.transform;
        swordBase = CreateOrGetPoint(meshXf, "Sword_Tip");
        swordTip = CreateOrGetPoint(meshXf, "Sword_Base");

        Bounds bounds = meshFilter.sharedMesh.bounds;
        float sizeX = bounds.size.x;
        float sizeY = bounds.size.y;
        float sizeZ = bounds.size.z;

        Vector3 minPoint;
        Vector3 maxPoint;
        if (sizeX > sizeY && sizeX > sizeZ)
        {
            minPoint = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z);
            maxPoint = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
            Debug.Log("Определена ориентация: Горизонтально (X)");
        }
        else if (sizeZ > sizeX && sizeZ > sizeY)
        {
            minPoint = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
            maxPoint = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);
            Debug.Log("Определена ориентация: Горизонтально (Z)");
        }
        else
        {
            minPoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            maxPoint = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            Debug.Log("Определена ориентация: Вертикально (Y)");
        }

        // Historical naming swap kept: GO "Sword_Tip" = hilt end, "Sword_Base" = tip end.
        swordBase.localPosition = minPoint;
        swordTip.localPosition = maxPoint;
    }

    private Transform CreateOrGetPoint(Transform parent, string pointName)
    {
        Transform existing = parent.Find(pointName);
        if (existing == null)
        {
            existing = transform.Find(pointName);
        }

        if (existing != null)
        {
            existing.SetParent(parent, false);
            return existing;
        }

        GameObject newPoint = new GameObject(pointName);
        newPoint.transform.SetParent(parent, false);
        return newPoint.transform;
    }
}
