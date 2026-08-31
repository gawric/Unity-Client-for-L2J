#ifndef L2FX_INSTANCING_INCLUDED
#define L2FX_INSTANCING_INCLUDED

// Per-particle uniforms that ParticleGroup used to stamp onto unique materials.
// Include AFTER UnityPerMaterial CBUFFER so #define remaps later reads.
// CPU layout: L2FxParticleInstance (56 bytes).

struct L2FxParticleInstance
{
    float4 ownerWorldPos;
    float startTime;
    float seed;
    float meshSpawnRandBits;
    float startSpinRandBits;
    float spriteMotionRandBits;
    float spriteSpinRandBits;
    float4 spawnLocationAddUe;
};

StructuredBuffer<L2FxParticleInstance> _L2FxParticleSlots;

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
L2FxParticleInstance L2Fx_LoadInstance()
{
    return _L2FxParticleSlots[unity_InstanceID];
}

float3 L2Fx_ApplySpawnLocationAddUe(float3 positionUe)
{
    float4 add = L2Fx_LoadInstance().spawnLocationAddUe;
    return add.w > 0.5 ? positionUe : positionUe + add.xyz;
}

float3 L2Fx_ApplySpawnWorldPositionOs(float3 positionOs)
{
    float4 add = L2Fx_LoadInstance().spawnLocationAddUe;
    return add.w > 0.5
        ? TransformWorldToObject(add.xyz) + positionOs
        : positionOs;
}

#define _StartTime (L2Fx_LoadInstance().startTime)
#define _Seed (L2Fx_LoadInstance().seed)
#define _MeshSpawnRandStateBits (L2Fx_LoadInstance().meshSpawnRandBits)
#define _StartSpinRandStateBits (L2Fx_LoadInstance().startSpinRandBits)
#define _SpriteMotionRandStateBits (L2Fx_LoadInstance().spriteMotionRandBits)
#define _SpriteSpinRandStateBits (L2Fx_LoadInstance().spriteSpinRandBits)
#define _OwnerWorldPos (L2Fx_LoadInstance().ownerWorldPos)
#else
float3 L2Fx_ApplySpawnLocationAddUe(float3 positionUe)
{
    return positionUe;
}

float3 L2Fx_ApplySpawnWorldPositionOs(float3 positionOs)
{
    return positionOs;
}
#endif

#endif
