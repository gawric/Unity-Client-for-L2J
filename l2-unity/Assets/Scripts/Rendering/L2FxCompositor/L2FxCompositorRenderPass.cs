using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Encode camera color → R8G8B8A8_UNorm, draw SkillEffect FX with D3D9 blends,
/// optional skill bloom + RGB Boost, decode back to the Unity camera target.
/// </summary>
public sealed class L2FxCompositorRenderPass : ScriptableRenderPass
{
    const int PostExtractPass = 0;
    const int PostKawasePass = 1;
    const int PostCompositePass = 2;
    const int PostDebugPass = 3;
    const int TransferEncodeSkyPass = 2;

    static readonly int D3D9ActiveId = Shader.PropertyToID("_L2FxD3D9CompositorActive");
    static readonly int DebugModeId = Shader.PropertyToID("_L2FxCompositorDebugMode");
    static readonly int SceneTexId = Shader.PropertyToID("_SceneTex");
    static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
    static readonly int BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    static readonly int BloomPreGainId = Shader.PropertyToID("_BloomPreGain");
    static readonly int BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    static readonly int FxGainId = Shader.PropertyToID("_FxGain");
    static readonly int KawaseOffsetId = Shader.PropertyToID("_KawaseOffset");
    static readonly int KawaseTexelSizeId = Shader.PropertyToID("_KawaseTexelSize");
    static readonly ShaderTagId ForwardOnlyTag = new ShaderTagId("UniversalForwardOnly");
    static readonly ProfilingSampler Sampler = new ProfilingSampler("L2Fx D3D9 Compositor");

    readonly L2FxCompositorSettings _settings;
    readonly Material _transferMaterial;
    readonly Material _postMaterial;

    RTHandle _l2Color;
    RTHandle _l2Scene;
    RTHandle _l2Sky;
    RTHandle _l2BloomA;
    RTHandle _l2BloomB;
    FilteringSettings _filtering;

    public L2FxCompositorRenderPass(
        L2FxCompositorSettings settings,
        Material transferMaterial,
        Material postMaterial)
    {
        _settings = settings;
        _transferMaterial = transferMaterial;
        _postMaterial = postMaterial;
        renderPassEvent = settings.renderPassEvent;
        _filtering = new FilteringSettings(RenderQueueRange.transparent, settings.effectLayerMask);
    }

    public void Dispose()
    {
        _l2Color?.Release();
        _l2Color = null;
        _l2Scene?.Release();
        _l2Scene = null;
        _l2Sky?.Release();
        _l2Sky = null;
        _l2BloomA?.Release();
        _l2BloomA = null;
        _l2BloomB?.Release();
        _l2BloomB = null;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;
        desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        desc.sRGB = false;

        RenderingUtils.ReAllocateIfNeeded(
            ref _l2Color,
            desc,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_L2FxD3D9Color");

        if (!WantSkillPost())
            return;

        RenderingUtils.ReAllocateIfNeeded(
            ref _l2Scene,
            desc,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_L2FxScene");

        RenderTextureDescriptor halfDesc = desc;
        halfDesc.width = Mathf.Max(1, desc.width / 2);
        halfDesc.height = Mathf.Max(1, desc.height / 2);

        RenderingUtils.ReAllocateIfNeeded(
            ref _l2BloomA,
            halfDesc,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_L2FxBloomA");
        RenderingUtils.ReAllocateIfNeeded(
            ref _l2BloomB,
            halfDesc,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_L2FxBloomB");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_transferMaterial == null || _l2Color == null)
        {
            L2FxGpuDrawQueue.FlushImmediateFallback();
            L2NameplateOverlayQueue.FlushImmediateFallback();
            return;
        }

        if (!ShouldRun(ref renderingData))
        {
            L2FxGpuDrawQueue.FlushImmediateFallback();
            L2NameplateOverlayQueue.FlushImmediateFallback();
            Shader.SetGlobalFloat(D3D9ActiveId, 0f);
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("L2Fx D3D9 Compositor");
        using (new ProfilingScope(cmd, Sampler))
        {
            Shader.SetGlobalFloat(D3D9ActiveId, 1f);
            Shader.SetGlobalFloat(DebugModeId, _settings.debugMode);

            RTHandle cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
            RTHandle cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            if (UseSkillPost())
                ExecuteSkillPost(context, ref renderingData, cmd, cameraColor, cameraDepth);
            else
                ExecuteLegacyComposite(context, ref renderingData, cmd, cameraColor, cameraDepth);

            DrawNameplatesAfterComposite(cmd, cameraColor);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void ExecuteLegacyComposite(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle cameraColor,
        RTHandle cameraDepth)
    {
        _transferMaterial.SetFloat("_TransferMode", _settings.encodeLinearToSrgb ? 1f : 0f);
        Blitter.BlitCameraTexture(cmd, cameraColor, _l2Color, _transferMaterial, 0);

        DrawCloudSheets(context, ref renderingData, cmd, _l2Color, cameraDepth);
        DrawHazeRing(context, ref renderingData, cmd, _l2Color, cameraDepth);
        DrawSkillFx(context, ref renderingData, cmd, _l2Color, cameraDepth, clearFx: false);
        DrawCelestialDiscs(context, ref renderingData, cmd, _l2Color, cameraDepth, "legacy", _l2Color);
        DecodeToCamera(cmd, _l2Color, cameraColor);
    }

    void ExecuteSkillPost(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle cameraColor,
        RTHandle cameraDepth)
    {
        // 1) Encode camera → UNORM working buffer (same path that already matched L2).
        _transferMaterial.SetFloat("_TransferMode", _settings.encodeLinearToSrgb ? 1f : 0f);
        Blitter.BlitCameraTexture(cmd, cameraColor, _l2Color, _transferMaterial, 0);

        // 2) Snapshot the colour pass (sky+world, A=1), then blend clouds onto it —
        // same as original: Tex Before is the sky, then SrcA/InvSrcA.
        cmd.CopyTexture(_l2Color, _l2Scene);
        DrawCloudSheets(context, ref renderingData, cmd, _l2Color, cameraDepth);

        // 3) Snapshot again so clouds stay in the scene copy (no fxGain/bloom).
        // Haze still draws after and covers the horizon belt.
        cmd.CopyTexture(_l2Color, _l2Scene);
        DrawHazeRing(context, ref renderingData, cmd, _l2Color, cameraDepth);
        DrawSkillFx(context, ref renderingData, cmd, _l2Color, cameraDepth, clearFx: false);

        BindPostParams();
        _postMaterial.SetTexture(SceneTexId, _l2Scene != null ? _l2Scene.rt : null);

        // 3) Bloom from what skills added above the scene (not from darken).
        Blitter.BlitCameraTexture(cmd, _l2Color, _l2BloomA, _postMaterial, PostExtractPass);

        _postMaterial.SetFloat(KawaseOffsetId, 1f);
        SetBlitTexelSize(_postMaterial, _l2BloomA);
        Blitter.BlitCameraTexture(cmd, _l2BloomA, _l2BloomB, _postMaterial, PostKawasePass);

        _postMaterial.SetFloat(KawaseOffsetId, 2f);
        SetBlitTexelSize(_postMaterial, _l2BloomB);
        Blitter.BlitCameraTexture(cmd, _l2BloomB, _l2BloomA, _postMaterial, PostKawasePass);

        if (_settings.debugMode == 2)
        {
            BlitDebug(cmd, _l2Scene, cameraColor);
            return;
        }

        if (_settings.debugMode == 3)
        {
            BlitDebug(cmd, _l2Color, cameraColor);
            return;
        }

        if (_settings.debugMode == 4)
        {
            BlitDebug(cmd, _l2BloomA, cameraColor);
            return;
        }

        // 4) RGB Boost on skill/sun/moon delta. Sky stays in the scene copy (no extra dest).
        DrawCelestialDiscs(context, ref renderingData, cmd, _l2Color, cameraDepth, "skillPost", _l2Scene);

        _postMaterial.SetTexture(BloomTexId, _l2BloomA != null ? _l2BloomA.rt : null);
        _postMaterial.SetFloat(
            "_TransferMode",
            _settings.debugMode == 1
                ? 3f
                : (_settings.decodeSrgbToLinear ? 2f : 0f));
        Blitter.BlitCameraTexture(cmd, _l2Color, cameraColor, _postMaterial, PostCompositePass);
    }

    void DrawCelestialDiscs(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle colorTarget,
        RTHandle cameraDepth,
        string path,
        RTHandle encodedScene)
    {
        float gain = _settings != null ? _settings.fxGain : 0f;
        float start = _settings != null ? _settings.skyBoostStart : 0f;
        float full = _settings != null ? _settings.skyBoostFull : 0f;
        if (full <= start + 0.001f)
        {
            start = 0.30f;
            full = 0.50f;
        }

        Shader.SetGlobalFloat(FxGainId, 1f);

        int celestialMask = _settings != null ? (int)_settings.celestialLayerMask : 0;
        if (celestialMask != 0)
            DrawFxLayer(context, ref renderingData, cmd, colorTarget, cameraDepth, celestialMask);

        L2FxSkyDebug.LogCpuAndEnqueueGpu(
            cmd,
            path,
            WantCelestial(),
            _settings != null && _settings.includeSkyInBoost,
            false,
            "camera skybox (UNORM DrawSkybox was empty)",
            encodedScene,
            colorTarget,
            renderingData.cameraData.camera,
            gain,
            start,
            full);
        Shader.SetGlobalFloat(FxGainId, gain);
    }

    void DrawSkillFx(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle colorTarget,
        RTHandle cameraDepth,
        bool clearFx)
    {
        ClearFlag clear = clearFx ? ClearFlag.Color : ClearFlag.None;
        if (_settings.bindCameraDepth && cameraDepth != null)
            CoreUtils.SetRenderTarget(cmd, colorTarget, cameraDepth, clear, Color.clear);
        else
            CoreUtils.SetRenderTarget(cmd, colorTarget, clear, Color.clear);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        DrawingSettings drawing = CreateDrawingSettings(
            ForwardOnlyTag,
            ref renderingData,
            SortingCriteria.SortingLayer | SortingCriteria.RenderQueue);
        drawing.perObjectData = PerObjectData.None;

        context.DrawRenderers(renderingData.cullResults, ref drawing, ref _filtering);

        L2FxGpuDrawQueue.Flush(cmd);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }

    static void BindUnormColorKeepContents(CommandBuffer cmd, RTHandle colorTarget, RTHandle cameraDepth)
    {
        if (colorTarget == null)
            return;

        if (cameraDepth != null)
        {
            cmd.SetRenderTarget(
                colorTarget,
                RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store,
                cameraDepth,
                RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store);
            return;
        }

        cmd.SetRenderTarget(colorTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
    }

    void DrawFxLayer(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle colorTarget,
        RTHandle cameraDepth,
        int layerMask)
    {
        if (layerMask == 0)
            return;

        BindUnormColorKeepContents(
            cmd,
            colorTarget,
            _settings.bindCameraDepth ? cameraDepth : null);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        var filtering = new FilteringSettings(RenderQueueRange.transparent, layerMask);
        DrawingSettings drawing = CreateDrawingSettings(
            ForwardOnlyTag,
            ref renderingData,
            SortingCriteria.SortingLayer | SortingCriteria.RenderQueue);
        drawing.perObjectData = PerObjectData.None;
        context.DrawRenderers(renderingData.cullResults, ref drawing, ref filtering);
    }

    void DrawCloudSheets(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle colorTarget,
        RTHandle cameraDepth)
    {
        int cloudMask = _settings != null ? (int)_settings.cloudLayerMask : 0;
        if (cloudMask == 0)
            return;

        Shader.SetGlobalFloat(FxGainId, 1f);
        DrawFxLayer(context, ref renderingData, cmd, colorTarget, cameraDepth, cloudMask);
    }

    void DrawHazeRing(
        ScriptableRenderContext context,
        ref RenderingData renderingData,
        CommandBuffer cmd,
        RTHandle colorTarget,
        RTHandle cameraDepth)
    {
        int hazeMask = _settings != null ? (int)_settings.hazeLayerMask : 0;
        if (hazeMask == 0)
            return;

        Shader.SetGlobalFloat(FxGainId, 1f);
        DrawFxLayer(context, ref renderingData, cmd, colorTarget, cameraDepth, hazeMask);
        Shader.SetGlobalFloat(FxGainId, _settings != null ? _settings.fxGain : 1f);
    }

    void DrawNameplatesAfterComposite(CommandBuffer cmd, RTHandle cameraColor)
    {
        if (cmd == null || cameraColor == null)
        {
            L2NameplateOverlayQueue.Clear();
            return;
        }

        CoreUtils.SetRenderTarget(cmd, cameraColor, ClearFlag.None, Color.clear);
        L2NameplateOverlayQueue.Flush(cmd);
    }

    void DecodeToCamera(CommandBuffer cmd, RTHandle source, RTHandle cameraColor)
    {
        _transferMaterial.SetFloat("_TransferMode", _settings.decodeSrgbToLinear ? 2f : 0f);
        Blitter.BlitCameraTexture(cmd, source, cameraColor, _transferMaterial, 1);

        if (_settings.debugMode == 1)
        {
            _transferMaterial.SetFloat("_TransferMode", 3f);
            Blitter.BlitCameraTexture(cmd, source, cameraColor, _transferMaterial, 1);
        }
    }

    void BlitDebug(CommandBuffer cmd, RTHandle source, RTHandle cameraColor)
    {
        _postMaterial.SetFloat("_TransferMode", _settings.decodeSrgbToLinear ? 2f : 0f);
        Blitter.BlitCameraTexture(cmd, source, cameraColor, _postMaterial, PostDebugPass);
    }

    void BindPostParams()
    {
        _postMaterial.SetFloat(BloomThresholdId, _settings.bloomThreshold);
        _postMaterial.SetFloat(BloomPreGainId, 1f);
        _postMaterial.SetFloat(BloomIntensityId, _settings.bloomIntensity);
        _postMaterial.SetFloat(FxGainId, _settings.fxGain);
        Shader.SetGlobalFloat(FxGainId, _settings.fxGain);
    }

    static void SetBlitTexelSize(Material material, RTHandle source)
    {
        RenderTexture rt = source != null ? source.rt : null;
        if (rt == null)
            return;

        material.SetVector(
            KawaseTexelSizeId,
            new Vector4(1f / rt.width, 1f / rt.height, rt.width, rt.height));
    }

    bool WantSkillPost()
    {
        return _settings != null && _settings.enableSkillPost && _postMaterial != null;
    }

    bool WantCelestial()
    {
        return _settings != null && _settings.celestialLayerMask != 0;
    }

    bool UseSkillPost()
    {
        return WantSkillPost() && _l2Scene != null && _l2BloomA != null && _l2BloomB != null;
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        Shader.SetGlobalFloat(D3D9ActiveId, 0f);
    }

    bool ShouldRun(ref RenderingData renderingData)
    {
        if (_settings == null || !_settings.enableD3D9Compositor)
            return false;

        CameraType type = renderingData.cameraData.camera.cameraType;
        if (_settings.gameCameraOnly &&
            type != CameraType.Game &&
            type != CameraType.VR)
        {
            return false;
        }

        return true;
    }
}
