using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the CPU-side unit strip used by the Interlude BeamEmitter port.
/// Hang <see cref="L2BeamStripMeshBuilder"/> on the BeamEmitter GameObject.
/// This type stays a MonoBehaviour so Unity can import the script; do not add it
/// as a component. Use the static factory methods only.
///
/// Mesh contract:
/// - position.z: normalized distance along the beam, 0..1;
/// - position.x: sheet edge, -0.5 / +0.5;
/// - UV.x: distance along the beam;
/// - UV.y: sheet edge, 0 / 1.
///
/// The BeamEmitter shader expands this unit strip to the sampled start/end points
/// and rotates its width axis toward the camera.
/// </summary>
[AddComponentMenu("")]
public class L2BeamEmitterStripBuilder : MonoBehaviour
{
    public const int MaxSegments = 32;

    public static Mesh Build(
        int highFrequencyPoints,
        float beamTextureUScale = 1f,
        float beamTextureVScale = 1f,
        HideFlags hideFlags = HideFlags.DontSave)
    {
        int points = Mathf.Clamp(highFrequencyPoints, 2, MaxSegments + 1);
        int segments = points - 1;
        int vertexCount = points * 2;

        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var triangles = new int[segments * 6];

        float uScale = Mathf.Max(beamTextureUScale, 0.0001f);
        float vScale = Mathf.Max(beamTextureVScale, 0.0001f);

        for (int i = 0; i < points; i++)
        {
            float along = i / (float)segments;
            int vertex = i * 2;

            vertices[vertex] = new Vector3(-0.5f, 0f, along);
            vertices[vertex + 1] = new Vector3(0.5f, 0f, along);
            uvs[vertex] = new Vector2(along * uScale, 0f);
            uvs[vertex + 1] = new Vector2(along * uScale, vScale);
        }

        int triangle = 0;
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            triangles[triangle++] = a;
            triangles[triangle++] = c;
            triangles[triangle++] = b;
            triangles[triangle++] = b;
            triangles[triangle++] = c;
            triangles[triangle++] = d;
        }

        var mesh = new Mesh
        {
            name = "L2BeamEmitterStrip_HF" + points,
            hideFlags = hideFlags,
        };
        mesh.SetVertices(new List<Vector3>(vertices));
        mesh.SetUVs(0, new List<Vector2>(uvs));
        mesh.SetTriangles(triangles, 0, false);
        mesh.RecalculateBounds();

        // The vertex shader replaces the unit positions with the actual beam span.
        // Keep a generous local bound until integration supplies an exact bound.
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 50f);
        return mesh;
    }

    public static int SegmentCount(int highFrequencyPoints)
    {
        int points = Mathf.Clamp(highFrequencyPoints, 2, MaxSegments + 1);
        return points - 1;
    }

    public static void AssignStripToFilters(Component host, Mesh mesh)
    {
        if (host == null || mesh == null)
        {
            return;
        }

        MeshFilter[] filters = host.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] != null)
            {
                filters[i].sharedMesh = mesh;
            }
        }
    }
}
