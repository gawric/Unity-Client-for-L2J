using System;
using UnityEngine;

[Serializable]
public class CompositeProjectileConfig
{
    public ProjectileLaunchMode launchMode = ProjectileLaunchMode.Disabled;
    // If false and launchMode=OnAnimationShoot, part stays hidden until shoot event.
    public bool showBeforeAnimationShoot = true;
    public ProjectileImpactType impactType = ProjectileImpactType.EffectOnly;
    public ProjectileData settingsOverride;
}
