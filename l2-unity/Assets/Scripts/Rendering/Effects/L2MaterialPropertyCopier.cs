using UnityEngine;

/// <summary>
/// GPU bridge: copies shared material params into runtime instances and writes dynamic shader uniforms.
/// </summary>
public static class L2MaterialPropertyCopier
{
    public const string OwnerWorldPosProperty = "_OwnerWorldPos";
    public const string UseExternalTargetPositionProperty = "_UseExternalTargetPosition";
    public const string UseOwnerFromShaderTargetProperty = "_UseOwnerFromShaderTarget";
    public const string L2FxTargetWorldPosProperty = "_L2FxTargetWorldPos";

    public static readonly int HoldId = Shader.PropertyToID("_Hold");
    public static readonly int HoldSizeReferenceId = Shader.PropertyToID("_HoldSizeReference");
    public static readonly int LifetimeRangeId = Shader.PropertyToID("_LifetimeRange");
    public static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    public static readonly int SeedId = Shader.PropertyToID("_Seed");
    public static readonly int HasLifetimeId = Shader.PropertyToID("_HasLifetime");
    public static readonly int InitialDelayRangeId = Shader.PropertyToID("_InitialDelayRange");
    public static readonly int FadeInId = Shader.PropertyToID("_FadeIn");
    public static readonly int FadeInEndTimeId = Shader.PropertyToID("_FadeInEndTime");
    public static readonly int FadeoutId = Shader.PropertyToID("_Fadeout");
    public static readonly int FadeoutStartTimeId = Shader.PropertyToID("_FadeoutStartTime");
    public static readonly int FadeOutPowerId = Shader.PropertyToID("_FadeOutPower");
    public static readonly int DebugAtlasPreviewId = Shader.PropertyToID("_DebugAtlasPreview");
    public static readonly int RgbBoostId = Shader.PropertyToID("_RgbBoost");
    public static readonly int AlphaBoostId = Shader.PropertyToID("_AlphaBoost");
    public static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    public static readonly int AtlasUvRemapId = Shader.PropertyToID("_AtlasUvRemap");
    public static readonly int AtlasUvMinMaxId = Shader.PropertyToID("_AtlasUvMinMax");
    public static readonly int IgnoreMainTexAlphaId = Shader.PropertyToID("_IgnoreMainTexAlpha");
    public static readonly int EmitterAlphaId = Shader.PropertyToID("_EmitterAlpha");
    public static readonly int AlphaEdgeFeatherId = Shader.PropertyToID("_AlphaEdgeFeather");
    public static readonly int SplitRibbonByLumId = Shader.PropertyToID("_SplitRibbonByLum");
    public static readonly int SoftLumMinId = Shader.PropertyToID("_SoftLumMin");
    public static readonly int SoftLumMaxId = Shader.PropertyToID("_SoftLumMax");
    public static readonly int LineLumMinId = Shader.PropertyToID("_LineLumMin");
    public static readonly int LineLumMaxId = Shader.PropertyToID("_LineLumMax");
    public static readonly int SoftOpacityMulId = Shader.PropertyToID("_SoftOpacityMul");
    public static readonly int LineOpacityMulId = Shader.PropertyToID("_LineOpacityMul");
    public static readonly int SoftRgbBoostId = Shader.PropertyToID("_SoftRgbBoost");
    public static readonly int LineRgbBoostId = Shader.PropertyToID("_LineRgbBoost");
    public static readonly int SplitByUvLayerId = Shader.PropertyToID("_SplitByUvLayer");
    public static readonly int UvLayerCenterId = Shader.PropertyToID("_UvLayerCenter");
    public static readonly int UvLayerDistMinId = Shader.PropertyToID("_UvLayerDistMin");
    public static readonly int UvLayerDistMaxId = Shader.PropertyToID("_UvLayerDistMax");
    public static readonly int OuterSoftOpacityMulId = Shader.PropertyToID("_OuterSoftOpacityMul");
    public static readonly int OuterLineOpacityMulId = Shader.PropertyToID("_OuterLineOpacityMul");
    public static readonly int OuterSoftRgbBoostId = Shader.PropertyToID("_OuterSoftRgbBoost");
    public static readonly int OuterLineRgbBoostId = Shader.PropertyToID("_OuterLineRgbBoost");
    public static readonly int ColorScaleRepeatsId = Shader.PropertyToID("_ColorScaleRepeats");
    public static readonly int ColorScaleCountId = Shader.PropertyToID("_ColorScaleCount");
    public static readonly int ColorScaleTime1Id = Shader.PropertyToID("_ColorScaleTime1");
    public static readonly int ColorScaleTime2Id = Shader.PropertyToID("_ColorScaleTime2");
    public static readonly int ColorScale0Id = Shader.PropertyToID("_ColorScale0");
    public static readonly int ColorScale1Id = Shader.PropertyToID("_ColorScale1");
    public static readonly int ColorScale2Id = Shader.PropertyToID("_ColorScale2");
    public static readonly int BAlphaBlendId = Shader.PropertyToID("_bAlphaBlend");
    public static readonly int BillboardToCameraId = Shader.PropertyToID("_BillboardToCamera");
    public static readonly int BillboardWorldUpId = Shader.PropertyToID("_BillboardWorldUp");
    public static readonly int BillboardEulerOffsetId = Shader.PropertyToID("_BillboardEulerOffset");
    public static readonly int UseExternalTargetPositionId = Shader.PropertyToID(UseExternalTargetPositionProperty);
    public static readonly int UseOwnerFromShaderTargetId = Shader.PropertyToID(UseOwnerFromShaderTargetProperty);
    public static readonly int L2FxTargetWorldPosId = Shader.PropertyToID(L2FxTargetWorldPosProperty);

    public static void CopyLifetimeFadeAndFxFromShared(Material runtimeMat, Material sharedMat)
    {
        if (runtimeMat == null || sharedMat == null)
        {
            return;
        }

        CopyFloatIfPresent(runtimeMat, sharedMat, HasLifetimeId);
        CopyVectorIfPresent(runtimeMat, sharedMat, LifetimeRangeId);
        CopyVectorIfPresent(runtimeMat, sharedMat, InitialDelayRangeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeInId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeInEndTimeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeoutId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeoutStartTimeId);
        CopyFloatIfPresent(runtimeMat, sharedMat, FadeOutPowerId);
        if (Application.isPlaying && runtimeMat.HasProperty(DebugAtlasPreviewId))
        {
            runtimeMat.SetFloat(DebugAtlasPreviewId, 0f);
        }
        else
        {
            CopyFloatIfPresent(runtimeMat, sharedMat, DebugAtlasPreviewId);
        }
        CopyFloatIfPresent(runtimeMat, sharedMat, RgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, AlphaBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OpacityId);
        CopyFloatIfPresent(runtimeMat, sharedMat, AtlasUvRemapId);
        CopyVectorIfPresent(runtimeMat, sharedMat, AtlasUvMinMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, IgnoreMainTexAlphaId);
        CopyFloatIfPresent(runtimeMat, sharedMat, EmitterAlphaId);
        CopyFloatIfPresent(runtimeMat, sharedMat, AlphaEdgeFeatherId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SplitRibbonByLumId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftLumMinId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftLumMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineLumMinId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineLumMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SoftRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, LineRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, SplitByUvLayerId);
        CopyVectorIfPresent(runtimeMat, sharedMat, UvLayerCenterId);
        CopyFloatIfPresent(runtimeMat, sharedMat, UvLayerDistMinId);
        CopyFloatIfPresent(runtimeMat, sharedMat, UvLayerDistMaxId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterSoftOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterLineOpacityMulId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterSoftRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, OuterLineRgbBoostId);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleRepeatsId);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleCountId);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleTime1Id);
        CopyFloatIfPresent(runtimeMat, sharedMat, ColorScaleTime2Id);
        CopyColorIfPresent(runtimeMat, sharedMat, ColorScale0Id);
        CopyColorIfPresent(runtimeMat, sharedMat, ColorScale1Id);
        CopyColorIfPresent(runtimeMat, sharedMat, ColorScale2Id);
        CopyFloatIfPresent(runtimeMat, sharedMat, BAlphaBlendId);
        CopyFloatIfPresent(runtimeMat, sharedMat, BillboardToCameraId);
        CopyVectorIfPresent(runtimeMat, sharedMat, BillboardWorldUpId);
        CopyVectorIfPresent(runtimeMat, sharedMat, BillboardEulerOffsetId);
    }

    public static float ReadLifetimeMax(Material[] sharedMaterials, float fallback)
    {
        if (sharedMaterials == null)
        {
            return fallback;
        }

        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            Material mat = sharedMaterials[i];
            if (mat != null && mat.HasProperty(LifetimeRangeId))
            {
                return mat.GetVector(LifetimeRangeId).y;
            }
        }

        return fallback;
    }

    public static void SetFloatOnMaterials(Material[] materials, int propertyId, float value)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && mat.HasProperty(propertyId))
            {
                mat.SetFloat(propertyId, value);
            }
        }
    }

    public static void SetVectorOnMaterials(Material[] materials, int propertyId, Vector4 value)
    {
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && mat.HasProperty(propertyId))
            {
                mat.SetVector(propertyId, value);
            }
        }
    }

    public static float ReadFloatFromFirstMaterial(Material[] materials, int propertyId, float fallback)
    {
        if (materials == null)
        {
            return fallback;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat != null && mat.HasProperty(propertyId))
            {
                return mat.GetFloat(propertyId);
            }
        }

        return fallback;
    }

    private static void CopyFloatIfPresent(Material runtimeMat, Material sharedMat, int propertyId)
    {
        if (runtimeMat.HasProperty(propertyId) && sharedMat.HasProperty(propertyId))
        {
            runtimeMat.SetFloat(propertyId, sharedMat.GetFloat(propertyId));
        }
    }

    private static void CopyVectorIfPresent(Material runtimeMat, Material sharedMat, int propertyId)
    {
        if (runtimeMat.HasProperty(propertyId) && sharedMat.HasProperty(propertyId))
        {
            runtimeMat.SetVector(propertyId, sharedMat.GetVector(propertyId));
        }
    }

    private static void CopyColorIfPresent(Material runtimeMat, Material sharedMat, int propertyId)
    {
        if (runtimeMat.HasProperty(propertyId) && sharedMat.HasProperty(propertyId))
        {
            runtimeMat.SetColor(propertyId, sharedMat.GetColor(propertyId));
        }
    }
}
