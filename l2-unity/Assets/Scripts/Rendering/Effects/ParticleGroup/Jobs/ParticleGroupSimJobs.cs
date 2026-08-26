using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Burst jobs for ParticleGroup slot lifetime, GPU burst activate, and instancing pack.
/// GameObject/Material APIs stay on the main thread.
/// </summary>
public static class ParticleGroupSimJobs
{
    public const int ExpireJobMinSlots = 16;
    public const int GpuBurstJobMinSlots = 8;

    [BurstCompile]
    public struct ExpireJob : IJob
    {
        public NativeArray<float> spawnTimes;
        public NativeArray<byte> active;
        public float now;
        public float duration;
        public int count;

        public void Execute()
        {
            for (int i = 0; i < count; i++)
            {
                if (active[i] == 0)
                    continue;
                if (now - spawnTimes[i] >= duration)
                    active[i] = 0;
            }
        }
    }

    [BurstCompile]
    public struct ActivateGpuParallelJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<L2FxParticleInstance> slots;
        [NativeDisableParallelForRestriction] public NativeArray<byte> active;
        [NativeDisableParallelForRestriction] public NativeArray<float> spawnTimes;
        [ReadOnly] public NativeArray<float> seeds;
        public float now;
        public float shaderStartTime;
        public uint meshBase;
        public uint spriteBase;
        public byte hasMeshSpawn;
        public byte hasStartSpin;
        public int startIndex;

        public void Execute(int index)
        {
            int slot = startIndex + index;
            spawnTimes[slot] = now;
            active[slot] = 1;
            WriteGpuSlot(
                slots,
                slot,
                shaderStartTime,
                seeds[index],
                hasMeshSpawn,
                hasStartSpin,
                meshBase,
                spriteBase);
        }
    }

    [BurstCompile]
    public struct ActivateGpuSequentialJob : IJob
    {
        public NativeArray<L2FxParticleInstance> slots;
        public NativeArray<byte> active;
        public NativeArray<float> spawnTimes;
        [ReadOnly] public NativeArray<float> seeds;
        public float now;
        public float shaderStartTime;
        public uint meshBase;
        public uint spriteBase;
        public byte hasMeshSpawn;
        public byte hasStartSpin;
        public int startIndex;
        public int activateCount;
        public byte skipRestartIfActive;

        public void Execute()
        {
            int particleIndex = startIndex;
            int slotCount = slots.Length;
            for (int n = 0; n < activateCount; n++)
            {
                if (particleIndex >= slotCount)
                    particleIndex = 0;

                if (skipRestartIfActive != 0 && active[particleIndex] != 0)
                {
                    particleIndex++;
                    continue;
                }

                spawnTimes[particleIndex] = now;
                active[particleIndex] = 1;
                WriteGpuSlot(
                    slots,
                    particleIndex,
                    shaderStartTime,
                    seeds[n],
                    hasMeshSpawn,
                    hasStartSpin,
                    meshBase,
                    spriteBase);
                particleIndex++;
            }
        }
    }

    [BurstCompile]
    public struct PackGpuJob : IJob
    {
        [ReadOnly] public NativeArray<L2FxParticleInstance> slots;
        [ReadOnly] public NativeArray<byte> active;
        [ReadOnly] public NativeArray<Matrix4x4> sourceMatrices;
        public NativeArray<L2FxParticleInstance> packed;
        public NativeArray<Matrix4x4> matrices;
        public NativeArray<int> packedCount;
        public Vector4 ownerWorldPos;
        public int count;

        public void Execute()
        {
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                if (active[i] == 0)
                    continue;

                L2FxParticleInstance slot = slots[i];
                slot.ownerWorldPos = ownerWorldPos;
                packed[n] = slot;
                matrices[n] = sourceMatrices[i];
                n++;
            }

            packedCount[0] = n;
        }
    }

    static void WriteGpuSlot(
        NativeArray<L2FxParticleInstance> slots,
        int slot,
        float shaderStartTime,
        float seed,
        byte hasMeshSpawn,
        byte hasStartSpin,
        uint meshBase,
        uint spriteBase)
    {
        L2AppRand.ResolveGpuInstanceRandBits(
            hasMeshSpawn != 0,
            hasStartSpin != 0,
            meshBase,
            spriteBase,
            slot,
            out float meshSpawnRandBits,
            out float startSpinRandBits,
            out float spriteMotionRandBits,
            out float spriteSpinRandBits);

        L2FxParticleInstance instance = slots[slot];
        instance.startTime = shaderStartTime;
        instance.seed = seed;
        instance.meshSpawnRandBits = meshSpawnRandBits;
        instance.startSpinRandBits = startSpinRandBits;
        instance.spriteMotionRandBits = spriteMotionRandBits;
        instance.spriteSpinRandBits = spriteSpinRandBits;
        slots[slot] = instance;
    }
}
