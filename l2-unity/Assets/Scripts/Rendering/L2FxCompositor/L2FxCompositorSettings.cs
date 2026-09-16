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

    [Tooltip("Sun/moon/star billboards (TagManager: L2Sky). Drawn in the UNORM blit after the world copy.")]
    public LayerMask celestialLayerMask = 1 << 20;

    [Tooltip("Horizon WhiteRing (TagManager: L2Haze). Drawn in UNORM after the scene copy, so skill post fxGain + bloom apply (same as sun/moon/skills).")]
    public LayerMask hazeLayerMask = 1 << 21;

    [Tooltip("Day cloud sheets (TagManager: L2Clouds). Drawn in UNORM after encode, before haze. Not in skill bloom.")]
    public LayerMask cloudLayerMask = 1 << 22;

    [Tooltip("Legacy diagnostic switch. Sky display correction now comes from L2SkyPostLut, outside this compositor.")]
    public bool includeSkyInBoost = false;

    [Tooltip("Peak RGB below this is night sky — no boost. Night LUT #283146 ≈ 0.27.")]
    [Range(0f, 1f)]
    public float skyBoostStart = 0.30f;

    [Tooltip("Peak RGB above this gets full fxGain (day blue). Sun/moon always use fxGain.")]
    [Range(0f, 1f)]
    public float skyBoostFull = 0.50f;

    [Tooltip("Encode Unity linear camera color into display-referred UNORM before FX.")]
    public bool encodeLinearToSrgb = true;

    [Tooltip("Decode UNORM back to linear when writing into the Unity camera target.")]
    public bool decodeSrgbToLinear = true;

    [Tooltip("Bind camera depth so FX still occlude against the world.")]
    public bool bindCameraDepth = true;

    [Tooltip("Skip SceneView / preview / reflection cameras.")]
    public bool gameCameraOnly = true;

    [Tooltip("0 = off, 1 = show L2 UNORM buffer, 2 = encode only, 3 = skill FX only, 4 = bloom only.")]
    public int debugMode;

    [Header("Skill post")]
    [Tooltip("Bloom + RGB Boost on SkillEffect, L2Haze, sun/moon. Night sky does not.")]
    public bool enableSkillPost = true;

    [Tooltip("Luma cutoff before downsample. Skills below this do not feed bloom.")]
    [Range(0f, 1f)]
    public float bloomThreshold = 0.2f;

    [Tooltip("Bloom add after Kawase. 0 disables bloom.")]
    public float bloomIntensity = 0.95f;

    [Tooltip("Post RGB Boost for ALL skills. 0 = skills off, 1 = as drawn, 2 = L2-like.")]
    [Range(0f, 4f)]
    public float fxGain = 2f;
}
