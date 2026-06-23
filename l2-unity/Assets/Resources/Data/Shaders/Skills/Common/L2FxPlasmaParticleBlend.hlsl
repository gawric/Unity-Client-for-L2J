#ifndef L2_FX_PLASMA_PARTICLE_BLEND_INCLUDED
#define L2_FX_PLASMA_PARTICLE_BLEND_INCLUDED

// Shared color control for fx_m_t0005-style plasma/particle textures.
// Low-luma texels are the soft plasma fill; high-luma texels are hot cores/lines.
half3 L2Fx_PlasmaParticle_ApplyLowLumaRgbScale(
    half3 rgb,
    half3 texRgb,
    float plasmaRgbScale,
    float plasmaLumaMax)
{
    float luma = dot((float3)texRgb, float3(0.2126, 0.7152, 0.0722));
    float plasma = 1.0 - smoothstep(plasmaLumaMax * 0.45, plasmaLumaMax, luma);
    return rgb * (half)lerp(1.0, plasmaRgbScale, plasma);
}

#endif // L2_FX_PLASMA_PARTICLE_BLEND_INCLUDED
