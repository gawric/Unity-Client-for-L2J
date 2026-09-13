#ifndef L2_FX_HE_UNIT_SCALE_INCLUDED
#define L2_FX_HE_UNIT_SCALE_INCLUDED

// High Elf stores Interlude UC linear UU already scaled (d_mon_fire2_ca dump):
//   StartSize 15 → 8.746, loc ±12 → ±6.997, maxAbsVel 10000 → 5830.8
//   live / UC ≈ 0.58308
// Interlude sprites keep enable=0 and use K World 1.1 * UU / 52.5 unchanged.

float L2FxHE_ResolveUnitScale(float enable, float scale)
{
    return (enable > 0.5 && scale > 1e-6) ? scale : 1.0;
}

#endif
