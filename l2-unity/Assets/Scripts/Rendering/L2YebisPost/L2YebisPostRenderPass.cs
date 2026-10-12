using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Global High Elf Yebis after sky/world/skills. Contrast is primary, bloom secondary.
/// Not gated on skills: WindStrike has no PostEffectDataID.
/// </summary>
public sealed class L2YebisPostRenderPass : ScriptableRenderPass
{
    const int PassExtract = 0;
    const int PassKawase = 1;
    const int PassLuma = 2;
    const int PassAdapt = 3;
    const int PassCombine = 4;
    const int PassCopy = 5;
    const int PassAddBloom = 6;
    const int PassSceneToFloat = 7;

    static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
    static readonly int AdaptedTexId = Shader.PropertyToID("_AdaptedTex");
    static readonly int BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    static readonly int BloomRemapId = Shader.PropertyToID("_BloomRemap");
    static readonly int BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    static readonly int KawaseOffsetId = Shader.PropertyToID("_KawaseOffset");
    static readonly int KawaseTexelSizeId = Shader.PropertyToID("_KawaseTexelSize");
    static readonly int AdaptLerpId = Shader.PropertyToID("_AdaptLerp");
    static readonly int YebisDebugId = Shader.PropertyToID("_YebisDebug");
    static readonly int ExposureId = Shader.PropertyToID("_Exposure");
    static readonly int UseExposureId = Shader.PropertyToID("_UseExposure");
    static readonly int Mapping0Id = Shader.PropertyToID("_Mapping0");
    static readonly int MiddleGrayId = Shader.PropertyToID("_MiddleGray");
    static readonly int AdaptationScaleId = Shader.PropertyToID("_AdaptationScale");
    static readonly int PrePostLumaScaleId = Shader.PropertyToID("_PrePostLumaScale");
    static readonly int AdaptedOverrideId = Shader.PropertyToID("_AdaptedOverride");
    static readonly int ColorGammaId = Shader.PropertyToID("_ColorGamma");
    static readonly int ColorSaturationId = Shader.PropertyToID("_ColorSaturation");
    static readonly int FilmicAId = Shader.PropertyToID("_FilmicA");
    static readonly int FilmicBId = Shader.PropertyToID("_FilmicB");
    static readonly int MatrixBiasId = Shader.PropertyToID("_MatrixBias");
    static readonly int GainMinId = Shader.PropertyToID("_GainMin");
    static readonly int GainMaxId = Shader.PropertyToID("_GainMax");
    static readonly int ContrastMixId = Shader.PropertyToID("_ContrastMix");
    static readonly int Pre3898Id = Shader.PropertyToID("_Pre3898");
    static readonly int CombineGainId = Shader.PropertyToID("_CombineGain");
    static readonly int FirstMatrixId = Shader.PropertyToID("_FirstMatrix");
    static readonly int SecondMatrixId = Shader.PropertyToID("_SecondMatrix");
    static readonly int DisplayGammaId = Shader.PropertyToID("_DisplayGamma");
    static readonly int CaptureFormulaId = Shader.PropertyToID("_CaptureFormula");
    static readonly int GradeStageId = Shader.PropertyToID("_GradeStage");
    static readonly int LumaGridId = Shader.PropertyToID("_LumaGrid");
    static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
    static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
    static readonly int BloomExcludeId = Shader.PropertyToID("_BloomExclude");
    static readonly int BloomExcludeMaskId = Shader.PropertyToID("_BloomExcludeMask");
    static readonly ProfilingSampler Sampler = new ProfilingSampler("L2 Yebis Post");

    readonly L2FxCompositorSettings _settings;
    readonly Material _material;

    RTHandle _temp;
    RTHandle _bloomA;
    RTHandle _bloomB;
    RTHandle _luma0;
    RTHandle _luma1;
    RTHandle _luma2;
    RTHandle _adapted;
    RTHandle _adaptedPrev;
    RTHandle _sceneFloat;
    RTHandle _bloomExclude;
    RTHandle _captureScene;
    RTHandle _captureGrade;
    RTHandle _captureControl;
    Material _excludeSkyMaterial;
    bool _adaptedSeeded;
    int _execFrame;

    public L2YebisPostRenderPass(L2FxCompositorSettings settings, Material material)
    {
        _settings = settings;
        _material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Dispose()
    {
        _temp?.Release();
        _temp = null;
        _bloomA?.Release();
        _bloomA = null;
        _bloomB?.Release();
        _bloomB = null;
        _luma0?.Release();
        _luma0 = null;
        _luma1?.Release();
        _luma1 = null;
        _luma2?.Release();
        _luma2 = null;
        _adapted?.Release();
        _adapted = null;
        _adaptedPrev?.Release();
        _adaptedPrev = null;
        _sceneFloat?.Release();
        _sceneFloat = null;
        _bloomExclude?.Release();
        _bloomExclude = null;
        _captureScene?.Release();
        _captureScene = null;
        _captureGrade?.Release();
        _captureGrade = null;
        _captureControl?.Release();
        _captureControl = null;
        CoreUtils.Destroy(_excludeSkyMaterial);
        _excludeSkyMaterial = null;
        _adaptedSeeded = false;
        _execFrame = 0;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor desc = ColorDesc(renderingData.cameraData.cameraTargetDescriptor, 0, 0);

        RenderingUtils.ReAllocateIfNeeded(
            ref _temp, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisTemp");

        RenderTextureDescriptor curveDesc = ColorDesc(renderingData.cameraData.cameraTargetDescriptor, 0, 0);
        curveDesc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
        curveDesc.sRGB = false;
        RenderingUtils.ReAllocateIfNeeded(
            ref _sceneFloat, curveDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_L2YebisSceneFloat");

        RenderTextureDescriptor halfDesc = ColorDesc(desc, Mathf.Max(1, desc.width / 2), Mathf.Max(1, desc.height / 2));
        RenderingUtils.ReAllocateIfNeeded(
            ref _bloomA, halfDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisBloomA");
        RenderingUtils.ReAllocateIfNeeded(
            ref _bloomB, halfDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisBloomB");

        RenderTextureDescriptor luma0 = ColorDesc(desc, Mathf.Max(1, desc.width / 4), Mathf.Max(1, desc.height / 4));
        RenderingUtils.ReAllocateIfNeeded(
            ref _luma0, luma0, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisLuma0");

        RenderTextureDescriptor luma1 = ColorDesc(desc, Mathf.Max(1, desc.width / 16), Mathf.Max(1, desc.height / 16));
        RenderingUtils.ReAllocateIfNeeded(
            ref _luma1, luma1, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisLuma1");

        // 8x8, not 1x1: BlitCameraTexture + 1x1 RTV writes black (RenderDoc luma2/adapted = 0).
        RenderTextureDescriptor adapt = ColorDesc(desc, 8, 8);
        RenderingUtils.ReAllocateIfNeeded(
            ref _luma2, adapt, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisLuma2");
        RenderingUtils.ReAllocateIfNeeded(
            ref _adapted, adapt, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisAdapted");
        RenderingUtils.ReAllocateIfNeeded(
            ref _adaptedPrev, adapt, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L2YebisAdaptedPrev");

        RenderingUtils.ReAllocateIfNeeded(
            ref _bloomExclude, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_L2YebisBloomExclude");

        if (renderingData.cameraData.cameraType == CameraType.Game
            && renderingData.cameraData.renderType == CameraRenderType.Base)
        {
            RenderingUtils.ReAllocateIfNeeded(
                ref _captureScene, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_L2YebisCaptureScene");
            RenderingUtils.ReAllocateIfNeeded(
                ref _captureGrade, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_L2YebisCaptureGrade");
            RenderingUtils.ReAllocateIfNeeded(
                ref _captureControl, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_L2YebisCaptureControl");
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_material == null || _temp == null)
        {
            L2NameplateOverlayQueue.FlushImmediateFallback();
            L2YebisPostRuntime.WillFlushNameplates = false;
            return;
        }

        RTHandle cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
        if (cameraColor == null)
        {
            L2NameplateOverlayQueue.FlushImmediateFallback();
            L2YebisPostRuntime.WillFlushNameplates = false;
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("L2 Yebis Post");
        using (new ProfilingScope(cmd, Sampler))
        {
            BindParams();

            _material.SetFloat(LumaGridId, 0f);
            cmd.SetGlobalFloat(LumaGridId, 0f);
            SetBlitTexelSize(cameraColor);
            BlitFull(cmd, cameraColor, _luma0, PassLuma);
            SetBlitTexelSize(_luma0);
            BlitFull(cmd, _luma0, _luma1, PassLuma);
            _material.SetFloat(LumaGridId, 8f);
            cmd.SetGlobalFloat(LumaGridId, 8f);
            SetBlitTexelSize(_luma1);
            BlitFull(cmd, _luma1, _luma2, PassLuma);
            _material.SetFloat(LumaGridId, 0f);
            cmd.SetGlobalFloat(LumaGridId, 0f);

            if (!_adaptedSeeded)
            {
                // Seed from this frame's 8x8 luma, not middleGray 0.18.
                // 0.18 + mapping 18 → gain 9 and a white flash (the old 2.1 workaround).
                BlitFull(cmd, _luma2, _adapted, PassCopy);
                BlitFull(cmd, _luma2, _adaptedPrev, PassCopy);
                _adaptedSeeded = true;
            }

            cmd.SetGlobalTexture(AdaptedTexId, _adaptedPrev.nameID);
            BlitFull(cmd, _luma2, _adapted, PassAdapt);
            BlitFull(cmd, _adapted, _adaptedPrev, PassCopy);

            cmd.SetGlobalTexture(AdaptedTexId, _adapted.nameID);
            cmd.SetGlobalTexture(BloomTexId, Texture2D.blackTexture);

            int debug = _settings.yebisDebugMode;
            // Off while the confirmed combine is being checked. Kawase was added after the curve.
            // Restore: wantBloom = intensity > 0.0001 && debug != 1. Saved intensity is yebisBloomIntensity (0.18).
            bool wantBloom = false;

            bool gameBase = renderingData.cameraData.cameraType == CameraType.Game
                && renderingData.cameraData.renderType == CameraRenderType.Base;
            string captureFolder = null;
            if (gameBase && L2YebisLiveCapture.Consume())
            {
                if (_captureScene == null || _captureScene.rt == null || _captureGrade == null || _captureGrade.rt == null || _captureControl == null || _captureControl.rt == null)
                {
                    Debug.LogWarning("[Yebis] свои RT для снимка ещё не созданы");
                }
                else
                {
                    var cameraFormat = renderingData.cameraData.cameraTargetDescriptor.graphicsFormat;
                    float hours = WorldClock.Instance != null ? WorldClock.Instance.WorldHours : -1f;
                    captureFolder = L2YebisLiveCapture.CreateFolder(
                        hours,
                        cameraFormat.ToString(),
                        QualitySettings.activeColorSpace.ToString());
                    cmd.SetRenderTarget(_captureControl.nameID);
                    cmd.ClearRenderTarget(false, true, new Color(0.8f, 0.2f, 0.05f, 1f));
                    cmd.SetRenderTarget(_captureScene.nameID);
                    cmd.ClearRenderTarget(false, true, new Color(1f, 0f, 1f, 1f));
                    cmd.SetRenderTarget(_captureGrade.nameID);
                    cmd.ClearRenderTarget(false, true, new Color(1f, 0f, 1f, 1f));
                    string sceneCopy = CopyCapture(cmd, cameraColor, _captureScene);
                    Debug.Log(
                        "[Yebis] перед combine " + _captureScene.rt.width + "x" + _captureScene.rt.height +
                        " cameraFormat=" + cameraFormat +
                        " copyFormat=" + _captureScene.rt.graphicsFormat +
                        " created=" + _captureScene.rt.IsCreated() +
                        " how=" + sceneCopy +
                        " src=" + RtSize(cameraColor));
                }
            }

            // Curve 0A08B2C83DB6048D writes the half-float buffer combine samples.
            // The confirmed combine formula is unchanged.
            RTHandle combineSource = cameraColor;
            if (_sceneFloat != null)
            {
                BlitFull(cmd, cameraColor, _sceneFloat, PassSceneToFloat);
                combineSource = _sceneFloat;
            }

            // Debug 3+ reads the raw scene (luma/gain). Bloom extract is from the
            // graded look so mid-tone skills (blue particles) still pass.
            if (debug >= 3)
            {
                BlitFull(cmd, combineSource, _temp, PassCombine);
                if (captureFolder != null)
                    CopyCapture(cmd, _temp, _captureGrade);
                BlitFull(cmd, _temp, cameraColor, PassCopy);
            }
            else
            {
                BlitFull(cmd, combineSource, _temp, PassCombine);
                if (captureFolder != null)
                    CopyCapture(cmd, _temp, _captureGrade);

                if (wantBloom)
                {
                    if (_settings.yebisBloomExcludeSkyCloudsSun)
                        FillBloomExcludeMask(cmd, ref renderingData);
                    else
                        BindBloomExclude(cmd, false);

                    SetBlitTexelSize(_temp);
                    BlitFull(cmd, _temp, _bloomA, PassExtract);
                    _material.SetFloat(KawaseOffsetId, 1f);
                    SetBlitTexelSize(_bloomA);
                    BlitFull(cmd, _bloomA, _bloomB, PassKawase);
                    _material.SetFloat(KawaseOffsetId, 2f);
                    SetBlitTexelSize(_bloomB);
                    BlitFull(cmd, _bloomB, _bloomA, PassKawase);
                    _material.SetFloat(KawaseOffsetId, 3f);
                    SetBlitTexelSize(_bloomA);
                    BlitFull(cmd, _bloomA, _bloomB, PassKawase);
                    cmd.SetGlobalTexture(BloomTexId, _bloomB.nameID);
                    if (debug == 2)
                        BlitFull(cmd, _bloomB, cameraColor, PassCopy);
                    else
                        BlitFull(cmd, _temp, cameraColor, PassAddBloom);
                }
                else
                {
                    BlitFull(cmd, _temp, cameraColor, PassCopy);
                }
            }

            CoreUtils.SetRenderTarget(cmd, cameraColor, ClearFlag.None, Color.clear);
            if (captureFolder != null && _captureScene != null && _captureScene.rt != null && _captureGrade != null && _captureGrade.rt != null && _captureControl != null && _captureControl.rt != null)
            {
                Debug.Log("[Yebis] ReadAfterFrame после записи своих RT, не временных целей URP");
                L2YebisLiveCapture.ReadAfterFrame(captureFolder, _captureScene.rt, _captureGrade.rt, _captureControl.rt);
            }
            L2NameplateOverlayQueue.Flush(cmd);
            MaybeLogYebis(cmd, cameraColor);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        L2YebisPostRuntime.WillFlushNameplates = false;
    }

    void BindParams()
    {
        L2FxCompositorSettings s = _settings;
        _material.SetFloat(BloomThresholdId, s.yebisBloomThreshold);
        _material.SetFloat(BloomRemapId, s.yebisBloomRemap);
        _material.SetFloat(BloomIntensityId, s.yebisBloomIntensity);
        _material.SetFloat(AdaptLerpId, Mathf.Clamp01(s.yebisAdaptationSensitivity * 0.16f));
        _material.SetFloat(YebisDebugId, s.yebisDebugMode);
        _material.SetFloat(ExposureId, s.yebisExposure);
        _material.SetFloat(UseExposureId, s.yebisUseExposure);
        _material.SetFloat(Mapping0Id, s.yebisMappingFactor0);
        _material.SetFloat(MiddleGrayId, s.yebisMiddleGray);
        _material.SetFloat(AdaptationScaleId, s.yebisAdaptationScale);
        _material.SetFloat(PrePostLumaScaleId, s.yebisPrePostLumaScale);
        _material.SetFloat(AdaptedOverrideId, s.yebisAdaptedOverride);
        _material.SetFloat(ColorGammaId, s.yebisColorGamma);
        _material.SetFloat(ColorSaturationId, s.yebisColorSaturation);
        _material.SetFloat(FilmicAId, s.yebisFilmicA);
        _material.SetFloat(FilmicBId, s.yebisFilmicB);
        _material.SetFloat(MatrixBiasId, s.yebisMatrixBias);
        _material.SetFloat(GainMinId, s.yebisGainMin);
        _material.SetFloat(GainMaxId, s.yebisGainMax);
        _material.SetFloat(ContrastMixId, s.yebisContrastMix);
        _material.SetFloat(Pre3898Id, s.yebisPre3898);
        _material.SetFloat(CombineGainId, s.yebisCombineGain);
        _material.SetFloat(FirstMatrixId, s.yebisFirstMatrix);
        _material.SetFloat(SecondMatrixId, s.yebisSecondMatrix);
        _material.SetFloat(DisplayGammaId, s.yebisDisplayGamma);
        // Confirmed combine. 0 would restore L2Yebis_GradeLegacy.
        _material.SetFloat(CaptureFormulaId, 1f);
        // 0 full grade. 1 sample, 2 matrix, 3 before power, 4 after power. F7 cycles these.
        _material.SetFloat(GradeStageId, L2YebisLiveCapture.GradeStage);
    }

    void FillBloomExcludeMask(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RTHandle cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
        if (_bloomExclude == null || cameraDepth == null)
        {
            BindBloomExclude(cmd, false);
            return;
        }

        EnsureExcludeSkyMaterial();
        CoreUtils.SetRenderTarget(cmd, _bloomExclude, cameraDepth, ClearFlag.Color, Color.black);

        if (_excludeSkyMaterial != null)
        {
            int skyPass = _excludeSkyMaterial.FindPass("BloomExclude");
            if (skyPass >= 0)
            {
                cmd.DrawProcedural(
                    Matrix4x4.identity,
                    _excludeSkyMaterial,
                    skyPass,
                    MeshTopology.Triangles,
                    3,
                    1);
            }
        }

        L2CloudSimple clouds = L2CloudSimple.Instance;
        if (clouds != null)
            clouds.DrawBloomExclude(cmd);

        L2CelestialDiscs discs = L2CelestialDiscs.Instance;
        if (discs != null)
        {
            discs.DrawSunBloomExclude(cmd);
            discs.DrawMoonBloomInclude(cmd);
        }

        BindBloomExclude(cmd, true);
    }

    void BindBloomExclude(CommandBuffer cmd, bool enabled)
    {
        float on = enabled && _bloomExclude != null ? 1f : 0f;
        _material.SetFloat(BloomExcludeId, on);
        cmd.SetGlobalFloat(BloomExcludeId, on);
        RenderTargetIdentifier mask = enabled && _bloomExclude != null
            ? _bloomExclude.nameID
            : (RenderTargetIdentifier)Texture2D.blackTexture;
        _material.SetTexture(BloomExcludeMaskId, enabled && _bloomExclude != null ? _bloomExclude.rt : Texture2D.blackTexture);
        cmd.SetGlobalTexture(BloomExcludeMaskId, mask);
    }

    void EnsureExcludeSkyMaterial()
    {
        if (_excludeSkyMaterial != null)
            return;

        Shader shader = Shader.Find("Hidden/L2/SkyBackdrop");
        if (shader != null)
            _excludeSkyMaterial = CoreUtils.CreateEngineMaterial(shader);
    }

    static RenderTextureDescriptor ColorDesc(RenderTextureDescriptor src, int width, int height)
    {
        RenderTextureDescriptor desc = src;
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;
        desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        desc.sRGB = false;
        desc.dimension = TextureDimension.Tex2D;
        desc.volumeDepth = 1;
        desc.useMipMap = false;
        desc.autoGenerateMips = false;
        desc.bindMS = false;
        if (width > 0)
            desc.width = width;
        if (height > 0)
            desc.height = height;
        return desc;
    }

    static Vector4 SourceScale(RTHandle source)
    {
        if (source != null && source.useScaling)
        {
            Vector4 s = source.rtHandleProperties.rtHandleScale;
            return new Vector4(s.x, s.y, 0f, 0f);
        }

        return new Vector4(1f, 1f, 0f, 0f);
    }

    // BlitCameraTexture keeps the camera viewport. That is wrong for 1/4 / 8x8 / 1x1
    // targets: either the draw is clipped or only the top-left crop is written.
    // Draw into the capture RT stays magenta: the clear lands, the triangle does not.
    // CopyTexture does not use that draw. Source must not be the bound target.
    string CopyCapture(CommandBuffer cmd, RTHandle source, RTHandle destination)
    {
        RenderTexture src = source != null ? source.rt : null;
        RenderTexture dst = destination != null ? destination.rt : null;
        if (src != null && dst != null
            && src.width == dst.width
            && src.height == dst.height
            && src.graphicsFormat == dst.graphicsFormat
            && src.antiAliasing <= 1
            && dst.antiAliasing <= 1)
        {
            cmd.SetRenderTarget(_captureControl.nameID);
            cmd.CopyTexture(src, 0, 0, dst, 0, 0);
            Debug.Log("[Yebis] CopyTexture " + src.width + "x" + src.height + " " + src.graphicsFormat);
            return "CopyTexture";
        }

        Debug.LogWarning("[Yebis] CopyTexture пропущен src=" + RtSize(source) + " dst=" + RtSize(destination));
        BlitFull(cmd, source, destination, PassCopy);
        return "Blit";
    }

    static string RtSize(RTHandle handle)
    {
        RenderTexture rt = handle != null ? handle.rt : null;
        if (rt == null)
            return "null";
        return rt.width + "x" + rt.height + " " + rt.graphicsFormat + " msaa=" + rt.antiAliasing;
    }

    void BlitFull(CommandBuffer cmd, RTHandle source, RTHandle destination, int pass)
    {
        if (source == null || destination == null)
            return;

        RenderTexture destRt = destination.rt;
        int w = destRt != null ? destRt.width : 8;
        int h = destRt != null ? destRt.height : 8;
        Vector4 scale = SourceScale(source);
        _material.SetVector(BlitScaleBiasId, scale);
        cmd.SetGlobalVector(BlitScaleBiasId, scale);
        cmd.SetGlobalTexture(BlitTextureId, source.nameID);
        cmd.SetRenderTarget(destination.nameID);
        cmd.SetViewport(new Rect(0f, 0f, w, h));
        // No MaterialPropertyBlock: Blitter's MPB drops material textures
        // (_AdaptedTex → black), so Adapt was lerp(0, luma, 0.08).
        cmd.DrawProcedural(Matrix4x4.identity, _material, pass, MeshTopology.Triangles, 3, 1);
    }

    void SetBlitTexelSize(RTHandle source)
    {
        RenderTexture rt = source != null ? source.rt : null;
        if (rt == null)
            return;

        _material.SetVector(
            KawaseTexelSizeId,
            new Vector4(1f / rt.width, 1f / rt.height, rt.width, rt.height));
    }

    void MaybeLogYebis(CommandBuffer cmd, RTHandle cameraColor)
    {
        _execFrame++;
        if (_execFrame > 5 && (_execFrame % 120) != 0)
            return;

        Vector4 camScale = SourceScale(cameraColor);
        Debug.Log(
            $"[Yebis] f={_execFrame} debug={_settings.yebisDebugMode} mix={_settings.yebisContrastMix:F2} " +
            $"camRt={(cameraColor != null && cameraColor.rt != null)} useScaling={(cameraColor != null && cameraColor.useScaling)} " +
            $"camScale=({camScale.x:F3},{camScale.y:F3}) " +
            $"luma0={RtInfo(_luma0)} luma1={RtInfo(_luma1)} luma2={RtInfo(_luma2)} " +
            $"adapted={RtInfo(_adapted)} prev={RtInfo(_adaptedPrev)} " +
            $"adaptLerp={Mathf.Clamp01(_settings.yebisAdaptationSensitivity * 0.16f):F3} seeded={_adaptedSeeded}");

        QueueReadback(cmd, _luma0, "luma0");
        QueueReadback(cmd, _luma1, "luma1");
        QueueReadback(cmd, _luma2, "luma2");
        QueueReadback(cmd, _adapted, "adapted");
        QueueReadback(cmd, _adaptedPrev, "adaptedPrev");
    }

    static string RtInfo(RTHandle handle)
    {
        if (handle == null)
            return "null";
        if (handle.rt == null)
            return "rt=null";
        return $"{handle.rt.width}x{handle.rt.height}";
    }

    void QueueReadback(CommandBuffer cmd, RTHandle handle, string tag)
    {
        if (handle == null || handle.rt == null)
        {
            Debug.Log($"[Yebis] {tag} skip rt=null f={_execFrame}");
            return;
        }

        RenderTexture rt = handle.rt;
        int width = rt.width;
        int height = rt.height;
        int frame = _execFrame;
        cmd.RequestAsyncReadback(rt, request =>
        {
            // RTHandles are released/reallocated before this callback (Dispose, resize).
            // Never touch the captured RenderTexture here.
            if (request.hasError)
            {
                Debug.LogWarning($"[Yebis] {tag} readback FAIL f={frame} {width}x{height}");
                return;
            }

            var bytes = request.GetData<byte>();
            int pixels = width * height;
            int stride = pixels > 0 ? bytes.Length / pixels : 0;
            if (stride < 1 || bytes.Length < 4)
            {
                Debug.Log($"[Yebis] {tag} f={frame} bytes={bytes.Length} {width}x{height}");
                return;
            }

            int r0 = bytes[0];
            int g0 = bytes[1];
            int b0 = bytes[2];
            int a0 = bytes[3];
            int mid = (pixels / 2) * stride;
            int rMid = bytes[mid];
            long sum = 0;
            int n = Mathf.Min(pixels, 64);
            for (int i = 0; i < n; i++)
                sum += bytes[i * stride];
            float avg = sum / (n * 255f);
            Debug.Log(
                $"[Yebis] {tag} f={frame} {width}x{height} stride={stride} " +
                $"p0=({r0},{g0},{b0},{a0}) midR={rMid} avgR={avg:F3}");
        });
    }
}
