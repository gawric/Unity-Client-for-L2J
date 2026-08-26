#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class L2EffectGeneratorMaterialConfigurator
{
    public const string SpriteShaderName = "L2/Effects/SpriteEmitter";
    public const string MeshShaderName = "L2/Effects/MeshEmitter";

    public static Shader ResolveShader(string className)
    {
        string shaderName = IsMeshEmitter(className) ? MeshShaderName : SpriteShaderName;
        return Shader.Find(shaderName);
    }

    public static string Configure(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter,
        Mesh slotMesh,
        Texture2D textureOverride = null)
    {
        if (material == null || emitter == null)
            return "material configuration skipped";

        bool isMesh = IsMeshEmitter(emitter.ClassName);
        Shader shader = ResolveShader(emitter.ClassName);
        if (shader == null)
            return "unified shader is missing";
        material.shader = shader;
        // Deferred URP: Geometry queue + UniversalForward never draws unlit meshes.
        material.renderQueue = (int)RenderQueue.Transparent;
        material.enableInstancing = emitter.MaxParticles > 1;

        ConfigureRenderState(material, emitter);
        ConfigureCommon(material, emitter);
        if (isMesh)
            ConfigureMesh(material, emitter);
        else
            ConfigureSprite(material, emitter);

        List<Texture2D> resolvedTextures = ResolveTextures(emitter.TextureReference, slotMesh);
        Texture2D texture = textureOverride ??
                            (resolvedTextures.Count > 0 ? resolvedTextures[0] : null);
        if (texture != null)
            SetTexture(material, "_MainTex", texture);

        EditorUtility.SetDirty(material);
        return texture != null
            ? "unified material configured, texture=" + texture.name
            : "unified material configured, texture unresolved";
    }

    static void ConfigureCommon(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        SetVector(material, "_LifetimeRange", RangeVector(
            emitter.HasLifetimeRange ? emitter.LifetimeMin : 1f,
            emitter.HasLifetimeRange ? emitter.LifetimeMax : 1f));
        // EffectPart._startDelay owns the UC emitter delay. Keep the shader
        // range at zero so the unified shader consumes its TLS draw without
        // applying the same delay a second time.
        SetVector(material, "_InitialDelayRange", RangeVector(0f, 0f));
        SetVector(material, "_StartLocationOffsetUc", new Vector4(
            emitter.StartLocationOffset.x,
            emitter.StartLocationOffset.y,
            emitter.StartLocationOffset.z,
            0f));
        SetVectorRange(material, "_StartLocationRange", emitter.StartLocationRange);
        SetVectorRange(material, "_StartVelocityRange", emitter.StartVelocityRange);
        SetVector(material, "_AccelerationUc", new Vector4(
            emitter.Acceleration.x, emitter.Acceleration.y, emitter.Acceleration.z, 0f));
        SetVector(material, "_VelocityLossRangeUc", new Vector4(
            emitter.VelocityLossRange.X.Max,
            emitter.VelocityLossRange.Y.Max,
            emitter.VelocityLossRange.Z.Max,
            0f));

        SetFloat(material, "_FadeIn", emitter.FadeIn ? 1f : 0f);
        SetFloat(material, "_FadeInEndTime", emitter.FadeInEndTime);
        SetFloat(material, "_FadeOut", emitter.FadeOut ? 1f : 0f);
        SetFloat(material, "_Fadeout", emitter.FadeOut ? 1f : 0f);
        SetFloat(material, "_FadeOutStartTime", emitter.FadeOutStartTime);
        SetFloat(material, "_FadeoutStartTime", emitter.FadeOutStartTime);
        SetFloat(material, "_Opacity", emitter.Opacity);
        SetFloat(material, "_ColorScaleRepeats", emitter.ColorScaleRepeats);
        SetFloat(material, "_ColorScaleParam", emitter.ColorScaleRepeats);
        SetColorMultiplier(material, emitter.ColorMultiplierRange);
        ConfigureColorKeys(material, emitter);
        ConfigureSizeKeys(material, emitter);
    }

    static void ConfigureMesh(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        SetFloat(material, "_SpawnMode", 2f);
        SetFloat(material, "_MotionMode", ResolveMotionMode(emitter));
        SetFloat(material, "_TransformMode",
            string.Equals(emitter.UseRotationFrom, "PTRS_Actor", StringComparison.OrdinalIgnoreCase)
                ? 1f
                : 0f);
        SetFloat(material, "_OffsetSource", 0f);
        SetFloat(material, "_SizeMode", emitter.UniformSize ? 0f : 2f);
        SetFloat(material, "_SpinSpsMode", 1f);
        SetFloat(material, "_SpinParticles", emitter.SpinParticles ? 1f : 0f);
        SetFloat(material, "_ExpandShaderBounds", emitter.MaxParticles <= 1 ? 1f : 0f);

        SetVectorRange(material, "_StartSizeRange", emitter.StartSizeRange);
        SetVector(material, "_StartSizeRange",
            RangeVector(emitter.StartSizeRange.X.Min, emitter.StartSizeRange.X.Max));
        SetFloat(material, "_StartSize",
            Midpoint(emitter.StartSizeRange.X));
        SetFloat(material, "_StartSizeXY",
            Midpoint(emitter.StartSizeRange.X));
        SetVector(material, "_StartSizeZRange",
            RangeVector(emitter.StartSizeRange.Z.Min, emitter.StartSizeRange.Z.Max));
        SetVector(material, "_StartSpinYawRangeUc", RangeVector(
            emitter.StartSpinRange.X.Min, emitter.StartSpinRange.X.Max));
        SetVector(material, "_StartSpinPitchRangeUc", RangeVector(
            emitter.StartSpinRange.Y.Min, emitter.StartSpinRange.Y.Max));
        SetVector(material, "_StartSpinRollRangeUc", RangeVector(
            emitter.StartSpinRange.Z.Min, emitter.StartSpinRange.Z.Max));
        SetVector(material, "_SpsYawRangeUc", RangeVector(
            emitter.SpinsPerSecondRange.X.Min, emitter.SpinsPerSecondRange.X.Max));
        SetVector(material, "_SpsPitchRangeUc", RangeVector(
            emitter.SpinsPerSecondRange.Y.Min, emitter.SpinsPerSecondRange.Y.Max));
        SetVector(material, "_SpsRollRangeUc", RangeVector(
            emitter.SpinsPerSecondRange.Z.Min, emitter.SpinsPerSecondRange.Z.Max));
        SetVector(material, "_SpsYawPitchRollUc", new Vector4(
            Midpoint(emitter.SpinsPerSecondRange.X),
            Midpoint(emitter.SpinsPerSecondRange.Y),
            Midpoint(emitter.SpinsPerSecondRange.Z),
            0f));
        SetVector(material, "_SpinCCWorCW", new Vector4(
            emitter.SpinCcwOrCw.x,
            emitter.SpinCcwOrCw.y,
            emitter.SpinCcwOrCw.z,
            0f));
    }

    static void ConfigureSprite(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        bool polar = string.Equals(
            emitter.StartLocationShape, "PTLS_Polar", StringComparison.OrdinalIgnoreCase);
        SetFloat(material, "_SpawnMode", 3f);
        SetFloat(material, "_FullTlsShape", polar ? 1f : 0f);
        SetFloat(material, "_MotionMode", ResolveMotionMode(emitter));
        SetFloat(material, "_OrientationMode",
            string.Equals(emitter.UseDirectionAs, "PTDU_Up", StringComparison.OrdinalIgnoreCase)
                ? 1f
                : 0f);
        SetFloat(material, "_PtvdMode", ResolvePtvdMode(emitter.GetVelocityDirectionFrom));
        SetFloat(material, "_SizeMode", emitter.UniformSize ? 0f : 1f);
        SetFloat(material, "_SpinMode", emitter.SpinParticles ? 1f : 0f);
        SetFloat(material, "_FlipbookMode", ResolveFlipbookMode(emitter));
        SetFloat(material, "_ColorFadeAlphaBlend",
            string.Equals(emitter.DrawStyle, "PTDS_AlphaBlend", StringComparison.OrdinalIgnoreCase)
                ? 1f
                : 0f);

        SetVector(material, "_PolarThetaRangeUc", RangeVector(
            emitter.StartLocationPolarRange.X.Min, emitter.StartLocationPolarRange.X.Max));
        SetVector(material, "_PolarPhiRangeUc", RangeVector(
            emitter.StartLocationPolarRange.Y.Min, emitter.StartLocationPolarRange.Y.Max));
        SetVector(material, "_PolarRadiusRangeUc", RangeVector(
            emitter.StartLocationPolarRange.Z.Min, emitter.StartLocationPolarRange.Z.Max));
        SetVector(material, "_SizeRange", RangeVector(
            emitter.StartSizeRange.X.Min, emitter.StartSizeRange.X.Max));
        SetVectorRange(material, "_SizeRange", emitter.StartSizeRange);
        SetVector(material, "_SpriteSpinStartRangeUc", RangeVector(
            emitter.StartSpinRange.X.Min, emitter.StartSpinRange.X.Max));
        SetVector(material, "_SpriteSpinSpsRangeUc", RangeVector(
            emitter.SpinsPerSecondRange.X.Min, emitter.SpinsPerSecondRange.X.Max));
        SetVector(material, "_SpriteSpinCcwOrCw", new Vector4(
            emitter.SpinCcwOrCw.x,
            emitter.SpinCcwOrCw.y,
            emitter.SpinCcwOrCw.z,
            0f));

        SetFloat(material, "_TextureUSubdivisions", Math.Max(1, emitter.TextureUSubdivisions));
        SetFloat(material, "_TextureVSubdivisions", Math.Max(1, emitter.TextureVSubdivisions));
        SetFloat(material, "_SubdivisionStart", emitter.SubdivisionStart);
        SetFloat(material, "_SubdivisionEnd", emitter.SubdivisionEnd);
        SetFloat(material, "_StaticSubdivision", emitter.SubdivisionStart);
    }

    static void ConfigureRenderState(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        BlendMode src = BlendMode.One;
        BlendMode dst = BlendMode.One;
        bool zWrite = false;
        switch (emitter.DrawStyle ?? string.Empty)
        {
            case "PTDS_Regular":
                src = BlendMode.One;
                dst = BlendMode.Zero;
                zWrite = true;
                break;
            case "PTDS_AlphaBlend":
                src = BlendMode.SrcAlpha;
                dst = BlendMode.OneMinusSrcAlpha;
                break;
            case "PTDS_Modulated":
                src = BlendMode.DstColor;
                dst = BlendMode.SrcColor;
                break;
            case "PTDS_AlphaModulate_MightNotFogCorrectly":
                src = BlendMode.One;
                dst = BlendMode.OneMinusSrcAlpha;
                break;
            case "PTDS_Darken":
                src = BlendMode.Zero;
                dst = BlendMode.OneMinusSrcColor;
                break;
            case "PTDS_Brighten":
                src = BlendMode.One;
                dst = BlendMode.OneMinusSrcColor;
                break;
        }
        SetFloat(material, "_SrcBlend", (float)src);
        SetFloat(material, "_DstBlend", (float)dst);
        SetFloat(material, "_ZWrite", zWrite ? 1f : 0f);
        bool cullOff = !IsMeshEmitter(emitter.ClassName) || emitter.RenderTwoSided;
        SetFloat(material, "_Cull", cullOff ? (float)CullMode.Off : (float)CullMode.Back);
    }

    static void ConfigureColorKeys(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        var keys = new List<L2EffectUcEmitterParser.UcColorScaleKey>(emitter.ColorScaleKeys);
        keys.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (keys.Count == 0)
        {
            keys.Add(new L2EffectUcEmitterParser.UcColorScaleKey
            {
                Index = 0,
                RelativeTime = 0f,
                Color = Color.white
            });
        }

        int capacity = IsMeshEmitter(emitter.ClassName) ? 6 : 4;
        int count = Math.Min(keys.Count, capacity);
        SetFloat(material, "_ColorScaleCount", count);
        for (int i = 0; i < capacity; i++)
        {
            L2EffectUcEmitterParser.UcColorScaleKey key = keys[Math.Min(i, count - 1)];
            SetColor(material, "_ColorKey" + i, key.Color);
            if (i > 0)
                SetFloat(material, "_ColorKey" + i + "Time", key.RelativeTime);
        }
    }

    static void ConfigureSizeKeys(
        Material material,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        var keys = new List<L2EffectUcEmitterParser.UcSizeScaleKey>(emitter.SizeScaleKeys);
        keys.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (keys.Count == 0)
        {
            keys.Add(new L2EffectUcEmitterParser.UcSizeScaleKey
            {
                Index = 0,
                RelativeTime = 0f,
                RelativeSize = 1f
            });
        }

        int count = Math.Min(keys.Count, 5);
        SetFloat(material, "_UseSizeScale", emitter.UseSizeScale ? 1f : 0f);
        SetFloat(material, "_SizeScaleRepeats", emitter.SizeScaleRepeats);
        SetFloat(material, "_SizeScaleCount", count);
        SetFloat(material, "_SizeKeyCount", count);
        for (int i = 0; i < 5; i++)
        {
            L2EffectUcEmitterParser.UcSizeScaleKey key = keys[Math.Min(i, count - 1)];
            SetVector(material, "_SizeKey" + i,
                new Vector4(key.RelativeTime, key.RelativeSize, 0f, 0f));
        }
    }

    public static List<Texture2D> ResolveTextures(string textureReference, Mesh slotMesh)
    {
        var result = new List<Texture2D>();
        if (!string.IsNullOrWhiteSpace(textureReference))
        {
            string fileName = GetUcObjectName(textureReference);
            string[] guids = AssetDatabase.FindAssets(
                fileName + " t:Texture2D",
                new[] { "Assets/Resources/Data" });
            Texture2D fallback = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                    continue;
                fallback ??= texture;
                if (string.Equals(texture.name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    AddUnique(result, texture);
                    return result;
                }
            }
            if (fallback != null)
            {
                AddUnique(result, fallback);
                return result;
            }
        }

        if (slotMesh == null)
            return result;
        string meshPath = AssetDatabase.GetAssetPath(slotMesh);
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(meshPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is Material importedMaterial)
            {
                Texture texture = importedMaterial.GetTexture("_BaseMap") ??
                                  importedMaterial.GetTexture("_MainTex");
                if (texture is Texture2D texture2D)
                    AddUnique(result, texture2D);
            }
        }

        string[] dependencies = AssetDatabase.GetDependencies(meshPath, true);
        for (int i = 0; i < dependencies.Length; i++)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(dependencies[i]);
            if (texture != null)
                AddUnique(result, texture);
            Material dependencyMaterial = AssetDatabase.LoadAssetAtPath<Material>(dependencies[i]);
            if (dependencyMaterial != null)
            {
                Texture dependencyTexture = dependencyMaterial.GetTexture("_BaseMap") ??
                                            dependencyMaterial.GetTexture("_MainTex");
                if (dependencyTexture is Texture2D dependencyTexture2D)
                    AddUnique(result, dependencyTexture2D);
            }
        }
        return result;
    }

    static void AddUnique(List<Texture2D> textures, Texture2D texture)
    {
        if (texture != null && !textures.Contains(texture))
            textures.Add(texture);
    }

    static string GetUcObjectName(string reference)
    {
        int dot = reference.LastIndexOf('.');
        return dot >= 0 ? reference.Substring(dot + 1) : Path.GetFileNameWithoutExtension(reference);
    }

    static float ResolveMotionMode(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        if (HasNonZeroRange(emitter.VelocityLossRange))
            return 2f;
        if (emitter.Acceleration.sqrMagnitude > 1e-8f ||
            HasNonZeroRange(emitter.StartVelocityRange))
            return 1f;
        return 0f;
    }

    static float ResolvePtvdMode(string mode)
    {
        if (string.Equals(mode, "PTVD_StartPositionAndOwner", StringComparison.OrdinalIgnoreCase))
            return 1f;
        if (string.Equals(mode, "PTVD_OwnerAndStartPosition", StringComparison.OrdinalIgnoreCase))
            return 2f;
        return 0f;
    }

    static float ResolveFlipbookMode(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        if (emitter.BlendBetweenSubdivisions)
            return 3f;
        if (emitter.UseRandomSubdivision)
            return 2f;
        return emitter.TextureUSubdivisions * emitter.TextureVSubdivisions > 1 &&
               emitter.SubdivisionEnd != emitter.SubdivisionStart
            ? 1f
            : 0f;
    }

    static bool HasNonZeroRange(L2EffectUcEmitterParser.UcVectorRange range)
    {
        return Math.Abs(range.X.Min) > 1e-6f || Math.Abs(range.X.Max) > 1e-6f ||
               Math.Abs(range.Y.Min) > 1e-6f || Math.Abs(range.Y.Max) > 1e-6f ||
               Math.Abs(range.Z.Min) > 1e-6f || Math.Abs(range.Z.Max) > 1e-6f;
    }

    static bool IsMeshEmitter(string className)
    {
        return string.Equals(className, "MeshEmitter", StringComparison.OrdinalIgnoreCase);
    }

    static float Midpoint(L2EffectUcEmitterParser.UcRange range)
    {
        return (range.Min + range.Max) * 0.5f;
    }

    static Vector4 RangeVector(float min, float max)
    {
        return new Vector4(min, max, 0f, 0f);
    }

    static void SetVectorRange(
        Material material,
        string propertyPrefix,
        L2EffectUcEmitterParser.UcVectorRange range)
    {
        SetVector(material, propertyPrefix + "XUc", RangeVector(range.X.Min, range.X.Max));
        SetVector(material, propertyPrefix + "YUc", RangeVector(range.Y.Min, range.Y.Max));
        SetVector(material, propertyPrefix + "ZUc", RangeVector(range.Z.Min, range.Z.Max));
    }

    static void SetColorMultiplier(
        Material material,
        L2EffectUcEmitterParser.UcVectorRange range)
    {
        Vector4 min = new Vector4(range.X.Min, range.Y.Min, range.Z.Min, 0f);
        Vector4 max = new Vector4(range.X.Max, range.Y.Max, range.Z.Max, 0f);
        SetVector(material, "_ColorMulMin", min);
        SetVector(material, "_ColorMulMax", max);
        SetVector(material, "_ColorMultiplier", min);
    }

    static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    static void SetVector(Material material, string property, Vector4 value)
    {
        if (material.HasProperty(property))
            material.SetVector(property, value);
    }

    static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    static void SetTexture(Material material, string property, Texture value)
    {
        if (material.HasProperty(property))
            material.SetTexture(property, value);
    }
}
#endif
