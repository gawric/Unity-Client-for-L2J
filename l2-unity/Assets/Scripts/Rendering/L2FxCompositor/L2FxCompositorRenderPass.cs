using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// Encode camera color → R8G8B8A8_UNorm, draw SkillEffect FX with D3D9 blends,
/// decode back to the Unity camera target.
/// </summary>
public sealed class L2FxCompositorRenderPass : ScriptableRenderPass
{
    static readonly int D3D9ActiveId = Shader.PropertyToID("_L2FxD3D9CompositorActive");
    static readonly int DebugModeId = Shader.PropertyToID("_L2FxCompositorDebugMode");
    static readonly ShaderTagId ForwardOnlyTag = new ShaderTagId("UniversalForwardOnly");
    static readonly ProfilingSampler Sampler = new ProfilingSampler("L2Fx D3D9 Compositor");

    readonly L2FxCompositorSettings _settings;
    readonly Material _transferMaterial;
    RTHandle _l2Color;
    FilteringSettings _filtering;

    public L2FxCompositorRenderPass(L2FxCompositorSettings settings, Material transferMaterial)
    {
        _settings = settings;
        _transferMaterial = transferMaterial;
        renderPassEvent = settings.renderPassEvent;
        _filtering = new FilteringSettings(RenderQueueRange.transparent, settings.effectLayerMask);
    }

    public void Dispose()
    {
        _l2Color?.Release();
        _l2Color = null;
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
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_transferMaterial == null || _l2Color == null)
            return;

        if (!ShouldRun(ref renderingData))
        {
            L2FxGpuDrawQueue.FlushImmediateFallback();
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

            // 1) Encode Unity linear camera color into raw UNORM (L2-like bytes).
            _transferMaterial.SetFloat("_TransferMode", _settings.encodeLinearToSrgb ? 1f : 0f);
            Blitter.BlitCameraTexture(cmd, cameraColor, _l2Color, _transferMaterial, 0);

            // 2) Draw L2 FX into the UNORM buffer with camera depth for occlusion.
            if (_settings.bindCameraDepth && cameraDepth != null)
                CoreUtils.SetRenderTarget(cmd, _l2Color, cameraDepth, ClearFlag.None, Color.clear);
            else
                CoreUtils.SetRenderTarget(cmd, _l2Color, ClearFlag.None, Color.clear);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DrawingSettings drawing = CreateDrawingSettings(
                ForwardOnlyTag,
                ref renderingData,
                SortingCriteria.SortingLayer | SortingCriteria.RenderQueue);
            drawing.perObjectData = PerObjectData.None;

            context.DrawRenderers(renderingData.cullResults, ref drawing, ref _filtering);

            // GPU-instanced batches enqueued from ParticleGroupGpuDrawer (LateUpdate).
            L2FxGpuDrawQueue.Flush(cmd);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 3) Decode UNORM back into the Unity camera color target.
            _transferMaterial.SetFloat("_TransferMode", _settings.decodeSrgbToLinear ? 2f : 0f);
            Blitter.BlitCameraTexture(cmd, _l2Color, cameraColor, _transferMaterial, 1);

            if (_settings.debugMode == 1)
            {
                _transferMaterial.SetFloat("_TransferMode", 3f);
                Blitter.BlitCameraTexture(cmd, _l2Color, cameraColor, _transferMaterial, 1);
            }
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
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
