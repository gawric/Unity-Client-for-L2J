#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// RenderDoc Mesh Viewer CSV → Unity Mesh.
/// Export VS Input (object space), not SV_POSITION / clip space.
/// </summary>
public static class L2EffectGeneratorRenderDocCsvMesh
{
    [MenuItem("Tools/L2 Effects/Convert OBJ to Mesh Asset...")]
    static void ConvertObjMenu()
    {
        string defaultDir = ToAbsoluteMeshFolder();
        string objPath = EditorUtility.OpenFilePanel("OBJ to Mesh Asset", defaultDir, "obj");
        if (string.IsNullOrEmpty(objPath) || !File.Exists(objPath))
        {
            return;
        }

        ConvertObjFileToMeshAsset(objPath);
    }

    [MenuItem("Tools/L2 Effects/Convert Selected OBJ to Mesh Asset", true)]
    static bool ConvertSelectedObjValidate()
    {
        return Selection.activeObject != null &&
               AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".obj", StringComparison.OrdinalIgnoreCase);
    }

    [MenuItem("Tools/L2 Effects/Convert Selected OBJ to Mesh Asset")]
    static void ConvertSelectedObj()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        ConvertObjFileToMeshAsset(ToAbsoluteAssetPath(assetPath));
    }

    [MenuItem("Tools/L2 Effects/Import RenderDoc Mesh CSV...")]
    static void ImportMenu()
    {
        string csv = EditorUtility.OpenFilePanel("RenderDoc mesh CSV", "", "csv");
        if (string.IsNullOrEmpty(csv) || !File.Exists(csv))
        {
            return;
        }

        string objectName = L2EffectImportUtil.Sanitize(Path.GetFileNameWithoutExtension(csv));
        if (string.IsNullOrWhiteSpace(objectName))
        {
            objectName = "mesh";
        }

        bool ueBasis = EditorUtility.DisplayDialog(
            "RenderDoc CSV",
            "Apply L2/UE → Unity basis (XZY × 0.01, flip V) like PSK meshes?\n\n" +
            "UE → Unity: VS Input from the L2 client (object space).\n" +
            "Raw: keep captured coordinates (VS Output / already transformed).",
            "UE → Unity",
            "Raw");

        try
        {
            Mesh mesh = Load(csv, ueBasis);
            mesh.name = objectName;
            L2EffectImportUtil.EnsureFolder(L2EffectPackageRoots.MeshDestFolder);
            string destPath = L2EffectPackageRoots.MeshDestFolder + "/" + objectName + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(destPath);
            if (existing != null)
            {
                existing.Clear();
                CopyInto(mesh, existing);
                existing.name = objectName;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                mesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, destPath);
            }

            AssetDatabase.ImportAsset(destPath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Mesh>(destPath));
            Debug.Log(
                "[L2EffectGenerator] RenderDoc CSV → " + destPath +
                " verts=" + mesh.vertexCount +
                " tris=" + (mesh.triangles.Length / 3) +
                " uv=" + (mesh.uv != null && mesh.uv.Length == mesh.vertexCount) +
                " basis=" + (ueBasis ? "UE" : "raw"));
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("RenderDoc CSV", ex.Message, "OK");
            Debug.LogWarning("[L2EffectGenerator] RenderDoc CSV failed: " + ex.Message);
        }
    }

    static void ConvertObjFileToMeshAsset(string objPath)
    {
        try
        {
            string objectName = L2EffectImportUtil.Sanitize(Path.GetFileNameWithoutExtension(objPath));
            if (string.IsNullOrWhiteSpace(objectName))
            {
                objectName = "mesh";
            }

            Mesh mesh = LoadObj(objPath);
            mesh.name = objectName;
            SaveMeshAsset(mesh, objectName);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("OBJ to Mesh Asset", ex.Message, "OK");
            Debug.LogWarning("[L2EffectGenerator] OBJ convert failed: " + ex.Message);
        }
    }

    static void SaveMeshAsset(Mesh mesh, string objectName)
    {
        L2EffectImportUtil.EnsureFolder(L2EffectPackageRoots.MeshDestFolder);
        string destPath = L2EffectPackageRoots.MeshDestFolder + "/" + objectName + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(destPath);
        if (existing != null)
        {
            existing.Clear();
            CopyInto(mesh, existing);
            existing.name = objectName;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, destPath);
        }

        AssetDatabase.ImportAsset(destPath);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Mesh>(destPath));
        Debug.Log(
            "[L2EffectGenerator] Mesh asset → " + destPath +
            " verts=" + mesh.vertexCount +
            " tris=" + (mesh.triangles.Length / 3));
    }

    public static Mesh LoadObj(string path)
    {
        var positions = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var outPos = new List<Vector3>();
        var outUv = new List<Vector2>();
        var outNrm = new List<Vector3>();
        var tris = new List<int>();
        bool hasUv = false;
        bool hasNrm = false;

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] t = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length == 0)
            {
                continue;
            }

            if (t[0] == "v" && t.Length >= 4)
            {
                positions.Add(new Vector3(ParseF(t[1]), ParseF(t[2]), ParseF(t[3])));
                continue;
            }

            if (t[0] == "vt" && t.Length >= 3)
            {
                uvs.Add(new Vector2(ParseF(t[1]), ParseF(t[2])));
                continue;
            }

            if (t[0] == "vn" && t.Length >= 4)
            {
                normals.Add(new Vector3(ParseF(t[1]), ParseF(t[2]), ParseF(t[3])));
                continue;
            }

            if (t[0] != "f" || t.Length < 4)
            {
                continue;
            }

            var face = new List<int>(t.Length - 1);
            for (int i = 1; i < t.Length; i++)
            {
                string[] parts = t[i].Split('/');
                int vi = ParseIndex(parts[0], positions.Count);
                int ti = parts.Length > 1 ? ParseIndex(parts[1], uvs.Count) : 0;
                int ni = parts.Length > 2 ? ParseIndex(parts[2], normals.Count) : 0;
                if (vi <= 0)
                {
                    continue;
                }

                outPos.Add(positions[vi - 1]);
                if (ti > 0)
                {
                    outUv.Add(uvs[ti - 1]);
                    hasUv = true;
                }
                else
                {
                    outUv.Add(Vector2.zero);
                }

                if (ni > 0)
                {
                    outNrm.Add(normals[ni - 1]);
                    hasNrm = true;
                }
                else
                {
                    outNrm.Add(Vector3.up);
                }

                face.Add(outPos.Count - 1);
            }

            for (int i = 1; i + 1 < face.Count; i++)
            {
                tris.Add(face[0]);
                tris.Add(face[i]);
                tris.Add(face[i + 1]);
            }
        }

        if (outPos.Count < 3 || tris.Count < 3)
        {
            throw new InvalidDataException("OBJ has no triangles: " + path);
        }

        var mesh = new Mesh();
        mesh.name = Path.GetFileNameWithoutExtension(path);
        if (outPos.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(outPos);
        if (hasUv)
        {
            mesh.SetUVs(0, outUv);
        }

        mesh.SetTriangles(tris, 0, false);
        if (hasNrm)
        {
            mesh.SetNormals(outNrm);
        }
        else
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static float ParseF(string raw)
    {
        return float.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    static int ParseIndex(string raw, int count)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return 0;
        }

        int idx = int.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (idx < 0)
        {
            idx = count + idx + 1;
        }

        return idx;
    }

    static string ToAbsoluteMeshFolder()
    {
        string abs = ToAbsoluteAssetPath(L2EffectPackageRoots.MeshDestFolder);
        return Directory.Exists(abs) ? abs : Application.dataPath;
    }

    static string ToAbsoluteAssetPath(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    public static Mesh Load(string path, bool ueBasis)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        int headerLine = FirstNonEmpty(lines, 0);
        if (headerLine < 0)
        {
            throw new InvalidDataException("CSV is empty: " + path);
        }

        char delimiter = DetectDelimiter(lines[headerLine]);
        string[] headers = SplitCsv(lines[headerLine], delimiter);
        ColumnMap columns = MapColumns(headers);
        if (columns.Px < 0 || columns.Py < 0 || columns.Pz < 0)
        {
            throw new InvalidDataException(
                "No POSITION.x/y/z columns. Export VS Input from RenderDoc Mesh Viewer, not SV_POSITION.");
        }

        if (columns.UsedSvPosition)
        {
            Debug.LogWarning(
                "[L2EffectGenerator] CSV uses SV_POSITION (clip space). Export VS Input / POSITION instead.");
        }

        var sourceIdx = new List<int>();
        var srcPos = new List<Vector3>();
        var srcUv = new List<Vector2>();
        var srcNrm = new List<Vector3>();
        var srcCol = new List<Color>();
        bool hasUv = columns.Ux >= 0 && columns.Uy >= 0;
        bool hasNrm = columns.Nx >= 0 && columns.Ny >= 0 && columns.Nz >= 0;
        bool hasCol = columns.Cx >= 0 && columns.Cy >= 0 && columns.Cz >= 0;

        for (int line = headerLine + 1; line < lines.Length; line++)
        {
            if (string.IsNullOrWhiteSpace(lines[line]))
            {
                continue;
            }

            string[] cells = SplitCsv(lines[line], delimiter);
            if (!TryFloat(cells, columns.Px, out float x) ||
                !TryFloat(cells, columns.Py, out float y) ||
                !TryFloat(cells, columns.Pz, out float z))
            {
                continue;
            }

            int idx = sourceIdx.Count;
            if (columns.Idx >= 0 && TryInt(cells, columns.Idx, out int parsedIdx))
            {
                idx = parsedIdx;
            }
            else if (columns.Vtx >= 0 && TryInt(cells, columns.Vtx, out int parsedVtx))
            {
                idx = parsedVtx;
            }

            sourceIdx.Add(idx);
            srcPos.Add(new Vector3(x, y, z));

            if (hasUv &&
                TryFloat(cells, columns.Ux, out float u) &&
                TryFloat(cells, columns.Uy, out float v))
            {
                srcUv.Add(new Vector2(u, v));
            }
            else
            {
                srcUv.Add(Vector2.zero);
                hasUv = false;
            }

            if (hasNrm &&
                TryFloat(cells, columns.Nx, out float nx) &&
                TryFloat(cells, columns.Ny, out float ny) &&
                TryFloat(cells, columns.Nz, out float nz))
            {
                srcNrm.Add(new Vector3(nx, ny, nz));
            }
            else
            {
                srcNrm.Add(Vector3.up);
                hasNrm = false;
            }

            if (hasCol &&
                TryFloat(cells, columns.Cx, out float cr) &&
                TryFloat(cells, columns.Cy, out float cg) &&
                TryFloat(cells, columns.Cz, out float cb))
            {
                float ca = 1f;
                if (columns.Cw >= 0)
                {
                    TryFloat(cells, columns.Cw, out ca);
                }

                srcCol.Add(new Color(cr, cg, cb, ca));
            }
            else
            {
                srcCol.Add(Color.white);
                hasCol = false;
            }
        }

        if (srcPos.Count < 3)
        {
            throw new InvalidDataException("CSV has fewer than 3 vertices: " + path);
        }

        var compactFromSource = new Dictionary<int, int>();
        var verts = new List<Vector3>(srcPos.Count);
        var uvs = new List<Vector2>(srcPos.Count);
        var nrms = new List<Vector3>(srcPos.Count);
        var cols = new List<Color>(srcPos.Count);
        var tris = new List<int>(srcPos.Count);

        for (int i = 0; i < srcPos.Count; i++)
        {
            int source = sourceIdx[i];
            if (!compactFromSource.TryGetValue(source, out int compact))
            {
                compact = verts.Count;
                compactFromSource.Add(source, compact);
                Vector3 p = srcPos[i];
                Vector3 n = srcNrm[i];
                Vector2 uv = srcUv[i];
                if (ueBasis)
                {
                    p = new Vector3(p.x, p.z, p.y) * 0.01f;
                    n = new Vector3(n.x, n.z, n.y);
                    uv = new Vector2(uv.x, 1f - uv.y);
                }

                verts.Add(p);
                uvs.Add(uv);
                nrms.Add(n);
                cols.Add(srcCol[i]);
            }

            tris.Add(compact);
        }

        if (tris.Count % 3 != 0)
        {
            Debug.LogWarning(
                "[L2EffectGenerator] RenderDoc CSV index count " + tris.Count +
                " is not a multiple of 3; dropping the remainder.");
            tris.RemoveRange(tris.Count - (tris.Count % 3), tris.Count % 3);
        }

        var cleanTris = new List<int>(tris.Count);
        for (int i = 0; i + 2 < tris.Count; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];
            if (a == b || b == c || a == c)
            {
                continue;
            }

            if (ueBasis)
            {
                cleanTris.Add(a);
                cleanTris.Add(c);
                cleanTris.Add(b);
            }
            else
            {
                cleanTris.Add(a);
                cleanTris.Add(b);
                cleanTris.Add(c);
            }
        }

        if (cleanTris.Count < 3)
        {
            throw new InvalidDataException("CSV produced no triangles: " + path);
        }

        if (columns.UsedSvPosition && verts.Count > 0)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < verts.Count; i++)
            {
                center += verts[i];
            }

            center /= verts.Count;
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] -= center;
            }
        }

        var mesh = new Mesh();
        mesh.name = Path.GetFileNameWithoutExtension(path);
        if (verts.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(verts);
        if (hasUv)
        {
            mesh.SetUVs(0, uvs);
        }

        if (hasCol)
        {
            mesh.SetColors(cols);
        }

        mesh.SetTriangles(cleanTris, 0, false);
        if (hasNrm)
        {
            mesh.SetNormals(nrms);
        }
        else
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static void CopyInto(Mesh src, Mesh dest)
    {
        dest.indexFormat = src.indexFormat;
        dest.SetVertices(new List<Vector3>(src.vertices));
        dest.SetUVs(0, new List<Vector2>(src.uv));
        dest.SetColors(new List<Color>(src.colors));
        dest.SetTriangles(src.triangles, 0, false);
        dest.SetNormals(new List<Vector3>(src.normals));
        dest.RecalculateBounds();
    }

    struct ColumnMap
    {
        public int Vtx;
        public int Idx;
        public int Px;
        public int Py;
        public int Pz;
        public int Ux;
        public int Uy;
        public int Nx;
        public int Ny;
        public int Nz;
        public int Cx;
        public int Cy;
        public int Cz;
        public int Cw;
        public bool UsedSvPosition;
    }

    static ColumnMap MapColumns(string[] headers)
    {
        var map = new ColumnMap
        {
            Vtx = -1,
            Idx = -1,
            Px = -1,
            Py = -1,
            Pz = -1,
            Ux = -1,
            Uy = -1,
            Nx = -1,
            Ny = -1,
            Nz = -1,
            Cx = -1,
            Cy = -1,
            Cz = -1,
            Cw = -1
        };

        map.Vtx = FindExact(headers, "vtx", "vertex");
        map.Idx = FindExact(headers, "idx", "index");

        if (TryFindXyz(headers, out map.Px, out map.Py, out map.Pz, out bool svPos,
                "position", "in_position", "in_position0", "position0", "pos"))
        {
            map.UsedSvPosition = svPos;
        }
        else if (TryFindXyz(headers, out map.Px, out map.Py, out map.Pz, out svPos,
                     "out_position0", "out_position"))
        {
            // VS Output: already in world/clip. Do not apply UE object-space basis.
            map.UsedSvPosition = true;
        }
        else if (TryFindXyz(headers, out map.Px, out map.Py, out map.Pz, out svPos, "sv_position"))
        {
            map.UsedSvPosition = true;
        }

        TryFindXy(headers, out map.Ux, out map.Uy,
            "texcoord0", "texcoord", "in_texcoord0", "in_texcoord0.xy",
            "in_texcoord", "out_texcoord0", "uv", "uv0");
        TryFindXyz(headers, out map.Nx, out map.Ny, out map.Nz, out _,
            "normal", "in_normal", "in_normal0", "normal0", "out_normal0", "out_normal");
        TryFindXyz(headers, out map.Cx, out map.Cy, out map.Cz, out _,
            "color", "color0", "in_color", "in_color0", "out_color0", "out_color");
        map.Cw = FindComponent(headers, "w", "color", "color0", "in_color", "in_color0",
            "out_color0", "out_color");
        return map;
    }

    static bool TryFindXyz(
        string[] headers,
        out int x,
        out int y,
        out int z,
        out bool usedSvPosition,
        params string[] prefixes)
    {
        usedSvPosition = false;
        x = y = z = -1;
        for (int p = 0; p < prefixes.Length; p++)
        {
            x = FindComponent(headers, "x", prefixes[p]);
            y = FindComponent(headers, "y", prefixes[p]);
            z = FindComponent(headers, "z", prefixes[p]);
            if (x >= 0 && y >= 0 && z >= 0)
            {
                usedSvPosition = prefixes[p].IndexOf("sv_position", StringComparison.OrdinalIgnoreCase) >= 0;
                return true;
            }
        }

        return false;
    }

    static bool TryFindXy(string[] headers, out int x, out int y, params string[] prefixes)
    {
        x = y = -1;
        for (int p = 0; p < prefixes.Length; p++)
        {
            x = FindComponent(headers, "x", prefixes[p]);
            y = FindComponent(headers, "y", prefixes[p]);
            if (x >= 0 && y >= 0)
            {
                return true;
            }
        }

        return false;
    }

    static int FindComponent(string[] headers, string component, params string[] prefixes)
    {
        string suffix = "." + component;
        for (int i = 0; i < headers.Length; i++)
        {
            string name = NormalizeHeader(headers[i]);
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string prefix = name.Substring(0, name.Length - suffix.Length);
            for (int p = 0; p < prefixes.Length; p++)
            {
                if (string.Equals(prefix, prefixes[p], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    static int FindExact(string[] headers, params string[] names)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            string name = NormalizeHeader(headers[i]);
            for (int n = 0; n < names.Length; n++)
            {
                if (string.Equals(name, names[n], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    static string NormalizeHeader(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        raw = raw.Trim().Trim('"').Trim('\uFEFF');
        return raw.Replace(" ", string.Empty);
    }

    static char DetectDelimiter(string header)
    {
        int commas = 0;
        int semis = 0;
        bool inQuotes = false;
        for (int i = 0; i < header.Length; i++)
        {
            char c = header[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
            {
                continue;
            }

            if (c == ',')
            {
                commas++;
            }
            else if (c == ';')
            {
                semis++;
            }
        }

        return semis > commas ? ';' : ',';
    }

    static string[] SplitCsv(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == delimiter && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Length = 0;
                continue;
            }

            current.Append(c);
        }

        cells.Add(current.ToString());
        return cells.ToArray();
    }

    static bool TryFloat(string[] cells, int index, out float value)
    {
        value = 0f;
        if (index < 0 || index >= cells.Length)
        {
            return false;
        }

        string raw = cells[index].Trim().Trim('"');
        if (raw.Length == 0)
        {
            return false;
        }

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    static bool TryInt(string[] cells, int index, out int value)
    {
        value = 0;
        if (index < 0 || index >= cells.Length)
        {
            return false;
        }

        string raw = cells[index].Trim().Trim('"');
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    static int FirstNonEmpty(string[] lines, int start)
    {
        for (int i = start; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
#endif
