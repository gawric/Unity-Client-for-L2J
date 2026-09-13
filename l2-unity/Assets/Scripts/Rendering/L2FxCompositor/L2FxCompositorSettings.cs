using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Runtime settings for the L2 D3D9-compatible effect compositor.
/// Evidence: L2 Colour Pass FB = B8G8R8A8_UNORM + PTDS blends;
/// Unity DrawTransparentObjects = R8G8B8A8_SRGB.
/// </summary>
[Serializable]
public sealed class L2FxCompositorSettings
{
    [Tooltip("When enabled, L2 FX draw into a raw UNORM buffer with encode/decode around the scene.")]
    public bool enableD3D9Compositor = true;

    [Tooltip("Injection relative to URP transparent/post. Match L2: after world, before UI/post.")]
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    [Tooltip("Unity layer used for generated L2 effects (TagManager: SkillEffect).")]
    public LayerMask effectLayerMask = 1 << 18;

    [Tooltip("Encode Unity linear camera color into display-referred UNORM before FX.")]
    public bool encodeLinearToSrgb = true;

    [Tooltip("Decode UNORM back to linear when writing into the Unity camera target.")]
    public bool decodeSrgbToLinear = true;

    [Tooltip("Bind camera depth so FX still occlude against the world.")]
    public bool bindCameraDepth = true;

    [Tooltip("Skip SceneView / preview / reflection cameras.")]
    public bool gameCameraOnly = true;

    [Tooltip("0 = off, 1 = show L2 UNORM buffer, 2 = show encode only.")]
    public int debugMode;
}
