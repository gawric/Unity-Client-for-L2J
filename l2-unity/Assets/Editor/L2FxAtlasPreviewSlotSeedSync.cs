#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Edit-mode atlas preview uses shared materials (_Seed=0 on asset), so every particle slot
/// spawns at the same point. Assigns a stable per-slot _Seed via MaterialPropertyBlock while
/// _DebugAtlasPreview is on and _StartTime is still 0.
/// </summary>
[InitializeOnLoad]
public static class L2FxAtlasPreviewSlotSeedSync
{
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int DebugAtlasPreviewId = Shader.PropertyToID("_DebugAtlasPreview");
    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");

    private static readonly List<ParticleGroup> GroupScratch = new List<ParticleGroup>(32);
    private static readonly List<ParticleSingle> SingleScratch = new List<ParticleSingle>(16);
    private static readonly List<Renderer> RendererScratch = new List<Renderer>(64);

    private static int _lastSyncFrame = -1;

    static L2FxAtlasPreviewSlotSeedSync()
    {
        EditorApplication.hierarchyChanged += Invalidate;
        Selection.selectionChanged += Invalidate;
        PrefabStage.prefabStageOpened += _ => Invalidate();
        PrefabStage.prefabStageClosing += _ => Invalidate();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void Invalidate()
    {
        _lastSyncFrame = -1;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.EnteredPlayMode)
        {
            ClearManagedSlotPropertyBlocks();
        }

        Invalidate();
    }

    private static void OnSceneGui(SceneView view)
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (UnityEngine.Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (_lastSyncFrame == Time.frameCount)
        {
            return;
        }

        _lastSyncFrame = Time.frameCount;
        SyncAll();
    }

    private static void SyncAll()
    {
        CollectEffectParts(GroupScratch, SingleScratch);

        for (int i = 0; i < GroupScratch.Count; i++)
        {
            SyncEffectPart(GroupScratch[i]);
        }

        for (int i = 0; i < SingleScratch.Count; i++)
        {
            SyncEffectPart(SingleScratch[i]);
        }
    }

    private static void CollectEffectParts(List<ParticleGroup> groups, List<ParticleSingle> singles)
    {
        groups.Clear();
        singles.Clear();

        ParticleGroup[] sceneGroups = Object.FindObjectsByType<ParticleGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneGroups.Length; i++)
        {
            AddUnique(groups, sceneGroups[i]);
        }

        ParticleSingle[] sceneSingles = Object.FindObjectsByType<ParticleSingle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneSingles.Length; i++)
        {
            AddUnique(singles, sceneSingles[i]);
        }

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && prefabStage.prefabContentsRoot != null)
        {
            ParticleGroup[] prefabGroups = prefabStage.prefabContentsRoot.GetComponentsInChildren<ParticleGroup>(true);
            for (int i = 0; i < prefabGroups.Length; i++)
            {
                AddUnique(groups, prefabGroups[i]);
            }

            ParticleSingle[] prefabSingles = prefabStage.prefabContentsRoot.GetComponentsInChildren<ParticleSingle>(true);
            for (int i = 0; i < prefabSingles.Length; i++)
            {
                AddUnique(singles, prefabSingles[i]);
            }
        }

        GameObject[] selection = Selection.gameObjects;
        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i] == null)
            {
                continue;
            }

            ParticleGroup[] selectedGroups = selection[i].GetComponentsInChildren<ParticleGroup>(true);
            for (int g = 0; g < selectedGroups.Length; g++)
            {
                AddUnique(groups, selectedGroups[g]);
            }

            ParticleSingle[] selectedSingles = selection[i].GetComponentsInChildren<ParticleSingle>(true);
            for (int s = 0; s < selectedSingles.Length; s++)
            {
                AddUnique(singles, selectedSingles[s]);
            }
        }
    }

    private static void AddUnique<T>(List<T> list, T item) where T : Object
    {
        if (item != null && !list.Contains(item))
        {
            list.Add(item);
        }
    }

    private static void SyncEffectPart(Component effectPart)
    {
        if (effectPart == null)
        {
            return;
        }

        Renderer[] slots = GetParticleSlots(effectPart);
        if (slots == null || slots.Length == 0)
        {
            return;
        }

        bool anyAtlasPreview = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (ShouldUsePerSlotSeed(slots[i]))
            {
                anyAtlasPreview = true;
                break;
            }
        }

        if (!anyAtlasPreview)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                ClearPropertyBlock(slots[i]);
            }

            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            Renderer renderer = slots[i];
            if (renderer == null || !ShouldUsePerSlotSeed(renderer))
            {
                ClearPropertyBlock(renderer);
                continue;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetFloat(SeedId, SlotSeed(i));
            renderer.SetPropertyBlock(block);
        }
    }

    private static bool ShouldUsePerSlotSeed(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Material mat = renderer.sharedMaterial;
        if (mat == null)
        {
            return false;
        }

        if (!mat.HasProperty(DebugAtlasPreviewId) || mat.GetFloat(DebugAtlasPreviewId) < 0.5f)
        {
            return false;
        }

        if (mat.HasProperty(StartTimeId) && mat.GetFloat(StartTimeId) > 1e-4f)
        {
            return false;
        }

        return mat.HasProperty(SeedId);
    }

    private static float SlotSeed(int slotIndex)
    {
        // Stable spread; matches debug drawer sample spacing.
        return (slotIndex + 1) * 17.31f;
    }

    private static Renderer[] GetParticleSlots(Component effectPart)
    {
        SerializedObject so = new SerializedObject(effectPart);
        SerializedProperty particles = so.FindProperty("_particles");
        if (particles == null || !particles.isArray || particles.arraySize == 0)
        {
            return null;
        }

        RendererScratch.Clear();
        for (int i = 0; i < particles.arraySize; i++)
        {
            Renderer renderer = particles.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
            if (renderer != null)
            {
                RendererScratch.Add(renderer);
            }
        }

        return RendererScratch.Count == 0 ? null : RendererScratch.ToArray();
    }

    private static void ClearPropertyBlock(Renderer renderer)
    {
        if (renderer != null)
        {
            renderer.SetPropertyBlock(null);
        }
    }

    private static void ClearManagedSlotPropertyBlocks()
    {
        CollectEffectParts(GroupScratch, SingleScratch);

        for (int i = 0; i < GroupScratch.Count; i++)
        {
            ClearSlots(GetParticleSlots(GroupScratch[i]));
        }

        for (int i = 0; i < SingleScratch.Count; i++)
        {
            ClearSlots(GetParticleSlots(SingleScratch[i]));
        }
    }

    private static void ClearSlots(Renderer[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            ClearPropertyBlock(slots[i]);
        }
    }
}
#endif
