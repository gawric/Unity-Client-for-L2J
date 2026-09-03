#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class L2EffectGeneratorMaterialConfigurator
{
    public const string SpriteShaderName = "L2/Effects/SpriteEmitter";
    public const string MeshShaderName = "L2/Effects/MeshEmitter";
    public const string BeamShaderName = "L2/Effects/BeamEmitter";

    public static Shader ResolveShader(string className)
    {
        if (IsMeshEmitter(className))
            return Shader.Find(MeshShaderName);
        if (IsBeamEmitter(className))
            return Shader.Find(BeamShaderName);
        return Shader.Find(SpriteShaderName);
    }

    public static string Configure(
        Material material,
        UcEmitterDefinition emitter,
        Mesh slotMesh,
        Texture2D textureOverride = null,
        string effectClassName = null,
        string extendsClass = null,
        int meshSlotIndex = 0)
    {
        if (material == null || emitter == null)
            return "material configuration skipped";

        bool isMesh = IsMeshEmitter(emitter.ClassName);
        bool isBeam = IsBeamEmitter(emitter.ClassName);
        Shader shader = ResolveShader(emitter.ClassName);
        if (shader == null)
            return "unified shader is missing";
        if (material.shader == null || material.shader.name != shader.name)
        {
            material.shader = shader;
        }
        // Deferred URP: Geometry queue + UniversalForward never draws unlit meshes.
        material.renderQueue = (int)RenderQueue.Transparent;
        if (L2EffectGeneratorAssetOverrides.TryGetDrawOrder(
                effectClassName, emitter, out var drawOrder))
        {
            material.renderQueue = drawOrder.RenderQueue;
        }

        material.enableInstancing = emitter.MaxParticles > 1;

        ConfigureRenderState(material, emitter);
        ApplyMeshPackageExtras(material, emitter);
        ConfigureCommon(material, emitter, effectClassName, extendsClass);
        if (isMesh)
            ConfigureMesh(material, emitter);
        else if (isBeam)
            ConfigureBeam(material, emitter);
        else
            ConfigureSprite(material, emitter);

        List<Texture2D> resolvedTextures = ResolveTextures(emitter, slotMesh);
        Texture2D texture = textureOverride ??
                            (resolvedTextures.Count > 0 ? resolvedTextures[0] : null);
        ApplyMainTexture(material, texture);
        ApplyFxMt0005Overrides(material, emitter, ref texture);
        bool multiSection = slotMesh != null && slotMesh.subMeshCount > 1;
        bool meshHasSeparateSlots = false;
        if (isMesh && !string.IsNullOrWhiteSpace(emitter.StaticMeshReference))
        {
            meshHasSeparateSlots =
                L2EffectGeneratorViewerImport.EnsureMeshPackageBinding(
                    emitter.StaticMeshReference).SectionCount > 1;
        }

        Texture2D secondTexture = !multiSection &&
                                  !meshHasSeparateSlots &&
                                  resolvedTextures.Count > 1
            ? resolvedTextures[1]
            : null;
        ApplySecondTexture(material, secondTexture);
        ApplyMeshSlotShading(material, emitter, meshSlotIndex);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        if (texture == null)
            return "unified material configured, texture unresolved";
        return secondTexture != null
            ? "unified material configured, texture=" + texture.name +
              ", second=" + secondTexture.name
            : "unified material configured, texture=" + texture.name;
    }

    static void ConfigureCommon(
        Material material,
        UcEmitterDefinition emitter,
        string effectClassName,
        string extendsClass)
    {
        Vector3 offset = emitter.StartLocationOffset;
        if (L2EffectGeneratorAssetOverrides.TryGetStartLocationOffset(
                effectClassName, emitter, out Vector3 offsetOverride))
        {
            offset = offsetOverride;
        }

        Vector3 accel = emitter.Acceleration;
        UcVectorRange velocity = emitter.StartVelocityRange;
        if (L2EffectGeneratorAssetOverrides.ShouldAlignActorXWithProjectileFlight(
                effectClassName, extendsClass, emitter))
        {
            offset.x = -offset.x;
            accel.x = -accel.x;
            velocity.X = new UcRange(
                -velocity.X.Max, -velocity.X.Min);
            Debug.Log(
                "[L2EffectGenerator] " + effectClassName + "/" + emitter.EmitterName +
                " extends " + extendsClass +
                ": aligned actor X to projectile flight (LookRotation * +90).");
        }

        emitter.ResolveLifetimeRange(out float lifetimeMin, out float lifetimeMax);
        SetVector(material, "_LifetimeRange", RangeVector(lifetimeMin, lifetimeMax));
        if (emitter.HasInferredLifetimeFromFades())
        {
            Debug.Log(
                "[L2EffectGenerator] " + (effectClassName ?? "?") + "/" + emitter.EmitterName +
                ": no LifetimeRange in UC; inferred " +
                lifetimeMax.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "s from FadeInEnd=" +
                emitter.FadeInEndTime.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                " FadeOutStart=" +
                emitter.FadeOutStartTime.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ".");
        }
        // EffectPart._startDelay owns the UC emitter delay. Keep the shader
        // range at zero so the unified shader consumes its TLS draw without
        // applying the same delay a second time.
        SetVector(material, "_InitialDelayRange", RangeVector(0f, 0f));
        SetVector(material, "_StartLocationOffsetUc", new Vector4(
            offset.x, offset.y, offset.z, 0f));
        SetVectorRange(material, "_StartLocationRange", emitter.StartLocationRange);
        SetVectorRange(material, "_StartVelocityRange", velocity);
        SetVector(material, "_AccelerationUc", new Vector4(
            accel.x, accel.y, accel.z, 0f));
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
        UcEmitterDefinition emitter)
    {
        bool polar = string.Equals(
            emitter.StartLocationShape, "PTLS_Polar", StringComparison.OrdinalIgnoreCase);
        SetFloat(material, "_SpawnMode", 2f);
        SetFloat(material, "_FullTlsShape", polar ? 1f : 0f);
        SetFloat(material, "_PtvdMode", ResolvePtvdMode(emitter.GetVelocityDirectionFrom));
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

        SetVector(material, "_PolarThetaRangeUc", RangeVector(
            emitter.StartLocationPolarRange.X.Min, emitter.StartLocationPolarRange.X.Max));
        SetVector(material, "_PolarPhiRangeUc", RangeVector(
            emitter.StartLocationPolarRange.Y.Min, emitter.StartLocationPolarRange.Y.Max));
        SetVector(material, "_PolarRadiusRangeUc", RangeVector(
            emitter.StartLocationPolarRange.Z.Min, emitter.StartLocationPolarRange.Z.Max));

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
        UcEmitterDefinition emitter)
    {
        bool polar = string.Equals(
            emitter.StartLocationShape, "PTLS_Polar", StringComparison.OrdinalIgnoreCase);
        SetFloat(material, "_SpawnMode", 3f);
        SetFloat(material, "_FullTlsShape", polar ? 1f : 0f);
        SetFloat(material, "_MotionMode", ResolveMotionMode(emitter));
        SetFloat(material, "_OrientationMode", ResolveOrientationMode(emitter.UseDirectionAs));
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

        ResolveSubdivisionRange(emitter, out int subdivStart, out int subdivEnd);
        SetFloat(material, "_TextureUSubdivisions", Math.Max(1, emitter.TextureUSubdivisions));
        SetFloat(material, "_TextureVSubdivisions", Math.Max(1, emitter.TextureVSubdivisions));
        SetFloat(material, "_SubdivisionStart", subdivStart);
        SetFloat(material, "_SubdivisionEnd", subdivEnd);
        SetFloat(material, "_StaticSubdivision", subdivStart);
        if (L2EffectGeneratorAssetOverrides.TryGetFxMt0054Uv44Cell2(
                emitter, out _, out _, out _))
        {
            SetFloat(material, "_FlipbookMode", 0f);
            SetFloat(material, "_StaticSubdivision", 2f);
        }

        if (ResolveOrientationMode(emitter.UseDirectionAs) > 1.5f)
        {
            Vector3 ue = emitter.HasProjectionNormal
                ? emitter.ProjectionNormal
                : new Vector3(0f, 0f, 1f);
            Vector3 unity = new Vector3(ue.x, ue.z, ue.y);
            SetVector(material, "_SurfaceNormals", new Vector4(unity.x, unity.y, unity.z, 0f));
        }
    }

    static void ConfigureBeam(
        Material material,
        UcEmitterDefinition emitter)
    {
        bool polar = string.Equals(
            emitter.StartLocationShape, "PTLS_Polar", StringComparison.OrdinalIgnoreCase);
        SetFloat(material, "_L2FxWorldCalibration", 1.4f);
        SetFloat(material, "_UsePolar", polar ? 1f : 0f);
        SetFloat(material, "_BeamEndpointMode", ResolveBeamEndpointMode(emitter.DetermineEndPointBy));
        SetFloat(material, "_OpacityRatio", 1f);
        SetVector(material, "_SizeRange", RangeVector(
            emitter.StartSizeRange.X.Min, emitter.StartSizeRange.X.Max));
        SetVector(material, "_PolarThetaRangeUc", RangeVector(
            emitter.StartLocationPolarRange.X.Min, emitter.StartLocationPolarRange.X.Max));
        SetVector(material, "_PolarPhiRangeUc", RangeVector(
            emitter.StartLocationPolarRange.Y.Min, emitter.StartLocationPolarRange.Y.Max));
        SetVector(material, "_PolarRadiusRangeUc", RangeVector(
            emitter.StartLocationPolarRange.Z.Min, emitter.StartLocationPolarRange.Z.Max));
        SetVector(material, "_BeamEndOffsetXUc", RangeVector(
            emitter.BeamEndOffset.X.Min, emitter.BeamEndOffset.X.Max));
        SetVector(material, "_BeamEndOffsetYUc", RangeVector(
            emitter.BeamEndOffset.Y.Min, emitter.BeamEndOffset.Y.Max));
        SetVector(material, "_BeamEndOffsetZUc", RangeVector(
            emitter.BeamEndOffset.Z.Min, emitter.BeamEndOffset.Z.Max));
    }

    static void ConfigureRenderState(
        Material material,
        UcEmitterDefinition emitter)
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

    static void ApplyMeshPackageExtras(
        Material material,
        UcEmitterDefinition emitter)
    {
        if (material == null ||
            emitter == null ||
            !IsMeshEmitter(emitter.ClassName) ||
            string.IsNullOrWhiteSpace(emitter.StaticMeshReference))
        {
            return;
        }

        if (!L2EffectGeneratorViewerImport.TryGetMeshPackageExtras(
                emitter.StaticMeshReference,
                out bool twoSided,
                out _))
        {
            return;
        }

        if (twoSided || emitter.RenderTwoSided)
        {
            SetFloat(material, "_Cull", (float)CullMode.Off);
        }
    }

    static void ApplyMeshSlotShading(
        Material material,
        UcEmitterDefinition emitter,
        int meshSlotIndex)
    {
        if (material == null || emitter == null || !IsMeshEmitter(emitter.ClassName))
        {
            return;
        }

        SetFloat(material, "_IgnoreMainTexAlpha", 0f);
        SetColor(material, "_TextureFactor", Color.white);
        SetFloat(material, "_TextureContrast", 1f);
        SetFloat(material, "_TextureFloor", 0f);
        if (!L2EffectGeneratorAssetOverrides.TryGetMeshSlotShading(
                emitter.StaticMeshReference,
                meshSlotIndex,
                out var shading))
        {
            return;
        }

        if (shading.IgnoreMainTexAlpha)
        {
            SetFloat(material, "_IgnoreMainTexAlpha", 1f);
        }

        if (shading.CullOff)
        {
            SetFloat(material, "_Cull", (float)CullMode.Off);
        }

        if (shading.HasTexturePaint)
        {
            SetColor(material, "_TextureFactor", shading.TextureFactor);
            SetFloat(material, "_TextureContrast", shading.TextureContrast);
            SetFloat(material, "_TextureFloor", shading.TextureFloor);
        }
    }

    static void ConfigureColorKeys(
        Material material,
        UcEmitterDefinition emitter)
    {
        var keys = new List<UcColorScaleKey>(emitter.ColorScaleKeys);
        keys.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (keys.Count == 0)
        {
            keys.Add(new UcColorScaleKey
            {
                Index = 0,
                RelativeTime = 0f,
                Color = Color.white
            });
        }

        int capacity = IsMeshEmitter(emitter.ClassName)
            ? 6
            : (IsBeamEmitter(emitter.ClassName) ? 3 : 4);
        int count = Math.Min(keys.Count, capacity);
        SetFloat(material, "_ColorScaleCount", count);
        for (int i = 0; i < capacity; i++)
        {
            UcColorScaleKey key = keys[Math.Min(i, count - 1)];
            SetColor(material, "_ColorKey" + i, key.Color);
            if (i > 0)
                SetFloat(material, "_ColorKey" + i + "Time", key.RelativeTime);
        }
    }

    static void ConfigureSizeKeys(
        Material material,
        UcEmitterDefinition emitter)
    {
        var keys = new List<UcSizeScaleKey>(emitter.SizeScaleKeys);
        keys.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (keys.Count == 0)
        {
            keys.Add(new UcSizeScaleKey
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
            UcSizeScaleKey key = keys[Math.Min(i, count - 1)];
            SetVector(material, "_SizeKey" + i,
                new Vector4(key.RelativeTime, key.RelativeSize, 0f, 0f));
        }
    }

    public static List<Texture2D> ResolveTextures(UcEmitterDefinition emitter, Mesh slotMesh)
    {
        return L2EffectTextureResolver.Resolve(emitter, slotMesh);
    }

    public static List<Texture2D> ResolveTextures(string textureReference, Mesh slotMesh)
    {
        return L2EffectTextureResolver.Resolve(textureReference, slotMesh);
    }

    public static List<Texture2D> ResolveTextures(
        string textureReference,
        Mesh slotMesh,
        UcEmitterDefinition emitter)
    {
        return L2EffectTextureResolver.Resolve(textureReference, slotMesh, emitter);
    }

    static void ApplyMainTexture(Material material, Texture2D texture)
    {
        if (material == null || texture == null)
            return;

        material.SetTexture("_MainTex", texture);
        material.SetTexture("_MainTexture", texture);
        material.SetTexture("_BaseMap", texture);
        material.mainTexture = texture;
    }

    static void ApplyFxMt0005Overrides(
        Material material,
        UcEmitterDefinition emitter,
        ref Texture2D currentTexture)
    {
        ResolveSubdivisionRange(emitter, out int start, out int end);
        bool colorGammaToLinear = false;
        float worldCalibration = 0f;
        if (!L2EffectGeneratorAssetOverrides.TryGetFxMt0005Uv2Cell23(
                emitter, out string texName, out float rgbBoost) &&
            !L2EffectGeneratorAssetOverrides.TryGetFxMt0005StarCell(
                emitter, currentTexture, start, end, out texName, out rgbBoost) &&
            !L2EffectGeneratorAssetOverrides.TryGetFxMt0054Uv44Cell2(
                emitter, out texName, out rgbBoost, out worldCalibration) &&
            !L2EffectGeneratorAssetOverrides.TryGetFxMt0006Uv42LinearTexture(
                emitter, out texName, out rgbBoost, out colorGammaToLinear))
        {
            return;
        }

        Texture2D overrideTex = L2EffectTextureResolver.FindByUcName(texName);
        if (overrideTex != null)
        {
            ApplyMainTexture(material, overrideTex);
            currentTexture = overrideTex;
        }

        SetFloat(material, "_RgbBoost", rgbBoost);
        if (worldCalibration > 0f)
        {
            SetFloat(material, "_L2FxWorldCalibration", worldCalibration);
        }

        if (colorGammaToLinear)
        {
            SetFloat(material, "_L2SpriteColorGammaToLinear", 1f);
        }
    }

    static void ApplySecondTexture(Material material, Texture2D texture)
    {
        if (material == null || !material.HasProperty("_SecondTex"))
            return;

        if (texture == null)
        {
            if (material.HasProperty("_UseSecondTex"))
                material.SetFloat("_UseSecondTex", 0f);
            return;
        }

        material.SetTexture("_SecondTex", texture);
        if (material.HasProperty("_UseSecondTex"))
            material.SetFloat("_UseSecondTex", 1f);
    }

    static float ResolveOrientationMode(string useDirectionAs)
    {
        if (string.Equals(useDirectionAs, "PTDU_Normal", StringComparison.OrdinalIgnoreCase))
            return 2f;
        if (string.Equals(useDirectionAs, "PTDU_Up", StringComparison.OrdinalIgnoreCase))
            return 1f;
        return 0f;
    }

    static float ResolveMotionMode(UcEmitterDefinition emitter)
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

    // UE2 / L2 editor stores a picked atlas cell as a 2-index pair:
    //   SubdivisionStart=4 SubdivisionEnd=3  (reversed)
    //   SubdivisionStart=3 SubdivisionEnd=4  (adjacent)
    // without BlendBetween / UseRandom. L2 samples only the higher cell; the
    // lower index is the unused 0-based sibling. min/max would invent a 2-frame
    // flipbook. Real animations (steam 4..15, BlendBetween) are left intact.
    static void ResolveSubdivisionRange(
        UcEmitterDefinition emitter,
        out int start,
        out int end)
    {
        start = emitter.SubdivisionStart;
        end = emitter.SubdivisionEnd;
        if (L2EffectGeneratorAssetOverrides.TryCorrectFxMt0005Subdivision68(
                emitter, ref start, ref end) ||
            L2EffectGeneratorAssetOverrides.TryCorrectFxMt0005Uv2Cell2(
                emitter, ref start, ref end) ||
            L2EffectGeneratorAssetOverrides.TryCorrectFxMt0054Uv44Cell2(
                emitter, ref start, ref end) ||
            L2EffectGeneratorAssetOverrides.TryCorrectFxMt0006Uv42Cell4(
                emitter, ref start, ref end) ||
            L2EffectGeneratorAssetOverrides.TryCorrectFxMt0000Uv44Cell15(
                emitter, ref start, ref end))
        {
            return;
        }

        if (emitter.BlendBetweenSubdivisions || emitter.UseRandomSubdivision)
            return;

        if (end < start)
            end = start;
        else if (end == start + 1)
            start = end;
    }

    static float ResolveFlipbookMode(UcEmitterDefinition emitter)
    {
        if (emitter.BlendBetweenSubdivisions)
            return 3f;
        if (emitter.UseRandomSubdivision)
            return 2f;
        ResolveSubdivisionRange(emitter, out int start, out int end);
        return emitter.TextureUSubdivisions * emitter.TextureVSubdivisions > 1 &&
               end != start
            ? 1f
            : 0f;
    }

    static bool HasNonZeroRange(UcVectorRange range)
    {
        return Math.Abs(range.X.Min) > 1e-6f || Math.Abs(range.X.Max) > 1e-6f ||
               Math.Abs(range.Y.Min) > 1e-6f || Math.Abs(range.Y.Max) > 1e-6f ||
               Math.Abs(range.Z.Min) > 1e-6f || Math.Abs(range.Z.Max) > 1e-6f;
    }

    static bool IsMeshEmitter(string className)
    {
        return string.Equals(className, "MeshEmitter", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsBeamEmitter(string className)
    {
        return string.Equals(className, "BeamEmitter", StringComparison.OrdinalIgnoreCase);
    }

    static float ResolveBeamEndpointMode(string determineEndPointBy)
    {
        if (string.Equals(determineEndPointBy, "PTEP_Distance", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(determineEndPointBy, "PTEP_Distance_Absolute", StringComparison.OrdinalIgnoreCase))
            return 1f;
        if (string.Equals(determineEndPointBy, "PTEP_OffsetAsAbsolute", StringComparison.OrdinalIgnoreCase))
            return 2f;
        return 0f;
    }

    static float Midpoint(UcRange range)
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
        UcVectorRange range)
    {
        SetVector(material, propertyPrefix + "XUc", RangeVector(range.X.Min, range.X.Max));
        SetVector(material, propertyPrefix + "YUc", RangeVector(range.Y.Min, range.Y.Max));
        SetVector(material, propertyPrefix + "ZUc", RangeVector(range.Z.Min, range.Z.Max));
    }

    static void SetColorMultiplier(
        Material material,
        UcVectorRange range)
    {
        Vector4 min = new Vector4(range.X.Min, range.Y.Min, range.Z.Min, 0f);
        Vector4 max = new Vector4(range.X.Max, range.Y.Max, range.Z.Max, 0f);
        SetVector(material, "_ColorMulMin", min);
        SetVector(material, "_ColorMulMax", max);
        SetVector(material, "_ColorMultiplier", min);
    }

    static void SetFloat(Material material, string property, float value)
    {
        material.SetFloat(property, value);
    }

    static void SetVector(Material material, string property, Vector4 value)
    {
        material.SetVector(property, value);
    }

    static void SetColor(Material material, string property, Color value)
    {
        material.SetColor(property, value);
    }

    static void SetTexture(Material material, string property, Texture value)
    {
        material.SetTexture(property, value);
    }
}
#endif
