using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// L2 appRand LCG. Burst-safe, O(log n) skip, bit-identical to the draw loop.
/// </summary>
public static class L2AppRand
{
    public const uint Multiplier = 214013u;
    public const uint Increment = 2531011u;
    public const int MeshSpawnSlotToSlotDrawCount = 31;
    public const int MeshSpawnDrawsBeforeStartSpin = 22;
    public const int SpriteMotionSlotStride = 28;
    public const int SpriteSpinDraws = 22;

    public static uint Advance(uint state, int drawCount)
    {
        if (drawCount <= 0)
            return state;

        uint mul = Multiplier;
        uint add = Increment;
        uint a = 1u;
        uint c = 0u;
        int n = drawCount;
        while (n > 0)
        {
            if ((n & 1) != 0)
            {
                c = unchecked(mul * c + add);
                a = unchecked(mul * a);
            }

            add = unchecked(mul * add + add);
            mul = unchecked(mul * mul);
            n >>= 1;
        }

        return unchecked(a * state + c);
    }

    public static float BitsToFloat(uint state)
    {
        return math.asfloat(state);
    }

    public static void ResolveGpuInstanceRandBits(
        bool hasMeshSpawn,
        bool hasStartSpin,
        uint meshEmitter3AppRandBaseState,
        uint spriteEmitterAppRandBaseState,
        int slotIndex,
        out float meshSpawnRandBits,
        out float startSpinRandBits,
        out float spriteMotionRandBits,
        out float spriteSpinRandBits)
    {
        meshSpawnRandBits = 0f;
        startSpinRandBits = 0f;
        uint spriteMotion = Advance(spriteEmitterAppRandBaseState, slotIndex * SpriteMotionSlotStride);
        spriteMotionRandBits = BitsToFloat(spriteMotion);
        spriteSpinRandBits = BitsToFloat(Advance(spriteMotion, SpriteSpinDraws));

        if (hasMeshSpawn)
        {
            uint velocityState = Advance(
                meshEmitter3AppRandBaseState,
                slotIndex * MeshSpawnSlotToSlotDrawCount);
            meshSpawnRandBits = BitsToFloat(velocityState);
            startSpinRandBits = BitsToFloat(
                Advance(velocityState, MeshSpawnDrawsBeforeStartSpin));
            return;
        }

        if (hasStartSpin)
        {
            startSpinRandBits = BitsToFloat(
                Advance(
                    meshEmitter3AppRandBaseState,
                    slotIndex * MeshSpawnSlotToSlotDrawCount));
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void VerifyAdvanceMatchesLoop()
    {
        uint[] starts = { 1u, 12345u, 0x6FEC3FC2u };
        int[] counts = { 0, 1, 7, 22, 28, 31, 31 * 249, 250 * 31 + 22 };
        for (int s = 0; s < starts.Length; s++)
        {
            for (int c = 0; c < counts.Length; c++)
            {
                uint loop = starts[s];
                int n = counts[c];
                for (int i = 0; i < n; i++)
                    loop = unchecked(loop * Multiplier + Increment);
                uint fast = Advance(starts[s], n);
                if (loop != fast)
                {
                    Debug.LogError(
                        $"[L2AppRand] skip mismatch start={starts[s]} n={n} loop={loop} fast={fast}");
                    return;
                }
            }
        }
    }
#endif
}
