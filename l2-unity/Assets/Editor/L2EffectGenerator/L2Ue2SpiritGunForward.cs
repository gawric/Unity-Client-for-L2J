#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UE2 mesh particles fly along local +X, and the effect shader treats imported
/// mesh X as that UE forward (Unity Y is UE up). spirit_gun00 comes from the
/// PSK to Blender FBX port with its nose pointing backward, so the tail leads.
/// Turn it 180 degrees around up. A reimport of the same FBX keeps the fix.
/// </summary>
public sealed class L2Ue2SpiritGunForward : AssetPostprocessor
{
    const string MeshSuffix = "/LineageEffectsStaticmeshes/spirit_gun00.fbx";

    void OnPostprocessModel(GameObject root)
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.EndsWith(MeshSuffix))
            return;

        var turned = new HashSet<Mesh>();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
            TurnAroundUp(filters[i] != null ? filters[i].sharedMesh : null, turned);

        SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
            TurnAroundUp(skinned[i] != null ? skinned[i].sharedMesh : null, turned);
    }

    static void TurnAroundUp(Mesh mesh, HashSet<Mesh> turned)
    {
        if (mesh == null || !turned.Add(mesh))
            return;

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            vertices[i] = new Vector3(-v.x, v.y, -v.z);
        }

        mesh.vertices = vertices;

        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            Vector3 n = normals[i];
            normals[i] = new Vector3(-n.x, n.y, -n.z);
        }

        if (normals.Length == vertices.Length)
            mesh.normals = normals;

        Vector4[] tangents = mesh.tangents;
        for (int i = 0; i < tangents.Length; i++)
        {
            Vector4 t = tangents[i];
            tangents[i] = new Vector4(-t.x, t.y, -t.z, t.w);
        }

        if (tangents.Length == vertices.Length)
            mesh.tangents = tangents;

        mesh.RecalculateBounds();
    }
}
#endif
