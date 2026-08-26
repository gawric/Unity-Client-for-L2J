using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Per-slot uniforms that used to live on unique material copies.
/// Layout must match <c>L2FxParticleInstance</c> in L2FxInstancing.hlsl.
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
    public float pad0;
    public float pad1;
}
