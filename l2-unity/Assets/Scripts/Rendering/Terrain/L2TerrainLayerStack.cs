using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Extra terrain draws after the opaque base: one DrawMesh per enabled
/// _Layer_1..9. Submitted from L2TerrainOverlayRenderPass (AfterRenderingOpaques).
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
[DefaultExecutionOrder(50)]
public sealed class L2TerrainLayerStack : MonoBehaviour
{
    public const string BaseShaderName = "L2/World/TerrainBaseModulate2X";
    public const string LayerShaderName = "L2/World/TerrainLayerModulate2X";
    public const int FirstOverlay = 1;
    public const int LastOverlay = 9;

    static readonly int LayerTexId = Shader.PropertyToID("_LayerTex");
    static readonly int LayerMaskId = Shader.PropertyToID("_LayerMask");
    static readonly int LayerUvId = Shader.PropertyToID("_LayerUV");
    static readonly int SplatSliceId = Shader.PropertyToID("_SplatSlice");
    static readonly int LayerIndexId = Shader.PropertyToID("_LayerIndex");
    static readonly int WorldUvId = Shader.PropertyToID("_L2WorldUvPerTileUu");
    static readonly int CullId = Shader.PropertyToID("_Cull");
    static readonly List<L2TerrainLayerStack> Active = new List<L2TerrainLayerStack>(8);

    MeshRenderer _renderer;
    MeshFilter _filter;
    Shader _layerShader;
    readonly Material[] _overlayMats = new Material[LastOverlay + 1];
    readonly bool[] _active = new bool[LastOverlay + 1];
    static bool _loggedOnce;
    static bool _loggedAttach;

    public static bool HasOverlays
    {
        get
        {
            for (int i = 0; i < Active.Count; i++)
            {
                L2TerrainLayerStack stack = Active[i];
                if (stack != null && stack.isActiveAndEnabled)
                    return true;
            }
            return false;
        }
    }

    public static void DrawAll(CommandBuffer cmd)
    {
        if (cmd == null)
            return;
        for (int i = 0; i < Active.Count; i++)
        {
            L2TerrainLayerStack stack = Active[i];
            if (stack != null)
                stack.Draw(cmd);
        }
    }

    public static void EnsureAttached()
    {
        if (HasOverlays)
            return;
        AttachAll();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AttachLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Camera.onPreCull -= OnPreCullAttach;
        Camera.onPreCull += OnPreCullAttach;
        AttachAll();
    }

    static void OnPreCullAttach(Camera camera)
    {
        if (camera == null || HasOverlays)
            return;
        AttachAll();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachAll();
    }

    static void AttachAll()
    {
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int attached = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (TryAttach(renderers[i]))
                attached++;
        }

        if (!_loggedAttach)
        {
            _loggedAttach = true;
            // Debug.Log("[L2TerrainLayerStack] attach scan renderers=" + renderers.Length + " terrain=" + attached);
        }
    }

    public static bool TryAttach(Renderer renderer)
    {
        if (renderer == null)
            return false;
        Material src = renderer.sharedMaterial;
        if (!IsTerrainSource(src))
            return false;
        if (renderer.GetComponent<L2TerrainLayerStack>() != null)
            return true;
        renderer.gameObject.AddComponent<L2TerrainLayerStack>();
        return true;
    }

    public static bool IsTerrainSource(Material mat)
    {
        if (mat == null || mat.shader == null)
            return false;
        string n = mat.shader.name;
        return n == BaseShaderName
            || n == "L2/World/TerrainModulate2X"
            || n == "L2/World/TerrainSplatModulate2X";
    }

    static bool IsSkippedOverlay(Texture tex)
    {
        if (tex == null)
            return true;
        string n = tex.name;
        if (string.IsNullOrEmpty(n))
            return false;
        if (n.IndexOf("Water", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("SL_WR", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.EndsWith("_WR", System.StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    static Texture ReadTex(Material src, string name)
    {
        if (src == null || string.IsNullOrEmpty(name))
            return null;
        if (!src.HasProperty(name))
            return null;
        return src.GetTexture(name);
    }

    void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        _filter = GetComponent<MeshFilter>();
        _layerShader = Shader.Find(LayerShaderName);
        EnsureNativeVertexGrid();
        if (!Active.Contains(this))
            Active.Add(this);
    }

    // Native terrain is 256x256 vertices (indices 0..255). The imported mesh stops at 254.
    // Existing vertices stay put. The new edge uses the same UV step i/254, so mask UV stays (i+0.5)/256.
    void EnsureNativeVertexGrid()
    {
        if (_filter == null || _filter.sharedMesh == null)
            return;
        Mesh src = _filter.sharedMesh;
        const int oldSide = 255;
        const int newSide = 256;
        if (src.vertexCount != oldSide * oldSide)
            return;

        Vector3[] oldPos = src.vertices;
        Vector3[] oldNrm = src.normals;
        Vector2[] oldUv = src.uv;
        int[] oldTris = src.triangles;
        bool hasNrm = oldNrm != null && oldNrm.Length == oldPos.Length;
        bool hasUv = oldUv != null && oldUv.Length == oldPos.Length;
        const float step = 1f / 254f;
        var cell = new int[newSide * newSide];
        for (int i = 0; i < cell.Length; i++)
            cell[i] = -1;
        for (int i = 0; i < oldPos.Length; i++)
        {
            int gx = Mathf.Clamp(Mathf.RoundToInt(oldPos[i].x / step), 0, oldSide - 1);
            int gz = Mathf.Clamp(Mathf.RoundToInt(oldPos[i].z / step), 0, oldSide - 1);
            cell[gz * newSide + gx] = i;
        }

        var pos = new List<Vector3>(oldPos);
        var nrm = new List<Vector3>(hasNrm ? oldNrm : new Vector3[oldPos.Length]);
        var uv = new List<Vector2>(hasUv ? oldUv : new Vector2[oldPos.Length]);
        if (!hasNrm)
        {
            for (int i = 0; i < nrm.Count; i++)
                nrm[i] = Vector3.up;
        }

        for (int z = 0; z < newSide; z++)
        {
            for (int x = 0; x < newSide; x++)
            {
                int slot = z * newSide + x;
                if (cell[slot] >= 0 || (x < oldSide && z < oldSide))
                    continue;
                int x0 = Mathf.Min(x, oldSide - 1);
                int z0 = Mathf.Min(z, oldSide - 1);
                int x1 = Mathf.Max(0, x0 - 1);
                int z1 = Mathf.Max(0, z0 - 1);
                int ia = cell[z0 * newSide + x0];
                int ib = cell[z0 * newSide + x1];
                int ic = cell[z1 * newSide + x0];
                if (ia < 0)
                    continue;
                Vector3 p = oldPos[ia];
                if (x == oldSide && ib >= 0)
                    p += oldPos[ia] - oldPos[ib];
                if (z == oldSide && ic >= 0)
                    p += oldPos[ia] - oldPos[ic];
                if (x == oldSide && z == oldSide && ib >= 0 && ic >= 0)
                    p = oldPos[ia] + (oldPos[ia] - oldPos[ib]) + (oldPos[ia] - oldPos[ic]);
                cell[slot] = pos.Count;
                pos.Add(p);
                nrm.Add(Vector3.up);
                uv.Add(new Vector2(x / 254f, z / 254f));
            }
        }

        var tris = new List<int>(oldTris);
        for (int z = 0; z < oldSide; z++)
        {
            for (int x = 0; x < oldSide; x++)
            {
                if (x < oldSide - 1 && z < oldSide - 1)
                    continue;
                int a = cell[z * newSide + x];
                int b = cell[z * newSide + (x + 1)];
                int c = cell[(z + 1) * newSide + x];
                int d = cell[(z + 1) * newSide + (x + 1)];
                if (a < 0 || b < 0 || c < 0 || d < 0)
                    continue;
                tris.Add(a);
                tris.Add(c);
                tris.Add(b);
                tris.Add(b);
                tris.Add(c);
                tris.Add(d);
            }
        }

        var mesh = new Mesh { name = src.name + "_256" };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(pos);
        mesh.SetNormals(nrm);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        _filter.mesh = mesh;
    }

    void OnDisable()
    {
        Active.Remove(this);
        ReleaseOverlays();
    }

    void OnDestroy()
    {
        Active.Remove(this);
        ReleaseOverlays();
    }

    public void Draw(CommandBuffer cmd)
    {
        if (cmd == null || _renderer == null || _filter == null || !_renderer.enabled || !isActiveAndEnabled)
            return;
        Mesh mesh = _filter.sharedMesh;
        if (mesh == null)
            return;
        if (_layerShader == null)
            _layerShader = Shader.Find(LayerShaderName);
        Material src = _renderer.sharedMaterial;
        if (!IsTerrainSource(src) || _layerShader == null)
        {
            LogOnce("skip draw shaderFound=" + (_layerShader != null) + " mat=" + (src != null ? src.name : "null") +
                    " shader=" + (src != null && src.shader != null ? src.shader.name : "null"));
            return;
        }

        SyncOverlays(src);
        Matrix4x4 matrix = _renderer.localToWorldMatrix;
        int subCount = Mathf.Max(1, mesh.subMeshCount);
        int drawn = 0;
        for (int layer = FirstOverlay; layer <= LastOverlay; layer++)
        {
            if (!_active[layer] || _overlayMats[layer] == null)
                continue;
            for (int sub = 0; sub < subCount; sub++)
                cmd.DrawMesh(mesh, matrix, _overlayMats[layer], sub, 0);
            drawn++;
        }

        LogOnce("overlays=" + drawn + " mesh=" + mesh.name + " mat=" + src.name +
                " shader=" + _layerShader.name);
    }

    void SyncOverlays(Material src)
    {
        float worldUv = src.HasProperty("_L2WorldUvPerTileUu") ? src.GetFloat("_L2WorldUvPerTileUu") : 128f;
        float cull = src.HasProperty("_Cull") ? src.GetFloat("_Cull") : 2f;

        for (int layer = FirstOverlay; layer <= LastOverlay; layer++)
        {
            bool on = !src.HasProperty("_Layer_" + layer + "_Enabled")
                || src.GetFloat("_Layer_" + layer + "_Enabled") > 0.5f;
            Texture tex = ReadTex(src, "_Layer_" + layer);
            Texture mask = ReadTex(src, "_LayerMask_" + layer);
            if (!on || tex == null || mask == null || IsSkippedOverlay(tex))
            {
                _active[layer] = false;
                continue;
            }

            Material mat = _overlayMats[layer];
            if (mat == null || mat.shader != _layerShader)
            {
                if (mat != null)
                    DestroyOverlay(mat);
                mat = new Material(_layerShader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                _overlayMats[layer] = mat;
            }

            mat.name = src.name + "_Overlay" + layer + "_" + tex.name;
            mat.SetTexture(LayerTexId, tex);
            mat.SetTexture(LayerMaskId, mask);
            mat.SetFloat(LayerIndexId, layer);
            mat.SetFloat(SplatSliceId, layer - 1);
            if (src.HasProperty("_Layer_" + layer + "_UV"))
                mat.SetVector(LayerUvId, src.GetVector("_Layer_" + layer + "_UV"));
            else
                mat.SetVector(LayerUvId, new Vector4(1f, 1f, 0f, 0f));
            mat.SetFloat(WorldUvId, worldUv);
            mat.SetFloat(CullId, cull);
            mat.enableInstancing = false;
            _active[layer] = true;
        }
    }

    static void LogOnce(string msg)
    {
        if (_loggedOnce)
            return;
        _loggedOnce = true;
        // Debug.Log("[L2TerrainLayerStack] " + msg);
    }

    void ReleaseOverlays()
    {
        for (int i = 0; i < _overlayMats.Length; i++)
        {
            DestroyOverlay(_overlayMats[i]);
            _overlayMats[i] = null;
            if (i < _active.Length)
                _active[i] = false;
        }
    }

    static void DestroyOverlay(Material mat)
    {
        if (mat == null)
            return;
        if (Application.isPlaying)
            Destroy(mat);
        else
            DestroyImmediate(mat);
    }
}
