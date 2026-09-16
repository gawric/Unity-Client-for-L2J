using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP feature: L2 D3D9-compatible FX compositor (raw UNORM + PTDS blends).
/// </summary>
public sealed class L2FxCompositorRendererFeature : ScriptableRendererFeature
{
    public L2FxCompositorSettings settings = new L2FxCompositorSettings();

    [SerializeField]
    Shader transferShader;

    [SerializeField]
    Shader postShader;

    Material _transferMaterial;
    Material _postMaterial;
    L2FxCompositorRenderPass _pass;

    public override void Create()
    {
        if (transferShader == null)
            transferShader = Shader.Find("Hidden/L2/FxColorTransfer");
        if (postShader == null)
            postShader = Shader.Find("Hidden/L2/FxPostBloomContrast");

        if (transferShader != null)
        {
            if (_transferMaterial == null || _transferMaterial.shader != transferShader)
                _transferMaterial = CoreUtils.CreateEngineMaterial(transferShader);
        }

        if (postShader != null)
        {
            if (_postMaterial == null || _postMaterial.shader != postShader)
                _postMaterial = CoreUtils.CreateEngineMaterial(postShader);
        }

        _pass?.Dispose();
        _pass = new L2FxCompositorRenderPass(settings, _transferMaterial, _postMaterial)
        {
            renderPassEvent = settings != null
                ? settings.renderPassEvent
                : RenderPassEvent.BeforeRenderingPostProcessing
        };

        bool prefer = settings != null && settings.enableD3D9Compositor;
        L2FxCompositorRuntime.SetPreferGpuQueue(prefer && isActive);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bool prefer = settings != null && settings.enableD3D9Compositor && isActive;
        L2FxCompositorRuntime.SetPreferGpuQueue(prefer);

        if (!prefer)
            return;

        if (_transferMaterial == null)
            return;

        if (settings.gameCameraOnly)
        {
            CameraType type = renderingData.cameraData.camera.cameraType;
            if (type != CameraType.Game && type != CameraType.VR)
                return;
        }

        _pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
        _pass = null;
        CoreUtils.Destroy(_transferMaterial);
        _transferMaterial = null;
        CoreUtils.Destroy(_postMaterial);
        _postMaterial = null;
        L2FxGpuDrawQueue.Clear();
        L2NameplateOverlayQueue.Clear();
        base.Dispose(disposing);
    }
}
