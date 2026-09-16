using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>
/// Compositor setup + cleanup menus.
/// </summary>
public static class L2FxCompositorEditorMenus
{
    const string EffectsRoot = "Assets/Resources/Data/Effects";
    const string RendererPath = "Assets/Rendering/URP/URP_Renderer.asset";
    const string UltraLowRendererPath = "Assets/Rendering/URP/URP_Renderer_UltraLow.asset";
    const string TransferShaderPath =
        "Assets/Resources/Data/Shaders/Skills/Common/Decompile_Common/L2FxColorTransfer.shader";
    const string PostShaderPath =
        "Assets/Resources/Data/Shaders/Skills/Common/Decompile_Common/L2FxPostBloomContrast.shader";

    // Everything except SkillEffect (18), L2Sky (20), L2Haze (21), L2Clouds (22).
    const uint TransparentMaskWithoutCompositorLayers =
        0xFFFFFFFFu
        & ~(1u << L2FxCompositorLayers.SkillEffect)
        & ~(1u << L2FxCompositorLayers.L2Sky)
        & ~(1u << L2FxCompositorLayers.L2Haze)
        & ~(1u << L2FxCompositorLayers.L2Clouds);

    [MenuItem("L2/Effects/Compositor/Repair Now (Mask + Wire Feature)")]
    public static void RepairNow()
    {
        // Wire feature first — SerializedObject edits were zeroing the transparent mask.
        EnsureFeatureOnRenderer(true);
        RepairTransparentMasks();
        if (!VerifyTransparentMasks())
        {
            EditorUtility.DisplayDialog(
                "L2Fx Compositor",
                "Transparent mask still wrong after repair.\n" +
                "Nameplates will stay invisible until mask != 0.\n" +
                "Check Console.",
                "OK");
            return;
        }

        Debug.Log(
            "[L2FxCompositor] Repaired transparent masks and wired feature. Re-enter Play Mode.");
    }

    [MenuItem("L2/Effects/Compositor/Enable (Migrate + Exclude Transparent + Activate Feature)")]
    public static void EnableFullSetup()
    {
        if (!EditorUtility.DisplayDialog(
                "L2Fx Compositor Enable",
                "This will:\n" +
                "• Set layer SkillEffect on all effect prefabs under Assets/Resources/Data/Effects\n" +
                "• Exclude SkillEffect, L2Sky, L2Haze and L2Clouds from URP default Transparent mask\n" +
                "• Add/activate L2FxCompositorRendererFeature on URP_Renderer\n\n" +
                "It does NOT edit the open scene asset.\nContinue?",
                "Enable",
                "Cancel"))
        {
            return;
        }

        MigratePrefabsToSkillEffect();
        EnsureFeatureOnRenderer(true);
        RepairTransparentMasks();
        VerifyTransparentMasks();
        Debug.Log("[L2FxCompositor] Enabled. Re-enter Play Mode and check RenderDoc for 'L2Fx D3D9 Compositor'.");
    }

    [MenuItem("L2/Effects/Compositor/Disable Feature Only")]
    public static void DisableFeatureOnly()
    {
        EnsureFeatureOnRenderer(false);
        L2FxCompositorRuntime.SetPreferGpuQueue(false);
        Debug.Log("[L2FxCompositor] Feature deactivated.");
    }

    [MenuItem("L2/Effects/Compositor/Migrate Generated Prefabs To SkillEffect")]
    public static void MigratePrefabsToSkillEffect()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EffectsRoot });
        int changed = 0;
        int skipped = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.IndexOf("_deprecated", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("_deprecate", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                skipped++;
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                L2FxCompositorLayers.ApplySkillEffectLayer(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            "[L2FxCompositor] Migrated " + changed + " prefabs to SkillEffect; skipped " +
            skipped + " deprecated.");
    }

    [MenuItem("L2/Effects/Compositor/Exclude SkillEffect From Default Transparent")]
    public static void ExcludeSkillEffectFromTransparent()
    {
        RepairTransparentMasks();
        Debug.Log("[L2FxCompositor] Transparent masks set to Everything except SkillEffect, L2Sky, L2Haze and L2Clouds.");
    }

    [MenuItem("L2/Effects/Compositor/Fix Missing RendererFeatures Warning")]
    public static void FixMissingRendererFeatures()
    {
        CleanupRenderer(RendererPath, log: true);
        CleanupRenderer(UltraLowRendererPath, log: true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[L2FxCompositor] Renderer feature lists cleaned.");
    }

    static void RepairTransparentMasks()
    {
        SetTransparentMask(RendererPath, TransparentMaskWithoutCompositorLayers);
        string ultraFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", UltraLowRendererPath));
        if (File.Exists(ultraFull))
            SetTransparentMask(UltraLowRendererPath, TransparentMaskWithoutCompositorLayers);
        AssetDatabase.SaveAssets();
    }

    static bool VerifyTransparentMasks()
    {
        bool ok = VerifyTransparentMask(RendererPath, TransparentMaskWithoutCompositorLayers);
        string ultraFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", UltraLowRendererPath));
        if (File.Exists(ultraFull))
            ok &= VerifyTransparentMask(UltraLowRendererPath, TransparentMaskWithoutCompositorLayers);
        return ok;
    }

    static bool VerifyTransparentMask(string rendererAssetPath, uint expected)
    {
        UniversalRendererData renderer =
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
        if (renderer == null)
            return false;

        uint got = unchecked((uint)(int)renderer.transparentLayerMask);
        bool ok = got == expected && got != 0u;
        Debug.Log(
            "[L2FxCompositor] VERIFY " + rendererAssetPath + " transparent mask got=" + got +
            " expected=" + expected + " ok=" + ok);
        return ok;
    }

    static void SetTransparentMask(string rendererAssetPath, uint bits)
    {
        // 1) Patch YAML on disk — SerializedProperty writes were silently storing 0.
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rendererAssetPath));
        if (File.Exists(fullPath))
        {
            string text = File.ReadAllText(fullPath);
            string patched = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"(m_TransparentLayerMask:\s*\r?\n\s*serializedVersion:\s*2\r?\n\s*m_Bits:\s*)\d+",
                "${1}" + bits,
                System.Text.RegularExpressions.RegexOptions.Multiline);
            if (patched != text)
            {
                File.WriteAllText(fullPath, patched);
                AssetDatabase.ImportAsset(rendererAssetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        UniversalRendererData renderer =
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
        if (renderer == null)
            return;

        // 2) Also set live object (no SerializedObject — that path zeroed the mask).
        renderer.transparentLayerMask = unchecked((int)bits);
        EditorUtility.SetDirty(renderer);

        uint got = unchecked((uint)(int)renderer.transparentLayerMask);
        Debug.Log(
            "[L2FxCompositor] " + rendererAssetPath + " transparent mask set=" + bits +
            " readback=" + got + (got == 0u ? " FAILED" : " ok"));
    }

    static void EnsureFeatureOnRenderer(bool active)
    {
        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            Debug.LogWarning("[L2FxCompositor] Missing " + RendererPath);
            return;
        }

        L2FxCompositorRendererFeature feature = FindExistingFeature();
        bool created = false;
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<L2FxCompositorRendererFeature>();
            feature.name = "L2FxCompositorRendererFeature";
            feature.settings = new L2FxCompositorSettings
            {
                enableD3D9Compositor = true,
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
                effectLayerMask = 1 << L2FxCompositorLayers.SkillEffect,
                celestialLayerMask = 1 << L2FxCompositorLayers.L2Sky,
                hazeLayerMask = 1 << L2FxCompositorLayers.L2Haze,
                cloudLayerMask = 1 << L2FxCompositorLayers.L2Clouds,
                includeSkyInBoost = false,
                skyBoostStart = 0.30f,
                skyBoostFull = 0.50f,
                encodeLinearToSrgb = true,
                decodeSrgbToLinear = true,
                bindCameraDepth = true,
                gameCameraOnly = true,
                enableSkillPost = true
            };

            AssetDatabase.AddObjectToAsset(feature, renderer);
            created = true;
        }

        BindFeatureShaders(feature);

        // Always ensure the feature is listed (orphaned sub-assets were the prior bug).
        LinkFeatureInRendererList(renderer, feature);

        feature.settings.enableD3D9Compositor = active;
        feature.SetActive(active);
        L2FxCompositorRuntime.SetPreferGpuQueue(active);

        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[L2FxCompositor] Feature " + (created ? "created+" : "") +
            "wired, active=" + active + ".");
    }

    static void BindFeatureShaders(L2FxCompositorRendererFeature feature)
    {
        Shader transfer = AssetDatabase.LoadAssetAtPath<Shader>(TransferShaderPath);
        if (transfer == null)
            transfer = Shader.Find("Hidden/L2/FxColorTransfer");
        Shader post = AssetDatabase.LoadAssetAtPath<Shader>(PostShaderPath);
        if (post == null)
            post = Shader.Find("Hidden/L2/FxPostBloomContrast");

        SerializedObject featureSo = new SerializedObject(feature);
        SerializedProperty shaderProp = featureSo.FindProperty("transferShader");
        if (shaderProp != null)
            shaderProp.objectReferenceValue = transfer;
        SerializedProperty postProp = featureSo.FindProperty("postShader");
        if (postProp != null)
            postProp.objectReferenceValue = post;
        featureSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void LinkFeatureInRendererList(
        UniversalRendererData renderer,
        L2FxCompositorRendererFeature feature)
    {
        SerializedObject rendererSo = new SerializedObject(renderer);
        rendererSo.Update();
        SerializedProperty features = rendererSo.FindProperty("m_RendererFeatures");
        SerializedProperty map = rendererSo.FindProperty("m_RendererFeatureMap");
        if (features == null)
            return;

        for (int i = 0; i < features.arraySize; i++)
        {
            if (features.GetArrayElementAtIndex(i).objectReferenceValue == feature)
                return;
        }

        int index = features.arraySize;
        features.arraySize = index + 1;
        features.GetArrayElementAtIndex(index).objectReferenceValue = feature;

        if (map != null)
        {
            if (map.isArray)
            {
                map.arraySize = features.arraySize;
                // Unique non-zero map id (URP uses these to track feature instances).
                map.GetArrayElementAtIndex(index).longValue =
                    System.DateTime.UtcNow.Ticks ^ feature.GetInstanceID();
            }
        }

        rendererSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static L2FxCompositorRendererFeature FindExistingFeature()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RendererPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is L2FxCompositorRendererFeature feature)
                return feature;
        }

        return null;
    }

    static void CleanupRenderer(string path, bool log = false)
    {
        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if (renderer == null)
            return;

        SerializedObject so = new SerializedObject(renderer);
        so.Update();
        SerializedProperty features = so.FindProperty("m_RendererFeatures");
        SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
        if (features == null)
            return;

        int removed = 0;
        for (int i = features.arraySize - 1; i >= 0; i--)
        {
            if (features.GetArrayElementAtIndex(i).objectReferenceValue != null)
                continue;

            features.DeleteArrayElementAtIndex(i);
            if (map != null && map.isArray && i < map.arraySize)
                map.DeleteArrayElementAtIndex(i);
            removed++;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);

        if (log)
            Debug.Log("[L2FxCompositor] " + path + " removed null slots: " + removed);
    }
}
