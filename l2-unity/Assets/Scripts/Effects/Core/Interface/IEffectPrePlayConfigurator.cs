using UnityEngine;

/// <summary>
/// Configures a spawned effect instance after Setup and immediately before its
/// EffectPart children start playing.
/// </summary>
public interface IEffectPrePlayConfigurator
{
    void ConfigureBeforePlay(Transform owner);
}
