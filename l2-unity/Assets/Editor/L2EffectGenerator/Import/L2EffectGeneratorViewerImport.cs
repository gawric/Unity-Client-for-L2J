#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Public import facade: pull a missing texture or static mesh into the Unity project.
/// Mesh Materials[] live in <see cref="L2EffectMeshMaterialResolver"/>.
/// </summary>
public static class L2EffectGeneratorViewerImport
{
    public const string TextureDestFolder = L2EffectPackageRoots.TextureDestFolder;
    public const string MeshDestFolder = L2EffectPackageRoots.MeshDestFolder;

    static readonly HashSet<string> FailedKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static Texture2D TryImportTexture(string textureReference)
    {
        if (!L2EffectImportUtil.TryParseRef(textureReference, out string package, out string objectName))
        {
            return null;
        }

        string destPath = TextureDestFolder + "/" + objectName + ".png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
        if (existing != null)
        {
            return existing;
        }

        string key = "tex:" + objectName;
        if (FailedKeys.Contains(key) || !IsAvailable())
        {
            return null;
        }

        string extractDir = L2EffectUModelClient.ExtractObject(package, objectName, texture: true);
        string png = L2EffectImportUtil.FindExported(extractDir, objectName, ".png", ".tga");
        if (string.IsNullOrEmpty(png))
        {
            FailedKeys.Add(key);
            return null;
        }

        if (!L2EffectImportUtil.CopyIntoProject(png, destPath))
        {
            FailedKeys.Add(key);
            return null;
        }

        Debug.Log("[L2EffectGenerator] imported texture " + objectName + " from ViewerData");
        return AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
    }

    public static Mesh TryImportMesh(string staticMeshReference)
    {
        if (!L2EffectImportUtil.TryParseRef(staticMeshReference, out string package, out string objectName))
        {
            return null;
        }

        string destPath = MeshDestFolder + "/" + objectName + ".asset";
        Mesh existing = L2EffectImportUtil.LoadProjectMesh(objectName);
        bool isOurAsset = existing != null &&
            string.Equals(
                AssetDatabase.GetAssetPath(existing),
                destPath,
                StringComparison.OrdinalIgnoreCase);
        if (existing != null && !isOurAsset)
        {
            return existing;
        }

        string key = "mesh:" + objectName;
        if (FailedKeys.Contains(key) || !IsAvailable())
        {
            return existing;
        }

        string extractDir = L2EffectUModelClient.ExtractObject(package, objectName, texture: false);
        string psk = L2EffectImportUtil.FindExported(extractDir, objectName, ".psk", ".pskx");
        if (string.IsNullOrEmpty(psk))
        {
            FailedKeys.Add(key);
            return existing;
        }

        int pskSlots = L2EffectGeneratorPskMesh.CountMaterialSlots(psk);
        if (existing != null && existing.subMeshCount >= Math.Max(1, pskSlots))
        {
            return existing;
        }

        L2EffectMeshMaterialResolver.CopyExtractedPngs(extractDir, objectName);
        L2EffectMeshMaterialResolver.RememberSidecarTextures(objectName, extractDir, psk);
        L2EffectMeshMaterialResolver.Ensure(staticMeshReference);

        try
        {
            L2EffectImportUtil.EnsureFolder(MeshDestFolder);
            if (existing != null)
            {
                L2EffectGeneratorPskMesh.Load(psk, existing);
                existing.name = objectName;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
            }
            else
            {
                Mesh mesh = L2EffectGeneratorPskMesh.Load(psk);
                mesh.name = objectName;
                AssetDatabase.CreateAsset(mesh, destPath);
                AssetDatabase.ImportAsset(destPath);
                existing = AssetDatabase.LoadAssetAtPath<Mesh>(destPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[L2EffectGenerator] ViewerData mesh convert skipped for " +
                objectName + ": " + ex.Message);
            FailedKeys.Add(key);
            return existing;
        }

        Debug.Log(
            "[L2EffectGenerator] imported mesh " + objectName +
            " submeshes=" + (existing != null ? existing.subMeshCount : 0) +
            " faces=" + L2EffectGeneratorPskMesh.FormatFaceCounts(existing));
        return existing;
    }

    public static void CollectSidecarTextures(string staticMeshReference, List<Texture2D> result)
    {
        L2EffectMeshMaterialResolver.CollectSidecarTextures(staticMeshReference, result);
    }

    public static L2EffectMeshPackageBinding EnsureMeshPackageBinding(string staticMeshReference)
    {
        return L2EffectMeshMaterialResolver.Ensure(staticMeshReference);
    }

    public static bool TryGetMeshPackageExtras(
        string staticMeshReference,
        out bool twoSided,
        out bool useVertexColor)
    {
        return L2EffectMeshMaterialResolver.TryGetExtras(
            staticMeshReference,
            out twoSided,
            out useVertexColor);
    }

    public static bool IsAvailable()
    {
        if (!File.Exists(L2EffectPackageRoots.FindUModelExe()))
        {
            return false;
        }

        foreach (string dir in L2EffectPackageRoots.MeshPackageDirs())
        {
            if (Directory.Exists(dir))
            {
                return true;
            }
        }

        foreach (string dir in L2EffectPackageRoots.TexturePackageDirs())
        {
            if (Directory.Exists(dir))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
