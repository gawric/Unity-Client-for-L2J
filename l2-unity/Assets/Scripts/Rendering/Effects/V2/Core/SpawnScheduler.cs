using UnityEngine;

/// <summary>
/// Fractional spawn accumulator matching UParticleEmitter::SpawnParticles.
/// </summary>
public struct SpawnScheduler
{
    public float Accumulator;
    public int SpawnedTotal;
    public bool BurstFinished;

    public void Reset()
    {
        Accumulator = 0f;
        SpawnedTotal = 0;
        BurstFinished = false;
    }

    public int ConsumeRate(float deltaTime, int countPerSecond, int remaining)
    {
        if (countPerSecond <= 0 || remaining <= 0 || deltaTime <= 0f)
        {
            return 0;
        }

        Accumulator += countPerSecond * deltaTime;
        int spawned = Mathf.Min(remaining, Mathf.FloorToInt(Accumulator));
        Accumulator -= spawned;
        return spawned;
    }
}
