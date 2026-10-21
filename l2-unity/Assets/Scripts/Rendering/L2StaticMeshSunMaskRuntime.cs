using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs remapped per-instance L2 sun visibility bytes into TEXCOORD3.
/// The byte contains one visibility bit for each of the eight three-hour slots.
/// </summary>
public sealed class L2StaticMeshSunMaskRuntime : MonoBehaviour
{
    [Serializable]
    public sealed class MapData
    {
        public string map;
        public ActorData[] actors;
    }

    [Serializable]
    public sealed class ActorData
    {
        public string actor;
        public string mesh;
        public string location;
        public int vertexCount;
        public string visibility;
        public bool hasUnityPosition;
        public Vector3 unityPosition;

        public Vector3 UnityPosition =>
            hasUnityPosition
                ? unityPosition
                : L2StaticMeshVertexLight.ToUnityPosition(
                    L2StaticMeshSunMaskDatabase.ParseVector(location));
    }

    const bool ShowSunMaskDebug = false;
    // TEMPORARY. Per-mesh sun masks are not applied while the Unity sun is tuned.
    const bool LoadSunMasks = false;
    static readonly int HasMaskId = Shader.PropertyToID("_L2HasSunMask");
    static readonly int ForceSunVisibleId =
        Shader.PropertyToID("_L2StaticMeshForceSunVisible");
    static readonly int SunMaskDebugId = Shader.PropertyToID("_L2SunMaskDebug");
    static bool _installed;

    Mesh _streamMesh;
    MeshRenderer _renderer;
    bool _ownsStream;

    sealed class CombinedStreamData
    {
        public readonly Mesh mesh;
        public readonly float[] packed;
        public readonly List<MeshFilter> filters = new List<MeshFilter>();

        public CombinedStreamData(Mesh source)
        {
            mesh = source;
            packed = new float[source.vertexCount];
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (_installed)
            return;
        _installed = true;
        if (!LoadSunMasks)
        {
            // Debug.Log("[L2StaticMeshSunMask] load paused");
            return;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyScene(SceneManager.GetSceneAt(i));
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene);
    }

    static void ApplyScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        TextAsset asset = Resources.Load<TextAsset>(
            "Data/Maps/" + scene.name + "/Meta/StaticMeshSunMasksRemapped");
        if (asset == null)
            return;

        MapData data = JsonUtility.FromJson<MapData>(asset.text);
        if (data == null || data.actors == null)
            return;

        Shader.SetGlobalFloat(ForceSunVisibleId, 0f);
        Shader.SetGlobalFloat(SunMaskDebugId, ShowSunMaskDebug ? 1f : 0f);

        var filtersByMesh = new Dictionary<string, List<MeshFilter>>(StringComparer.OrdinalIgnoreCase);
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            MeshFilter[] filters = roots[r].GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (!filtersByMesh.TryGetValue(filter.gameObject.name, out List<MeshFilter> list))
                {
                    list = new List<MeshFilter>();
                    filtersByMesh.Add(filter.gameObject.name, list);
                }
                list.Add(filter);
            }
        }

        var used = new HashSet<MeshFilter>();
        var combinedStreams = new Dictionary<Mesh, CombinedStreamData>();
        int applied = 0;
        int missingMeshName = 0;
        int positionMismatch = 0;
        int vertexMismatch = 0;
        var mismatchExamples = new List<string>();
        string bridgeStatus = "not in database";
        for (int i = 0; i < data.actors.Length; i++)
        {
            ActorData actor = data.actors[i];
            if (actor == null || string.IsNullOrEmpty(actor.visibility))
                continue;
            bool isBridge = string.Equals(
                actor.actor,
                "StaticMeshActor1",
                StringComparison.OrdinalIgnoreCase);
            if (!filtersByMesh.TryGetValue(actor.mesh, out List<MeshFilter> candidates))
            {
                missingMeshName++;
                if (isBridge)
                    bridgeStatus = "mesh name not found: " + actor.mesh;
                continue;
            }

            MeshFilter best = null;
            float bestDistance = float.PositiveInfinity;
            MeshFilter nearestAny = null;
            float nearestAnyDistance = float.PositiveInfinity;
            Vector3 expected = actor.UnityPosition;
            for (int c = 0; c < candidates.Count; c++)
            {
                MeshFilter candidate = candidates[c];
                if (used.Contains(candidate) || candidate.sharedMesh == null)
                    continue;
                float distance = Vector3.Distance(candidate.transform.position, expected);
                if (distance < nearestAnyDistance)
                {
                    nearestAny = candidate;
                    nearestAnyDistance = distance;
                }
                bool compatible =
                    candidate.sharedMesh.vertexCount == actor.vertexCount ||
                    TryGetCombinedVertexRange(
                        candidate,
                        actor.vertexCount,
                        out _,
                        out _);
                if (compatible && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                vertexMismatch++;
                if (mismatchExamples.Count < 8)
                {
                    mismatchExamples.Add(
                        actor.actor + "/" + actor.mesh +
                        " expected=" + actor.vertexCount +
                        " nearest=" +
                        (nearestAny != null
                            ? nearestAny.sharedMesh.name + ":" +
                                nearestAny.sharedMesh.vertexCount +
                                " distance=" + nearestAnyDistance.ToString("G6")
                            : "none") +
                        " candidates=" + candidates.Count);
                }
                if (isBridge)
                    bridgeStatus = "no scene mesh with " + actor.vertexCount + " vertices";
                continue;
            }
            if (bestDistance > 0.05f)
            {
                positionMismatch++;
                if (isBridge)
                    bridgeStatus = "position mismatch distance=" + bestDistance.ToString("G6");
                continue;
            }

            byte[] visibility = Convert.FromBase64String(actor.visibility);
            if (visibility.Length != actor.vertexCount)
                continue;

            bool direct = best.sharedMesh.vertexCount == actor.vertexCount;
            if (direct)
            {
                L2StaticMeshSunMaskRuntime component =
                    best.gameObject.AddComponent<L2StaticMeshSunMaskRuntime>();
                component.Initialize(best, visibility);
            }
            else if (!TryAddCombinedVisibility(
                best,
                visibility,
                combinedStreams,
                out string combinedError))
            {
                vertexMismatch++;
                if (mismatchExamples.Count < 8)
                {
                    mismatchExamples.Add(
                        actor.actor + "/" + actor.mesh +
                        " expected=" + actor.vertexCount +
                        " combined=" + best.sharedMesh.name + ":" +
                        best.sharedMesh.vertexCount +
                        " error=" + combinedError);
                }
                continue;
            }

            used.Add(best);
            applied++;
            if (isBridge)
                bridgeStatus = "applied distance=" + bestDistance.ToString("G6") +
                    " vertices=" + actor.vertexCount;
        }

        InstallCombinedStreams(combinedStreams);

        // Debug.Log("[L2StaticMeshSunMask] " + scene.name + ": applied " + applied +
            // " of " + data.actors.Length +
            // " missingName=" + missingMeshName +
            // " positionMismatch=" + positionMismatch +
            // " vertexMismatch=" + vertexMismatch +
            // " zeroMaskSunFallback=0" +
            // " sunMaskDebug=" + (ShowSunMaskDebug ? "1" : "0") +
            // "; bridge=" + bridgeStatus + "." +
            // (mismatchExamples.Count > 0
                // ? Environment.NewLine + "Mismatch examples: " +
                    // string.Join(" | ", mismatchExamples)
                // : ""));
    }

    void Initialize(MeshFilter filter, byte[] visibility)
    {
        var packed = new float[visibility.Length];
        for (int i = 0; i < visibility.Length; i++)
            packed[i] = visibility[i] / 255f;
        Mesh stream = CreateStream(filter.sharedMesh, packed);
        Attach(filter, stream, true);
    }

    static Mesh CreateStream(Mesh source, float[] packed)
    {
        var stream = new Mesh
        {
            name = source.name + "_L2SunMaskStream",
            bounds = source.bounds
        };
        stream.SetVertexBufferParams(
            packed.Length,
            new VertexAttributeDescriptor(
                VertexAttribute.TexCoord3,
                VertexAttributeFormat.Float32,
                1));
        stream.SetVertexBufferData(
            packed,
            0,
            0,
            packed.Length,
            0,
            MeshUpdateFlags.DontRecalculateBounds);
        return stream;
    }

    void Attach(MeshFilter filter, Mesh stream, bool ownsStream)
    {
        _streamMesh = stream;
        _ownsStream = ownsStream;
        _renderer = filter.GetComponent<MeshRenderer>();
        if (_renderer != null)
        {
            _renderer.additionalVertexStreams = _streamMesh;
            var properties = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(properties);
            properties.SetFloat(HasMaskId, 1f);
            _renderer.SetPropertyBlock(properties);
        }
    }

    static bool TryAddCombinedVisibility(
        MeshFilter filter,
        byte[] visibility,
        Dictionary<Mesh, CombinedStreamData> streams,
        out string error)
    {
        Mesh mesh = filter.sharedMesh;
        if (!TryGetCombinedVertexRange(
            filter,
            visibility.Length,
            out int firstVertex,
            out error))
            return false;

        if (!streams.TryGetValue(mesh, out CombinedStreamData stream))
        {
            stream = new CombinedStreamData(mesh);
            streams.Add(mesh, stream);
        }
        for (int i = 0; i < visibility.Length; i++)
            stream.packed[firstVertex + i] = visibility[i] / 255f;
        if (!stream.filters.Contains(filter))
            stream.filters.Add(filter);
        error = null;
        return true;
    }

    static bool TryGetCombinedVertexRange(
        MeshFilter filter,
        int expectedVertexCount,
        out int firstVertex,
        out string error)
    {
        MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
        Mesh mesh = filter.sharedMesh;
        firstVertex = 0;
        if (renderer == null || mesh == null || !renderer.isPartOfStaticBatch)
        {
            error = "not a static batch renderer";
            return false;
        }

        int subMeshStart = renderer.subMeshStartIndex;
        int subMeshCount = renderer.sharedMaterials.Length;
        int rangeStart = int.MaxValue;
        int rangeEnd = 0;
        for (int i = 0; i < subMeshCount; i++)
        {
            int subMesh = subMeshStart + i;
            if (subMesh < 0 || subMesh >= mesh.subMeshCount)
                continue;
            SubMeshDescriptor descriptor = mesh.GetSubMesh(subMesh);
            rangeStart = Mathf.Min(rangeStart, descriptor.firstVertex);
            rangeEnd = Mathf.Max(
                rangeEnd,
                descriptor.firstVertex + descriptor.vertexCount);
        }

        int rangeLength = rangeStart == int.MaxValue ? 0 : rangeEnd - rangeStart;
        if (rangeLength != expectedVertexCount)
        {
            error = "range=" + rangeLength +
                " submeshes=" + subMeshStart + "+" + subMeshCount;
            return false;
        }

        firstVertex = rangeStart;
        error = null;
        return true;
    }

    static void InstallCombinedStreams(Dictionary<Mesh, CombinedStreamData> streams)
    {
        foreach (CombinedStreamData data in streams.Values)
        {
            Mesh stream = CreateStream(data.mesh, data.packed);
            for (int i = 0; i < data.filters.Count; i++)
            {
                MeshFilter filter = data.filters[i];
                L2StaticMeshSunMaskRuntime component =
                    filter.gameObject.AddComponent<L2StaticMeshSunMaskRuntime>();
                component.Attach(filter, stream, i == 0);
            }
        }
    }

    void OnDestroy()
    {
        if (_renderer != null && _renderer.additionalVertexStreams == _streamMesh)
            _renderer.additionalVertexStreams = null;
        if (_streamMesh == null || !_ownsStream)
            return;
        if (Application.isPlaying)
            Destroy(_streamMesh);
        else
            DestroyImmediate(_streamMesh);
    }
}
