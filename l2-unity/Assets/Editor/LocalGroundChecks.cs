using System;
using UnityEngine;

public static class LocalGroundChecks
{
    public static void Run()
    {
        var room = new GameObject("GroundSnapRoomFixture") { hideFlags = HideFlags.HideAndDontSave };
        var mesh = new Mesh { name = "FloorAndRoofInOneCollider" };
        try
        {
            room.transform.position = new Vector3(100000, 0, 100000);
            mesh.vertices = new[] {
                new Vector3(-1,0,-1), new Vector3(-1,0,1), new Vector3(1,0,1), new Vector3(1,0,-1),
                new Vector3(-1,3,-1), new Vector3(-1,3,1), new Vector3(1,3,1), new Vector3(1,3,-1)
            };
            mesh.triangles = new[] { 0,1,2, 0,2,3, 4,5,6, 4,6,7 };
            mesh.RecalculateNormals();
            room.AddComponent<MeshCollider>().sharedMesh = mesh;
            Physics.SyncTransforms();
            foreach (float height in new[] { 0.5f, -0.5f, 3.5f })
            {
                Vector3 position = room.transform.position + Vector3.up * height;
                float expected = height > 3 ? 3 : 0;
                if (!GroundSnapHelper.TrySnapToGround(position, out Vector3 snapped, 1 << 0) || Mathf.Abs(snapped.y - expected) > 0.001f)
                    throw new InvalidOperationException("Indoor ground snap must find the near floor behind the collider roof; start=" + height);
            }
            if (GroundSnapHelper.TrySnapToGround(room.transform.position + Vector3.down * 2, out _, 1 << 0))
                throw new InvalidOperationException("Ground snap must keep the maximum allowed lift");
            Debug.Log("[LocalGroundChecks] Indoor multi-surface collider, upper floor and lift limit passed");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(room);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
