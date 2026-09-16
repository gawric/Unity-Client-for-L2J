using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Frame-wide switch: when the URP feature is present and enabled, GPU FX
/// enqueue for the compositor instead of drawing immediately.
/// Skybox stays on the camera — DrawSkybox into a UNORM scratch writes nothing
/// (RenderDoc/readback: scratch stays #000000 a=0).
/// </summary>
public static class L2FxCompositorRuntime
{
    static bool s_PreferQueue;
    static bool s_Hooked;

    public static bool PreferGpuQueue => s_PreferQueue;

    public static void SetPreferGpuQueue(bool prefer)
    {
        s_PreferQueue = prefer;
        EnsureHook();
    }

    static void EnsureHook()
    {
        if (s_Hooked)
            return;

        s_Hooked = true;
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
    }

    static void OnEndContextRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
    {
        L2FxGpuDrawQueue.FlushImmediateFallback();
        L2NameplateOverlayQueue.FlushImmediateFallback();
        Shader.SetGlobalFloat("_L2FxD3D9CompositorActive", 0f);
    }
}
