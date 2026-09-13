# L2 D3D9 FX Compositor — how to enable

RenderDoc evidence (same skill):

- L2 Vulkan FB: `B8G8R8A8_UNORM`, Brighten=`One/InvSrcColor`, Translucent=`One/One`
- Unity: `R8G8B8A8_SRGB` inside `DrawTransparentObjects`

## Enable steps

One-click: **L2 → Effects → Compositor → Enable (Migrate + Exclude Transparent + Activate Feature)**

Or step by step:

1. **Migrate Generated Prefabs To SkillEffect**
2. **Exclude SkillEffect From Default Transparent**
3. Open `Assets/Rendering/URP/URP_Renderer.asset` and confirm feature Active
4. Settings: Event `BeforeRenderingPostProcessing`, layer SkillEffect, Encode/Decode on, bind depth on

## Verify in RenderDoc

Look for marker `L2Fx D3D9 Compositor`:

1. Encode blit into `_L2FxD3D9Color` (`R8G8B8A8_UNorm`)
2. Effect draws (MeshEmitter / SpriteEmitter) with same PTDS blends
3. Decode blit back to `_CameraColorAttachmentA`

Without FX, world should look almost identical (sRGB roundtrip).
With FX, compare Brighten/Translucent to L2 Colour Pass #32.
