using UnityEngine;

/// <summary>
/// UC AddLocationFromOtherEmitter: bake other-emitter particle location into the
/// activating GPU slot once at spawn (engine SpawnParticle), not each frame.
/// </summary>
public interface IParticleSpawnLocationAddProvider
{
    bool TryGetSpawnLocationAddUe(EffectPart tailEmitter, float spawnTime, out Vector4 addUe);
}
