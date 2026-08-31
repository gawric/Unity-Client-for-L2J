using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Per-slot uniforms that used to live on unique material copies.
/// Layout must match <c>L2FxParticleInstance</c> in L2FxInstancing.hlsl.
/// CPU layout: L2FxParticleInstance (56 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct L2FxParticleInstance
{
    public Vector4 ownerWorldPos;
    public float startTime;
    public float seed;
    public float meshSpawnRandBits;
    public float startSpinRandBits;
    public float spriteMotionRandBits;
    public float spriteSpinRandBits;
    /// <summary>
    /// UC AddLocationFromOtherEmitter baked at spawn.
    /// w=0: owner-relative UE XYZ; w=1: absolute Unity world XYZ.
    /// </summary>
    public Vector4 spawnLocationAddUe;
}
