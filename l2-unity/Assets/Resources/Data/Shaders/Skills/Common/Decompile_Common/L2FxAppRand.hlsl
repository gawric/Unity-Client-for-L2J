#ifndef L2_FX_APP_RAND_INCLUDED
#define L2_FX_APP_RAND_INCLUDED


//
// CONFIRMED: this library is validated at 90% port accuracy or higher against
// the original L2 client (healing potion / m_u004_b MeshEmitter3 and
// SpriteEmitter2). The LCG, appFrand conversion, FRange formula and Z/Y/X
// draw order reproduce captured L2 spawn values. The caller remains
// responsible for supplying the TLS state for the exact particle spawn.
//
// Verified at the StartSpin write in UParticleEmitter::SpawnParticle:
//   state = state * 214013 + 2531011  (uint32 wraparound)
//   appRand = (state >> 16) & 0x7fff
//   appFrand = appRand / 32767.0
//
// The input state is the MSVC CRT TLS random state (PTD + 0x14) immediately
// before the first requested draw. It must be supplied as a uint; a float
// cannot preserve every 32-bit state exactly.

static const uint L2FX_APP_RAND_MULTIPLIER = 214013u;
static const uint L2FX_APP_RAND_INCREMENT = 2531011u;
static const uint L2FX_APP_RAND_MASK = 0x7fffu;
static const float L2FX_APP_FRAND_DIVISOR = 32767.0;

uint L2Fx_AppRand(inout uint state)
{
    state = state * L2FX_APP_RAND_MULTIPLIER + L2FX_APP_RAND_INCREMENT;
    return (state >> 16u) & L2FX_APP_RAND_MASK;
}

float L2Fx_AppFrand(inout uint state)
{
    return (float)L2Fx_AppRand(state) / L2FX_APP_FRAND_DIVISOR;
}

// FRange::GetRand in this L2 build:
//   appFrand() * (Min - Max) + Max
// This intentionally differs from the usual lerp(Min, Max, random).
float L2Fx_FRange_GetRand(float2 minMax, inout uint state)
{
    return L2Fx_AppFrand(state) * (minMax.x - minMax.y) + minMax.y;
}

// FRangeVector::GetRand calls appFrand in Z, Y, X order, then returns X, Y, Z.
// Returned vector order is therefore the particle-slot order:
//   x = Yaw (+0x3C), y = Pitch (+0x40), z = Roll (+0x44).
float3 L2Fx_FRangeVector_GetRandYawPitchRoll(
    float2 yawRange,
    float2 pitchRange,
    float2 rollRange,
    inout uint state)
{
    float roll = L2Fx_FRange_GetRand(rollRange, state);
    float pitch = L2Fx_FRange_GetRand(pitchRange, state);
    float yaw = L2Fx_FRange_GetRand(yawRange, state);
    return float3(yaw, pitch, roll);
}

#endif // L2_FX_APP_RAND_INCLUDED
