#ifndef L2_FX_D3D9_COLOR_PATH_INCLUDED
#define L2_FX_D3D9_COLOR_PATH_INCLUDED

// Set by L2FxCompositorRenderPass while the UNORM path is active.
float _L2FxD3D9CompositorActive;

float L2Fx_D3d9ColorPathEnabled()
{
    return step(0.5, _L2FxD3D9CompositorActive);
}

// Passthrough: author Boost / Gamma on materials & textures manually.
// Compositor no longer forces Boost=1 or Gamma=0.
float L2Fx_D3d9EffectiveGammaToggle(float materialToggle)
{
    return materialToggle;
}

float L2Fx_D3d9EffectiveRgbBoost(float materialBoost)
{
    return materialBoost;
}

#endif
