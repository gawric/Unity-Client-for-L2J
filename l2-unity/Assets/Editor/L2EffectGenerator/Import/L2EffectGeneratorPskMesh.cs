#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// PSK/PSKX → Unity Mesh, same UE→Unity basis as the effect viewer
/// (UE XZY * 0.01, ground rings flattened onto XZ).
/// </summary>
public static class L2EffectGeneratorPskMesh
{
    struct Chunk
    {
        public string Id;
        public int DataSize;
        public int DataCount;
        public long DataStart;
    }

    public static Mesh Load(string path, Mesh dest = null)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var chunks = ReadChunks(bytes);
        if (!chunks.TryGetValue("PNTS0000", out Chunk points) ||
            !chunks.TryGetValue("VTXW0000", out Chunk wedges) ||
            !TryGetFace(chunks, out Chunk faces))
        {
            throw new InvalidDataException("PSK missing PNTS/VTXW/FACE: " + path);
        }

        var verts = new Vector3[points.DataCount];
        for (int i = 0; i < points.DataCount; i++)
        {
            int o = (int)points.DataStart + i * 12;
            float x = BitConverter.ToSingle(bytes, o);
            float y = BitConverter.ToSingle(bytes, o + 4);
            float z = BitConverter.ToSingle(bytes, o + 8);
            verts[i] = new Vector3(x, z, y) * 0.01f;
        }

        int wedgeStride = wedges.DataCount > 0 ? wedges.DataSize : 16;
        var meshVerts = new List<Vector3>(wedges.DataCount);
        var meshUv = new List<Vector2>(wedges.DataCount);
        for (int i = 0; i < wedges.DataCount; i++)
        {
            int o = (int)wedges.DataStart + i * wedgeStride;
            int idx = wedgeStride >= 16 && wedges.DataSize >= 16
                ? BitConverter.ToUInt16(bytes, o)
                : BitConverter.ToInt32(bytes, o);
            if (idx < 0 || idx >= verts.Length)
            {
                idx = 0;
            }

            float u = BitConverter.ToSingle(bytes, o + 4);
            float v = BitConverter.ToSingle(bytes, o + 8);
            meshVerts.Add(verts[idx]);
            meshUv.Add(new Vector2(u, 1f - v));
        }

        int slotCount = CountMaterialSlots(chunks, bytes, faces);
        var submeshTris = new List<int>[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            submeshTris[i] = new List<int>();
        }

        int faceStride = faces.DataCount > 0 ? faces.DataSize : 12;
        bool index32 = string.Equals(faces.Id, "FACE3200", StringComparison.OrdinalIgnoreCase) ||
                       faceStride >= 18;
        int wedgeIndexSize = index32 ? 4 : 2;
        for (int i = 0; i < faces.DataCount; i++)
        {
            int o = (int)faces.DataStart + i * faceStride;
            int a = ReadWedgeIndex(bytes, o, index32);
            int b = ReadWedgeIndex(bytes, o + wedgeIndexSize, index32);
            int c = ReadWedgeIndex(bytes, o + wedgeIndexSize * 2, index32);
            if (a < 0 || b < 0 || c < 0 ||
                a >= meshVerts.Count || b >= meshVerts.Count || c >= meshVerts.Count)
            {
                continue;
            }

            int matIndex = bytes[o + wedgeIndexSize * 3];
            if (matIndex < 0 || matIndex >= slotCount)
            {
                matIndex = 0;
            }

            List<int> tris = submeshTris[matIndex];
            tris.Add(a);
            tris.Add(c);
            tris.Add(b);
        }

        FlattenGroundPlaneOntoXz(meshVerts);
        RecenterGroundRingOnYAxis(meshVerts);

        Mesh mesh = dest != null ? dest : new Mesh();
        if (dest != null)
        {
            mesh.Clear();
        }

        mesh.name = Path.GetFileNameWithoutExtension(path);
        mesh.SetVertices(meshVerts);
        mesh.SetUVs(0, meshUv);
        mesh.subMeshCount = slotCount;
        for (int i = 0; i < slotCount; i++)
        {
            mesh.SetTriangles(submeshTris[i], i, false);
        }

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    public static int CountMaterialSlots(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var chunks = ReadChunks(bytes);
        if (!TryGetFace(chunks, out Chunk faces))
        {
            return 1;
        }

        return CountMaterialSlots(chunks, bytes, faces);
    }

    public static string FormatFaceCounts(Mesh mesh)
    {
        if (mesh == null)
        {
            return string.Empty;
        }

        var parts = new string[mesh.subMeshCount];
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            parts[i] = (mesh.GetTriangles(i).Length / 3).ToString();
        }

        return string.Join(",", parts);
    }

    static void FlattenGroundPlaneOntoXz(List<Vector3> verts)
    {
        if (verts == null || verts.Count < 3)
        {
            return;
        }

        Vector3 min = verts[0];
        Vector3 max = verts[0];
        for (int i = 1; i < verts.Count; i++)
        {
            min = Vector3.Min(min, verts[i]);
            max = Vector3.Max(max, verts[i]);
        }

        Vector3 size = max - min;
        float sx = Mathf.Abs(size.x);
        float sy = Mathf.Abs(size.y);
        float sz = Mathf.Abs(size.z);
        float largest = Mathf.Max(sx, Mathf.Max(sy, sz));
        if (largest < 1e-6f)
        {
            return;
        }

        float thinThreshold = largest * 0.2f;
        bool thinY = sy <= thinThreshold;
        bool thinZ = sz <= thinThreshold;
        bool thinX = sx <= thinThreshold;
        if (thinY || (!thinZ && !thinX))
        {
            return;
        }

        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];
            verts[i] = thinZ
                ? new Vector3(v.x, v.z, -v.y)
                : new Vector3(v.z, v.y, -v.x);
        }
    }

    /// <summary>
    /// Ground rings must spin around local Y. FBX dumps often leave XZ
    /// off-pivot; subtract the XZ AABB center so yaw does not orbit.
    /// </summary>
    static void RecenterGroundRingOnYAxis(List<Vector3> verts)
    {
        if (verts == null || verts.Count < 3)
        {
            return;
        }

        Vector3 min = verts[0];
        Vector3 max = verts[0];
        for (int i = 1; i < verts.Count; i++)
        {
            min = Vector3.Min(min, verts[i]);
            max = Vector3.Max(max, verts[i]);
        }

        Vector3 size = max - min;
        float largest = Mathf.Max(Mathf.Abs(size.x), Mathf.Max(Mathf.Abs(size.y), Mathf.Abs(size.z)));
        if (largest < 1e-6f || Mathf.Abs(size.y) > largest * 0.2f)
        {
            return;
        }

        Vector3 shift = new Vector3(
            (min.x + max.x) * 0.5f,
            0f,
            (min.z + max.z) * 0.5f);
        if (shift.sqrMagnitude < 1e-10f)
        {
            return;
        }

        for (int i = 0; i < verts.Count; i++)
        {
            verts[i] -= shift;
        }
    }

    static bool TryGetFace(Dictionary<string, Chunk> chunks, out Chunk faces)
    {
        if (chunks.TryGetValue("FACE0000", out faces))
        {
            return true;
        }

        if (chunks.TryGetValue("FACE3200", out faces))
        {
            return true;
        }

        faces = default;
        return false;
    }

    static int CountMaterialSlots(Dictionary<string, Chunk> chunks, byte[] bytes, Chunk faces)
    {
        int fromMatt = 0;
        if (chunks.TryGetValue("MATT0000", out Chunk matt) && matt.DataCount > 0)
        {
            fromMatt = matt.DataCount;
        }

        int maxIndex = -1;
        int faceStride = faces.DataCount > 0 ? faces.DataSize : 12;
        bool index32 = string.Equals(faces.Id, "FACE3200", StringComparison.OrdinalIgnoreCase) ||
                       faceStride >= 18;
        int matOffset = (index32 ? 4 : 2) * 3;
        for (int i = 0; i < faces.DataCount; i++)
        {
            int o = (int)faces.DataStart + i * faceStride + matOffset;
            if (o >= bytes.Length)
            {
                break;
            }

            int matIndex = bytes[o];
            if (matIndex > maxIndex)
            {
                maxIndex = matIndex;
            }
        }

        return Math.Max(1, Math.Max(fromMatt, maxIndex + 1));
    }

    static int ReadWedgeIndex(byte[] bytes, int offset, bool index32)
    {
        if (index32)
        {
            return BitConverter.ToInt32(bytes, offset);
        }

        return BitConverter.ToUInt16(bytes, offset);
    }

    static Dictionary<string, Chunk> ReadChunks(byte[] bytes)
    {
        var map = new Dictionary<string, Chunk>(StringComparer.Ordinal);
        int offset = 0;
        while (offset + 32 <= bytes.Length)
        {
            string id = Encoding.ASCII.GetString(bytes, offset, 20).TrimEnd('\0', ' ');
            int dataSize = BitConverter.ToInt32(bytes, offset + 24);
            int dataCount = BitConverter.ToInt32(bytes, offset + 28);
            offset += 32;
            if (dataSize < 0 || dataCount < 0)
            {
                break;
            }

            long dataBytes = (long)dataSize * dataCount;
            if (offset + dataBytes > bytes.Length)
            {
                break;
            }

            map[id] = new Chunk
            {
                Id = id,
                DataSize = dataSize,
                DataCount = dataCount,
                DataStart = offset
            };
            offset += (int)dataBytes;
        }

        return map;
    }
}
#endif
