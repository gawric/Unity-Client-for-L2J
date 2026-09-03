#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resolves emitter textures: UC Texture=, then mesh Materials[] for MeshEmitter, then FBX/sidecar.
/// </summary>
public static class L2EffectTextureResolver
{
    public static List<Texture2D> Resolve(UcEmitterDefinition emitter, Mesh slotMesh)
    {
        var result = new List<Texture2D>();
        string[] meshOverrideNames;
        if (emitter != null &&
            L2EffectGeneratorAssetOverrides.TryGetTexturesForStaticMesh(
                emitter.StaticMeshReference,
                out meshOverrideNames) &&
            meshOverrideNames != null)
        {
            string meshName = L2EffectGeneratorAssetOverrides.GetUcObjectName(
                emitter.StaticMeshReference);
            for (int i = 0; i < meshOverrideNames.Length; i++)
            {
                string meshOverrideName = meshOverrideNames[i];
                Texture2D overrideTexture = FindByUcName(meshOverrideName);
                if (overrideTexture == null)
                {
                    Debug.LogWarning(
                        "[L2EffectGenerator] " + emitter.EmitterName +
                        ": mesh map " + meshName + " slot " + i +
                        " texture '" + meshOverrideName + "' not found");
                    continue;
                }

                result.Add(overrideTexture);
            }

            if (result.Count > 0)
            {
                return result;
            }
        }

        if (emitter != null &&
            string.Equals(emitter.ClassName, "MeshEmitter", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(emitter.TextureReference) &&
            !string.IsNullOrWhiteSpace(emitter.StaticMeshReference))
        {
            List<Texture2D> fromMesh = ResolveFromMeshPackage(emitter.StaticMeshReference);
            if (fromMesh.Count > 0)
            {
                return fromMesh;
            }
        }

        return Resolve(emitter != null ? emitter.TextureReference : null, slotMesh, emitter);
    }

    public static List<Texture2D> Resolve(string textureReference, Mesh slotMesh)
    {
        return Resolve(textureReference, slotMesh, null);
    }

    public static List<Texture2D> Resolve(
        string textureReference,
        Mesh slotMesh,
        UcEmitterDefinition emitter)
    {
        var result = new List<Texture2D>();
        if (!string.IsNullOrWhiteSpace(textureReference))
        {
            Texture2D named = FindByUcName(ObjectName(textureReference)) ??
                              L2EffectGeneratorViewerImport.TryImportTexture(textureReference);
            if (named != null)
            {
                AddUnique(result, named);
                return result;
            }
        }

        CollectMeshTextures(slotMesh, result);
        if (emitter != null)
        {
            L2EffectGeneratorViewerImport.CollectSidecarTextures(emitter.StaticMeshReference, result);
        }

        return result;
    }

    public static List<Texture2D> ResolveFromMeshPackage(string staticMeshReference)
    {
        var result = new List<Texture2D>();
        L2EffectMeshPackageBinding binding =
            L2EffectMeshMaterialResolver.Ensure(staticMeshReference);
        for (int i = 0; i < binding.TextureNames.Count; i++)
        {
            string name = binding.TextureNames[i];
            Texture2D texture = FindByUcName(name) ??
                                L2EffectGeneratorViewerImport.TryImportTexture(name);
            AddUnique(result, texture);
        }

        return result;
    }

    public static Texture2D FindByUcName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.IndexOfAny(new[] { ':', '/', '\\', '"', '*', '?', '<', '>', '|' }) >= 0)
        {
            return null;
        }

        string[] folders =
        {
            "Assets/Resources/Data/SysTextures/LineageEffectsTextures",
            "Assets/Resources/Data/SysTextures/LineageEffectsTextures/Particles",
            "Assets/Resources/Data/SysTextures/LineageEffectsTextures/SRGB",
            "Assets/Resources/Data/Textures/FX_E_T",
            "Assets/Resources/Data/SysTextures"
        };
        string[] extensions = { ".png", ".tga", ".jpg", ".jpeg", ".psd" };
        for (int folderIndex = 0; folderIndex < folders.Length; folderIndex++)
        {
            for (int extIndex = 0; extIndex < extensions.Length; extIndex++)
            {
                Texture2D direct = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    folders[folderIndex] + "/" + fileName + extensions[extIndex]);
                if (direct != null)
                {
                    return direct;
                }
            }
        }

        string materialPath =
            "Assets/Resources/Data/SysTextures/LineageEffectsTextures/Materials/" + fileName + ".mat";
        Texture2D fromPackageMaterial = TextureFromMaterial(
            AssetDatabase.LoadAssetAtPath<Material>(materialPath));
        if (fromPackageMaterial != null)
        {
            return fromPackageMaterial;
        }

        string[] guids;
        try
        {
            guids = AssetDatabase.FindAssets(fileName + " t:Texture2D", new[] { "Assets/Resources/Data" });
        }
        catch (Exception)
        {
            return null;
        }

        Texture2D fallback = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                continue;
            }

            if (string.Equals(texture.name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                if (path.IndexOf("LineageEffectsTextures", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return texture;
                }

                fallback ??= texture;
            }
            else
            {
                fallback ??= texture;
            }
        }

        return fallback;
    }

    static void CollectMeshTextures(Mesh slotMesh, List<Texture2D> result)
    {
        if (slotMesh == null)
        {
            return;
        }

        string meshPath = AssetDatabase.GetAssetPath(slotMesh);
        if (string.IsNullOrEmpty(meshPath))
        {
            return;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(meshPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is Material importedMaterial)
            {
                AddUnique(result, TextureFromMaterial(importedMaterial));
            }
        }

        string[] dependencies = AssetDatabase.GetDependencies(meshPath, true);
        for (int i = 0; i < dependencies.Length; i++)
        {
            AddUnique(result, AssetDatabase.LoadAssetAtPath<Texture2D>(dependencies[i]));
            AddUnique(result, TextureFromMaterial(
                AssetDatabase.LoadAssetAtPath<Material>(dependencies[i])));
        }
    }

    static Texture2D TextureFromMaterial(Material material)
    {
        if (material == null)
        {
            return null;
        }

        Texture texture = material.GetTexture("_BaseMap") ??
                          material.GetTexture("_MainTex") ??
                          material.GetTexture("_MainTexture") ??
                          material.mainTexture;
        return texture as Texture2D;
    }

    static void AddUnique(List<Texture2D> textures, Texture2D texture)
    {
        if (texture != null && !textures.Contains(texture))
        {
            textures.Add(texture);
        }
    }

    static string ObjectName(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return string.Empty;
        }

        int dot = reference.LastIndexOf('.');
        return dot >= 0 ? reference.Substring(dot + 1) : Path.GetFileNameWithoutExtension(reference);
    }
}
#endif
