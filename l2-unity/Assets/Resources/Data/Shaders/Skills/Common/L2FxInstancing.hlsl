#ifndef L2FX_INSTANCING_INCLUDED
#define L2FX_INSTANCING_INCLUDED

// Per-particle uniforms that ParticleGroup used to stamp onto unique materials.
// Include AFTER UnityPerMaterial CBUFFER so #define remaps later reads.
// CPU layout: L2FxParticleInstance (48 bytes).

struct L2FxParticleInstance
{
    float4 ownerWorldPos;
    float startTime;
    float seed;
    float meshSpawnRandBits;
    float startSpinRandBits;
    float spriteMotionRandBits;
    float spriteSpinRandBits;
    float pad0;
    float pad1;
};

StructuredBuffer<L2FxParticleInstance> _L2FxParticleSlots;

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
L2FxParticleInstance L2Fx_LoadInstance()
{
    return _L2FxParticleSlots[unity_InstanceID];
}

#define _StartTime (L2Fx_LoadInstance().startTime)
#define _Seed (L2Fx_LoadInstance().seed)
#define _MeshSpawnRandStateBits (L2Fx_LoadInstance().meshSpawnRandBits)
#define _StartSpinRandStateBits (L2Fx_LoadInstance().startSpinRandBits)
#define _SpriteMotionRandStateBits (L2Fx_LoadInstance().spriteMotionRandBits)
#define _SpriteSpinRandStateBits (L2Fx_LoadInstance().spriteSpinRandBits)
#define _OwnerWorldPos (L2Fx_LoadInstance().ownerWorldPos)
#endif

#endif
