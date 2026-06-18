#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene wireframe for L2 sprite spawn regions (_Polar* + _StartLocationRange* + _StartLocationOffset).
/// Enable _DebugSpawnRegion on material. Drawn only for prefab stage or current Selection.
/// Menu: Tools/L2 Effects/Diagnose Spawn Region Debug
/// </summary>
[InitializeOnLoad]
public static class L2FxSpriteSpawnRegionDebugDrawer
{
    private static readonly int DebugSpawnRegionId = Shader.PropertyToID("_DebugSpawnRegion");
    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    private static readonly int DebugSpawnRegionColorId = Shader.PropertyToID("_DebugSpawnRegionColor");
    private static readonly int StartLocationOffsetId = Shader.PropertyToID("_StartLocationOffset");
    private static readonly int StartLocationRangeXId = Shader.PropertyToID("_StartLocationRangeX");
    private static readonly int StartLocationRangeYId = Shader.PropertyToID("_StartLocationRangeY");
    private static readonly int StartLocationRangeZId = Shader.PropertyToID("_StartLocationRangeZ");
    private static readonly int PolarAzimuthDegId = Shader.PropertyToID("_PolarAzimuthDeg");
    private static readonly int PolarPitchDegId = Shader.PropertyToID("_PolarPitchDeg");
    private static readonly int PolarRadiusId = Shader.PropertyToID("_PolarRadius");
    private static readonly int SpawnUnitScaleId = Shader.PropertyToID("_SpawnUnitScale");

    private static readonly List<Renderer> RendererScratch = new List<Renderer>(64);
    private static readonly List<CachedDrawEntry> DrawCache = new List<CachedDrawEntry>(16);
    private static readonly List<Vector3> LineScratch = new List<Vector3>(256);

    static L2FxSpriteSpawnRegionDebugDrawer()
    {
        SceneView.duringSceneGui += OnSceneGui;
        Selection.selectionChanged += MarkCacheDirty;
        EditorApplication.hierarchyChanged += MarkCacheDirty;
        PrefabStage.prefabStageOpened += _ => MarkCacheDirty();
        PrefabStage.prefabStageClosing += _ => MarkCacheDirty();
    }

    private static void MarkCacheDirty()
    {
        DrawCache.Clear();
    }

    private static void OnSceneGui(SceneView view)
    {
        if (UnityEngine.Event.current.type != EventType.Repaint)
        {
            return;
        }

        EnsureDrawCache();
        if (DrawCache.Count == 0)
        {
            return;
        }

        CompareFunction prevZTest = Handles.zTest;
        Handles.zTest = CompareFunction.LessEqual;

        for (int i = 0; i < DrawCache.Count; i++)
        {
            CachedDrawEntry entry = DrawCache[i];
            if (entry.Transform == null)
            {
                continue;
            }

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = entry.Transform.localToWorldMatrix;
            Handles.color = entry.Color;

            Vector3[] lines = entry.LocalLines;
            for (int l = 0; l + 1 < lines.Length; l += 2)
            {
                Handles.DrawLine(lines[l], lines[l + 1]);
            }

            Handles.matrix = prevMatrix;
        }

        Handles.zTest = prevZTest;
    }

    private static void EnsureDrawCache()
    {
        CollectRelevantRenderers(RendererScratch);
        if (RendererScratch.Count == 0)
        {
            DrawCache.Clear();
            return;
        }

        if (DrawCache.Count == RendererScratch.Count)
        {
            bool stillValid = true;
            for (int i = 0; i < RendererScratch.Count; i++)
            {
                Renderer renderer = RendererScratch[i];
                CachedDrawEntry entry = DrawCache[i];
                if (entry.Transform != renderer.transform ||
                    entry.SourceMaterial != ResolveMaterial(renderer) ||
                    entry.SettingsHash != ComputeSettingsHash(renderer, ResolveMaterial(renderer)))
                {
                    stillValid = false;
                    break;
                }
            }

            if (stillValid)
            {
                return;
            }
        }

        DrawCache.Clear();
        for (int i = 0; i < RendererScratch.Count; i++)
        {
            Renderer renderer = RendererScratch[i];
            if (renderer == null)
            {
                continue;
            }

            if (Application.isPlaying &&
                (!renderer.enabled || !renderer.gameObject.activeInHierarchy))
            {
                continue;
            }

            Material mat = ResolveMaterial(renderer);
            if (mat == null || !TryReadSpawnSettings(mat, out SpawnSettings settings))
            {
                continue;
            }

            float objectScale = ResolveSpawnObjectScale(mat);
            BuildWireframeLines(settings, objectScale, LineScratch);

            DrawCache.Add(new CachedDrawEntry
            {
                Transform = renderer.transform,
                SourceMaterial = mat,
                SettingsHash = ComputeSettingsHash(renderer, mat),
                Color = ResolveDrawColor(mat),
                LocalLines = LineScratch.ToArray(),
            });
        }
    }

    /// <summary>Prefab stage root, or Selection subtree only — never the whole loaded world.</summary>
    private static void CollectRelevantRenderers(List<Renderer> output)
    {
        output.Clear();

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && prefabStage.prefabContentsRoot != null)
        {
            AddRenderersFromRoot(output, prefabStage.prefabContentsRoot);
            return;
        }

        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            return;
        }

        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i] != null)
            {
                AddRenderersFromRoot(output, selection[i]);
            }
        }
    }

    private static void AddRenderersFromRoot(List<Renderer> output, GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || output.Contains(renderer))
            {
                continue;
            }

            if (!IsPrimaryParticleSlot(renderer))
            {
                continue;
            }

            output.Add(renderer);
        }
    }

    /// <summary>ParticleGroup/ParticleSingle clone slots share one spawn region — draw only _particles[0].</summary>
    private static bool IsPrimaryParticleSlot(Renderer renderer)
    {
        ParticleGroup group = renderer.GetComponentInParent<ParticleGroup>();
        if (group != null)
        {
            return renderer == GetFirstParticleSlot(group);
        }

        ParticleSingle single = renderer.GetComponentInParent<ParticleSingle>();
        if (single != null)
        {
            return renderer == GetFirstParticleSlot(single);
        }

        return true;
    }

    private static Renderer GetFirstParticleSlot(Object effectPart)
    {
        if (effectPart == null)
        {
            return null;
        }

        SerializedObject so = new SerializedObject(effectPart);
        SerializedProperty particles = so.FindProperty("_particles");
        if (particles == null || !particles.isArray || particles.arraySize == 0)
        {
            return null;
        }

        return particles.GetArrayElementAtIndex(0).objectReferenceValue as Renderer;
    }

    private static int ComputeSettingsHash(Renderer renderer, Material mat)
    {
        if (mat == null || !TryReadSpawnSettings(mat, out SpawnSettings s))
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + s.OffsetUe.GetHashCode();
            hash = hash * 31 + s.RangeMinUe.GetHashCode();
            hash = hash * 31 + s.RangeMaxUe.GetHashCode();
            hash = hash * 31 + s.AzimuthMin.GetHashCode();
            hash = hash * 31 + s.AzimuthMax.GetHashCode();
            hash = hash * 31 + s.PitchMin.GetHashCode();
            hash = hash * 31 + s.PitchMax.GetHashCode();
            hash = hash * 31 + s.RadiusMin.GetHashCode();
            hash = hash * 31 + s.RadiusMax.GetHashCode();
            hash = hash * 31 + ResolveSpawnObjectScale(mat).GetHashCode();
            hash = hash * 31 + renderer.transform.localToWorldMatrix.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// MightTaSprite: raw UE UU in object space, then TransformObjectToWorld (renderer scale).
    /// ShieldTaSprite and similar: UE * _SpawnUnitScale in object space, then TransformObjectToWorld.
    /// </summary>
    private static float ResolveSpawnObjectScale(Material mat)
    {
        if (mat == null || mat.shader == null)
        {
            return 1f;
        }

        string shaderName = mat.shader.name;
        if (shaderName.Contains("MightTaSprite"))
        {
            return 1f;
        }

        if (mat.HasProperty(SpawnUnitScaleId))
        {
            return mat.GetFloat(SpawnUnitScaleId);
        }

        return 1f;
    }

    private static void BuildWireframeLines(SpawnSettings s, float objectScale, List<Vector3> lines)
    {
        lines.Clear();

        const int azimuthSegments = 12;
        const int meridians = 4;
        const int arcSegments = 6;

        float radius = s.RadiusMax;
        DrawLatitudeRing(s, s.PitchMin, radius, azimuthSegments, objectScale, lines);
        DrawLatitudeRing(s, s.PitchMax, radius, azimuthSegments, objectScale, lines);

        for (int m = 0; m <= meridians; m++)
        {
            float theta = Mathf.Lerp(s.AzimuthMin, s.AzimuthMax, m / (float)meridians);
            Vector3 prev = default;
            for (int i = 0; i <= arcSegments; i++)
            {
                float phi = Mathf.Lerp(s.PitchMin, s.PitchMax, i / (float)arcSegments);
                Vector3 current = SpawnLocal(s, theta, phi, radius, objectScale);
                if (i > 0)
                {
                    lines.Add(prev);
                    lines.Add(current);
                }

                prev = current;
            }
        }

        DrawSpawnBoundsAabb(s, objectScale, lines, out Vector3 boundsCenter, out float boundsArm);
        AppendAxisCross(boundsCenter, boundsArm, lines);
    }

    /// <summary>RGB = Unity X/Y/Z at spawn AABB center (Y = up).</summary>
    private static void AppendAxisCross(Vector3 center, float arm, List<Vector3> lines)
    {
        if (arm <= 1e-5f)
        {
            return;
        }

        AddLine(lines, center, center + Vector3.right * arm);
        AddLine(lines, center, center + Vector3.up * arm);
        AddLine(lines, center, center + Vector3.forward * arm);
    }

    /// <summary>AABB of polar + StartLocationRange box jitter + offset (same as L2Fx_SpawnRegionOffsetUe).</summary>
    private static void DrawSpawnBoundsAabb(SpawnSettings s, float objectScale, List<Vector3> lines, out Vector3 center, out float axisArm)
    {
        center = Vector3.zero;
        axisArm = 0f;
        if (!TryComputeSpawnAabbLocal(s, objectScale, out Vector3 min, out Vector3 max))
        {
            return;
        }

        AddBoxLines(min, max, lines);
        center = (min + max) * 0.5f;
        axisArm = Mathf.Max(0.08f, (max - min).magnitude * 0.12f);
    }

    private static bool TryComputeSpawnAabbLocal(SpawnSettings s, float objectScale, out Vector3 min, out Vector3 max)
    {
        min = Vector3.positiveInfinity;
        max = Vector3.negativeInfinity;

        float[] thetas = s.AzimuthMax - s.AzimuthMin >= 359f
            ? new[] { 0f, 90f, 180f, 270f }
            : new[] { s.AzimuthMin, (s.AzimuthMin + s.AzimuthMax) * 0.5f, s.AzimuthMax };
        float[] phis = { s.PitchMin, s.PitchMax };
        float[] radii = s.RadiusMin != s.RadiusMax
            ? new[] { s.RadiusMin, s.RadiusMax }
            : new[] { s.RadiusMax };

        Vector3[] boxCorners =
        {
            new Vector3(s.RangeMinUe.x, s.RangeMinUe.y, s.RangeMinUe.z),
            new Vector3(s.RangeMaxUe.x, s.RangeMaxUe.y, s.RangeMaxUe.z),
            new Vector3(s.RangeMinUe.x, s.RangeMaxUe.y, s.RangeMinUe.z),
            new Vector3(s.RangeMaxUe.x, s.RangeMaxUe.y, s.RangeMinUe.z),
            new Vector3(s.RangeMinUe.x, s.RangeMinUe.y, s.RangeMaxUe.z),
            new Vector3(s.RangeMaxUe.x, s.RangeMinUe.y, s.RangeMaxUe.z),
            new Vector3(s.RangeMinUe.x, s.RangeMaxUe.y, s.RangeMaxUe.z),
            new Vector3(s.RangeMaxUe.x, s.RangeMaxUe.y, s.RangeMaxUe.z),
        };

        for (int ti = 0; ti < thetas.Length; ti++)
        {
            for (int pi = 0; pi < phis.Length; pi++)
            {
                for (int ri = 0; ri < radii.Length; ri++)
                {
                    Vector3 polarUe = PolarOffsetUe(thetas[ti], phis[pi], radii[ri]);
                    for (int bi = 0; bi < boxCorners.Length; bi++)
                    {
                        Vector3 local = UeToUnity(polarUe + boxCorners[bi] + s.OffsetUe) * objectScale;
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }
        }

        return !float.IsInfinity(min.x);
    }

    private static void DrawLatitudeRing(SpawnSettings s, float phiDeg, float radius, int segments, float objectScale, List<Vector3> lines)
    {
        Vector3 prev = default;
        for (int i = 0; i <= segments; i++)
        {
            float theta = Mathf.Lerp(s.AzimuthMin, s.AzimuthMax, i / (float)segments);
            Vector3 current = SpawnLocal(s, theta, phiDeg, radius, objectScale);
            if (i > 0)
            {
                lines.Add(prev);
                lines.Add(current);
            }

            prev = current;
        }
    }

    private static void AddBoxLines(Vector3 min, Vector3 max, List<Vector3> lines)
    {
        Vector3 c000 = new Vector3(min.x, min.y, min.z);
        Vector3 c001 = new Vector3(min.x, min.y, max.z);
        Vector3 c010 = new Vector3(min.x, max.y, min.z);
        Vector3 c011 = new Vector3(min.x, max.y, max.z);
        Vector3 c100 = new Vector3(max.x, min.y, min.z);
        Vector3 c101 = new Vector3(max.x, min.y, max.z);
        Vector3 c110 = new Vector3(max.x, max.y, min.z);
        Vector3 c111 = new Vector3(max.x, max.y, max.z);

        AddLine(lines, c000, c100);
        AddLine(lines, c100, c101);
        AddLine(lines, c101, c001);
        AddLine(lines, c001, c000);
        AddLine(lines, c010, c110);
        AddLine(lines, c110, c111);
        AddLine(lines, c111, c011);
        AddLine(lines, c011, c010);
        AddLine(lines, c000, c010);
        AddLine(lines, c100, c110);
        AddLine(lines, c101, c111);
        AddLine(lines, c001, c011);
    }

    private static void AddLine(List<Vector3> lines, Vector3 a, Vector3 b)
    {
        lines.Add(a);
        lines.Add(b);
    }

    private static Vector3 SpawnLocal(SpawnSettings s, float thetaDeg, float phiDeg, float radius, float objectScale)
    {
        return UeToUnity(PolarOffsetUe(thetaDeg, phiDeg, radius) + s.OffsetUe) * objectScale;
    }

    private static Material ResolveMaterial(Renderer renderer)
    {
        Material[] shared = renderer.sharedMaterials;
        for (int i = 0; i < shared.Length; i++)
        {
            if (ShouldDraw(shared[i]))
            {
                return shared[i];
            }
        }

        return null;
    }

    private static bool ShouldDraw(Material mat)
    {
        if (mat == null)
        {
            return false;
        }

        if (!IsSpawnDebugEnabled(mat))
        {
            return false;
        }

        if (Application.isPlaying &&
            mat.HasProperty(StartTimeId) &&
            mat.GetFloat(StartTimeId) > 1e-4f)
        {
            return false;
        }

        return true;
    }

    private static bool IsSpawnDebugEnabled(Material mat)
    {
        if (mat.IsKeywordEnabled("_DEBUGSPAWNREGION_ON"))
        {
            return true;
        }

        if (mat.HasProperty(DebugSpawnRegionId) && mat.GetFloat(DebugSpawnRegionId) >= 0.5f)
        {
            return true;
        }

        return TryReadSerializedFloat(mat, "_DebugSpawnRegion", out float serialized) && serialized >= 0.5f;
    }

    private static bool TryReadSerializedFloat(Material mat, string propertyName, out float value)
    {
        value = 0f;
        if (mat == null)
        {
            return false;
        }

        SerializedObject so = new SerializedObject(mat);
        SerializedProperty floats = so.FindProperty("m_SavedProperties.m_Floats");
        if (floats == null || !floats.isArray)
        {
            return false;
        }

        for (int i = 0; i < floats.arraySize; i++)
        {
            SerializedProperty entry = floats.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = entry.FindPropertyRelative("first");
            if (nameProp != null && nameProp.stringValue == propertyName)
            {
                value = entry.FindPropertyRelative("second").floatValue;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadSerializedColor(Material mat, string propertyName, out Color value)
    {
        value = Color.white;
        if (mat == null)
        {
            return false;
        }

        SerializedObject so = new SerializedObject(mat);
        SerializedProperty colors = so.FindProperty("m_SavedProperties.m_Colors");
        if (colors == null || !colors.isArray)
        {
            return false;
        }

        for (int i = 0; i < colors.arraySize; i++)
        {
            SerializedProperty entry = colors.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = entry.FindPropertyRelative("first");
            if (nameProp != null && nameProp.stringValue == propertyName)
            {
                value = entry.FindPropertyRelative("second").colorValue;
                return true;
            }
        }

        return false;
    }

    [MenuItem("Tools/L2 Effects/Diagnose Spawn Region Debug")]
    private static void DiagnoseSpawnRegionDebug()
    {
        MarkCacheDirty();
        CollectRelevantRenderers(RendererScratch);
        int matchCount = 0;

        Debug.Log($"[L2 Spawn Debug] Relevant renderers: {RendererScratch.Count} " +
                  $"(prefab stage or selection only). PrefabStage={(PrefabStageUtility.GetCurrentPrefabStage() != null ? "open" : "none")}.");

        for (int i = 0; i < RendererScratch.Count; i++)
        {
            Renderer renderer = RendererScratch[i];
            Material mat = ResolveMaterial(renderer);
            if (mat == null)
            {
                continue;
            }

            matchCount++;
            Debug.Log(
                $"[L2 Spawn Debug] OK renderer='{renderer.name}' mat='{mat.name}' " +
                $"shader='{mat.shader.name}' path='{GetGameObjectPath(renderer.gameObject)}'",
                renderer.gameObject);
        }

        Debug.Log($"[L2 Spawn Debug] Active draw targets: {matchCount}. " +
                  "Pink wireframe = spawn region (matches MightTaSprite spawnOfs).");
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }

        return path;
    }

    private static Color ResolveDrawColor(Material mat)
    {
        Color c;
        if (mat.HasProperty(DebugSpawnRegionColorId))
        {
            c = mat.GetColor(DebugSpawnRegionColorId);
        }
        else if (TryReadSerializedColor(mat, "_DebugSpawnRegionColor", out Color serialized))
        {
            c = serialized;
        }
        else
        {
            c = new Color(1f, 0.15f, 0.55f, 1f);
        }

        if (c.r + c.g + c.b < 0.35f)
        {
            c = new Color(1f, 0.15f, 0.55f, 1f);
        }

        return c;
    }

    private static bool TryReadSpawnSettings(Material mat, out SpawnSettings settings)
    {
        settings = default;
        if (!mat.HasProperty(PolarRadiusId) ||
            !mat.HasProperty(PolarPitchDegId) ||
            !mat.HasProperty(PolarAzimuthDegId))
        {
            return false;
        }

        Vector4 offset = mat.HasProperty(StartLocationOffsetId)
            ? mat.GetVector(StartLocationOffsetId)
            : Vector4.zero;
        Vector4 rangeX = mat.HasProperty(StartLocationRangeXId)
            ? mat.GetVector(StartLocationRangeXId)
            : Vector4.zero;
        Vector4 rangeY = mat.HasProperty(StartLocationRangeYId)
            ? mat.GetVector(StartLocationRangeYId)
            : Vector4.zero;
        Vector4 rangeZ = mat.HasProperty(StartLocationRangeZId)
            ? mat.GetVector(StartLocationRangeZId)
            : Vector4.zero;
        Vector4 azimuth = mat.GetVector(PolarAzimuthDegId);
        Vector4 pitch = mat.GetVector(PolarPitchDegId);
        Vector4 radius = mat.GetVector(PolarRadiusId);

        settings.OffsetUe = new Vector3(offset.x, offset.y, offset.z);
        settings.RangeMinUe = new Vector3(rangeX.x, rangeY.x, rangeZ.x);
        settings.RangeMaxUe = new Vector3(rangeX.y, rangeY.y, rangeZ.y);
        settings.AzimuthMin = azimuth.x;
        settings.AzimuthMax = azimuth.y;
        settings.PitchMin = pitch.x;
        settings.PitchMax = pitch.y;
        settings.RadiusMin = radius.x;
        settings.RadiusMax = radius.y;
        return true;
    }

    private static Vector3 PolarOffsetUe(float thetaDeg, float phiDeg, float radius)
    {
        float theta = thetaDeg * Mathf.Deg2Rad;
        float phi = phiDeg * Mathf.Deg2Rad;
        float sinPhi = Mathf.Sin(phi);
        return new Vector3(
            radius * sinPhi * Mathf.Cos(theta),
            radius * sinPhi * Mathf.Sin(theta),
            radius * Mathf.Cos(phi));
    }

    // Same remap as L2Fx_UeVectorToUnity: UE (X,Y,Z) -> Unity (X,Z,Y), Z-up UE -> Y-up Unity.
    private static Vector3 UeToUnity(Vector3 ue)
    {
        return new Vector3(ue.x, ue.z, ue.y);
    }

    private struct CachedDrawEntry
    {
        public Transform Transform;
        public Material SourceMaterial;
        public int SettingsHash;
        public Color Color;
        public Vector3[] LocalLines;
    }

    private struct SpawnSettings
    {
        public Vector3 OffsetUe;
        public Vector3 RangeMinUe;
        public Vector3 RangeMaxUe;
        public float AzimuthMin;
        public float AzimuthMax;
        public float PitchMin;
        public float PitchMax;
        public float RadiusMin;
        public float RadiusMax;
    }
}
#endif
