using UnityEngine;

/// <summary>
/// Thin V2 composite part. Shared spawn/follow/scale. Specialized subclasses own extra behavior.
/// </summary>
[System.Serializable]
public abstract class CompositePart : ISerializationCallbackReceiver
{
    public string name;
    public BaseEffect prefab;
    public float scale = 1f;
    public CompositePartSpawnTiming spawnTiming = CompositePartSpawnTiming.Immediate;
    public float spawnDelaySeconds;
    [Tooltip("Where this part appears. Spawn Timing is when.")]
    public EffectAttachmentPoint spawnAttachmentPoint;
    public string boneName;
    public bool follow = true;
    public bool inheritRotation;
    // Nested SerializeReference inside a SerializeReference part orphans RefIds
    // (missing 1008/1009) on prefab save. Runtime uses spawnAttachmentPoint.
    // Kept only to migrate old prefabs in OnAfterDeserialize.
    [HideInInspector]
    [SerializeReference] public EffectPlacement placement;

    public bool IsSpawnable => prefab != null;

    public virtual bool WantsAnimationShoot =>
        spawnTiming == CompositePartSpawnTiming.OnAnimationShoot;

    public virtual bool WantsHitCollider =>
        spawnTiming == CompositePartSpawnTiming.OnHitCollider;

    public virtual bool IsLaunchedProjectile => false;

    /// <summary>
    /// Independent burst (skill _ta): plays out after the composite GO is gone.
    /// Generic weapon/bow hit FX are separate; this is the skill overlay.
    /// </summary>
    public virtual bool OutlivesComposite => false;

    /// <summary>
    /// Stationary cast FX stretch GO life to server HitTime. Shot / impact do not.
    /// </summary>
    public virtual bool UsesCastHitLifetime =>
        spawnTiming != CompositePartSpawnTiming.OnHitCollider;

    /// <summary>
    /// Part-owned playback policy. ParticleGroupV2 only receives a window + stop mode.
    /// </summary>
    public virtual void ConfigurePlayback(
        BaseEffect instance,
        EffectSettings settings,
        MagicCastData castData,
        EffectResolveContext context)
    {
    }

    public virtual string Describe()
    {
        string place = spawnAttachmentPoint.ToString();
        return GetType().Name + " " + place + " / " + spawnTiming +
               " follow=" + follow + " scale=" + scale.ToString("0.###");
    }

    public virtual void OnAfterSpawn(BaseEffect instance, EffectResolveContext context)
    {
    }

    public virtual void OnAnimationShoot(BaseEffect instance, EffectResolveContext context)
    {
    }

    public virtual bool TryResolveSpawn(
        IEffectAttachmentResolver resolver,
        EffectResolveContext context,
        out Transform followTransform,
        out Vector3 worldPosition)
    {
        if (!string.IsNullOrEmpty(boneName))
        {
            var bonePlacement = new BonePlacement { boneName = boneName };
            if (bonePlacement.TryResolve(resolver, context, out followTransform, out worldPosition))
            {
                return true;
            }
        }

        followTransform = null;
        worldPosition = Vector3.zero;
        if (resolver != null &&
            resolver.Resolve(spawnAttachmentPoint, context, out followTransform, out worldPosition))
        {
            return true;
        }

        return placement != null &&
               placement.TryResolve(resolver, context, out followTransform, out worldPosition);
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if (placement is BonePlacement bone && !string.IsNullOrEmpty(bone.boneName))
        {
            boneName = bone.boneName;
        }

        if (spawnAttachmentPoint == EffectAttachmentPoint.CasterRoot && placement != null)
        {
            spawnAttachmentPoint = EffectPlacement.ToAttachment(placement);
        }
    }
}

[System.Serializable]
public sealed class StationaryPart : CompositePart
{
    public override void ConfigurePlayback(
        BaseEffect instance,
        EffectSettings settings,
        MagicCastData castData,
        EffectResolveContext context)
    {
        if (instance == null)
        {
            return;
        }

        float window = EffectCastDurationResolver.Resolve(
            0.01f,
            false,
            settings,
            castData,
            out _,
            out _);
        ParticleEmitterV2.SetEmissionWindow(instance, window, EmitterStopMode.Drain, skipFixedDuration: true);
    }
}

[System.Serializable]
public class IndependentEffectPart : CompositePart
{
    public override bool OutlivesComposite => true;

    public override bool UsesCastHitLifetime => false;

    public override void ConfigurePlayback(
        BaseEffect instance,
        EffectSettings settings,
        MagicCastData castData,
        EffectResolveContext context)
    {
        if (!follow)
        {
            DetachFromContainer(instance);
        }

        L2Particle particle = instance as L2Particle;
        if (particle != null && ParticleEmitterV2.InChildren(particle).Length > 0)
        {
            particle.BindLifetimeToAuthoredStreams();
            return;
        }

        float life = ParticleEmitterV2.MaxAuthoredDuration(instance, 0.01f);
        if (settings != null)
        {
            settings.defaultLifeTime = life;
            settings.hideTime = 0f;
        }
    }

    public override void OnAfterSpawn(BaseEffect instance, EffectResolveContext context)
    {
        if (!follow)
        {
            DetachFromContainer(instance);
        }
    }

    static void DetachFromContainer(BaseEffect instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.transform.SetParent(null, true);
    }
}

[System.Serializable]
public sealed class ImpactPart : IndependentEffectPart
{
}

[System.Serializable]
public sealed class ShotProjectilePart : CompositePart
{
    public ProjectileLaunchMode launchMode = ProjectileLaunchMode.OnAnimationShoot;
    public bool showBeforeAnimationShoot;
    public ProjectileImpactType impactType = ProjectileImpactType.EffectOnly;
    public float speed;

    public override bool WantsAnimationShoot =>
        spawnTiming == CompositePartSpawnTiming.OnAnimationShoot ||
        launchMode == ProjectileLaunchMode.OnAnimationShoot;

    public override bool IsLaunchedProjectile =>
        launchMode == ProjectileLaunchMode.Immediate ||
        launchMode == ProjectileLaunchMode.OnAnimationShoot;

    public override bool UsesCastHitLifetime => false;

    public override void ConfigurePlayback(
        BaseEffect instance,
        EffectSettings settings,
        MagicCastData castData,
        EffectResolveContext context)
    {
        // Clock is ProjectileManager flytime (ProjectileFlightTimeCalculator at launch
        // distance), not castData.FlightTime / EffectCastDurationResolver (those only
        // shift the animation shoot: HitTime − FlightTime).
        float flytime = ResolveLaunchFlytimeSeconds(instance, context);
        if (settings != null)
        {
            settings.defaultLifeTime = flytime;
            settings.hideTime = 0f;
        }

        L2Particle particle = instance as L2Particle;
        if (particle != null)
        {
            particle.BindLifetimeToProjectile();
        }

        if (instance == null)
        {
            return;
        }

        ParticleEmitterV2.SetEmissionWindow(instance, flytime, EmitterStopMode.Drain, skipFixedDuration: false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            "[ShotProjectilePart] '" + name + "' emit=ProjectileFlightTimeCalculator flytime=" +
            flytime.ToString("0.###") + "s hideTime=0 drain-until-destroy emitters=" +
            ParticleEmitterV2.InChildren(instance).Length);
#endif
    }

    static float ResolveLaunchFlytimeSeconds(BaseEffect instance, EffectResolveContext context)
    {
        if (instance == null || context == null || context.TargetTransform == null)
        {
            return 2f;
        }

        Vector3 start = instance.transform.position;
        Vector3 aim = VectorUtils.GetCollision(start, context.TargetTransform);
        float distance = Vector3.Distance(start, aim);
        return ProjectileFlightTimeCalculator.CalculateL2AccelFlightTimeSeconds(distance);
    }

    public override string Describe()
    {
        return base.Describe() + " launch=" + launchMode;
    }

    public override void OnAfterSpawn(BaseEffect instance, EffectResolveContext context)
    {
        if (ShouldHideUntilShoot())
        {
            CompositeProjectileLaunchHelper.SetPartVisualsVisible(instance != null ? instance.transform : null, false);
        }
    }

    public override void OnAnimationShoot(BaseEffect instance, EffectResolveContext context)
    {
        if (instance == null)
        {
            return;
        }

        if (ShouldHideUntilShoot())
        {
            L2Particle particle = instance as L2Particle;
            if (particle != null)
            {
                particle.ResetTimer();
            }

            CompositeProjectileLaunchHelper.SetPartVisualsVisible(instance.transform, true);
        }
    }

    public bool ShouldLaunchImmediately => launchMode == ProjectileLaunchMode.Immediate;

    public bool ShouldLaunchOnShoot => launchMode == ProjectileLaunchMode.OnAnimationShoot;

    bool ShouldHideUntilShoot()
    {
        if (spawnTiming == CompositePartSpawnTiming.OnHitCollider ||
            spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
        {
            return false;
        }

        return launchMode == ProjectileLaunchMode.OnAnimationShoot && !showBeforeAnimationShoot;
    }

    public bool TryLaunch(BaseEffect instance, EffectResolveContext context)
    {
        if (instance == null || context == null || context.TargetTransform == null || ProjectileManager.Instance == null)
        {
            return false;
        }

        Transform projectileTransform = instance.transform;
        projectileTransform.SetParent(null, true);
        Vector3 startPos = projectileTransform.position;
        ProjectileData settings = new ProjectileData
        {
            prefab = instance.gameObject,
            transform = projectileTransform,
            startPosition = startPos,
            targetTransform = context.TargetTransform,
            impactType = impactType,
            speed = speed > 0.01f ? speed : 10f
        };
        ProjectileManager.Instance.LaunchProjectile(
            instance.gameObject,
            startPos,
            context.TargetTransform,
            settings);
        return true;
    }
}

[System.Serializable]
public sealed class HomeFlightPart : CompositePart
{
    public ProjectileLaunchMode launchMode = ProjectileLaunchMode.OnAnimationShoot;
    // Default outbound home-flight speed. m_u003_b overwrites this from the
    // live lock in L2EffectGeneratorAssetOverrides, not from UC Speed.
    public float speed = 5f;
    public float acceleration;
    [HideInInspector] public float maxSpeed;
    [HideInInspector] public float fadeStartDistance = 0.5f;
    [HideInInspector] public float fadeOutSeconds = 0.35f;
    [HideInInspector] public float arriveDistance = 0.2f;
    [HideInInspector] public float maxLifetime;
    public EffectAttachmentPoint homeAttachmentPoint = EffectAttachmentPoint.CasterCenter;
    public Vector3 homeOffset = new Vector3(0f, 0.1f, 0f);
    public bool usePathArc = true;
    [HideInInspector] public float pathStartLineFactor = -0.15f;
    [HideInInspector] public float pathPeelAlongLine = 0.16f;
    [HideInInspector] public float pathApexAlongLine;
    [HideInInspector] public float pathPeakHeightAlongLine;
    [HideInInspector] public float pathSideOffset = 1.25f;
    [HideInInspector] public float pathHeightOffset = 0.44f;
    [HideInInspector] public float pathDistanceHeightFactor = 0.112f;
    [HideInInspector] public float pathEarlyClimbFactor = 0.2f;
    [HideInInspector] public float pathAscentSpeedScale = 1f;
    [HideInInspector] public float pathDescentSpeedScale = 1f;
    public bool rotateToVelocity = true;
    public bool destroyOnArrive = true;
    [HideInInspector] public bool mirrorDualFlight;

    public override bool WantsAnimationShoot =>
        spawnTiming == CompositePartSpawnTiming.OnAnimationShoot ||
        launchMode == ProjectileLaunchMode.OnAnimationShoot;

    public override bool IsLaunchedProjectile =>
        launchMode == ProjectileLaunchMode.Immediate ||
        launchMode == ProjectileLaunchMode.OnAnimationShoot;

    public override bool UsesCastHitLifetime => false;

    public override bool OutlivesComposite => true;

    public override void ConfigurePlayback(
        BaseEffect instance,
        EffectSettings settings,
        MagicCastData castData,
        EffectResolveContext context)
    {
        // UC SpriteEmitter5: Lifetime=0.01 + 3000 pps + MaxParticles=3. Live
        // recycles those slots for the whole FNMover trip. Fixed-duration
        // burst would spawn once and vanish in a frame (SpriteEmitter2's
        // 0.33s trail still reads; the core does not).
        ParticleEmitterV2.BindHostOwnedEmission(instance);
    }

    public bool ShouldLaunchImmediately => launchMode == ProjectileLaunchMode.Immediate;

    public bool ShouldLaunchOnShoot => launchMode == ProjectileLaunchMode.OnAnimationShoot;

    public override string Describe()
    {
        return base.Describe() + " homeFlight launch=" + launchMode +
               " speed=" + speed.ToString("0.###") +
               " accel=" + acceleration.ToString("0.##") +
               " max=" + maxSpeed.ToString("0.##") +
               " side=" + pathSideOffset.ToString("0.##") +
               " height=" + pathHeightOffset.ToString("0.##") +
               " dual=" + (mirrorDualFlight ? "1" : "0");
    }

    public override void OnAfterSpawn(BaseEffect instance, EffectResolveContext context)
    {
        if (instance == null)
        {
            return;
        }

        instance.PrepareDestroyOnHomeArrival();
        EffectPart[] parts = instance.GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null)
            {
                parts[i].SetOwnerWorldPosOverride(true, instance.transform.position);
            }
        }

        if (ShouldHideUntilShoot())
        {
            CompositeProjectileLaunchHelper.SetPartVisualsVisible(instance.transform, false);
        }
    }

    public override void OnAnimationShoot(BaseEffect instance, EffectResolveContext context)
    {
        if (instance == null)
        {
            return;
        }

        if (ShouldHideUntilShoot())
        {
            L2Particle particle = instance as L2Particle;
            if (particle != null)
            {
                particle.ResetTimer();
            }

            CompositeProjectileLaunchHelper.SetPartVisualsVisible(instance.transform, true);
        }
    }

    public bool TryLaunch(BaseEffect instance, EffectResolveContext context, IHomeProjectileService homeProjectiles)
    {
        if (instance == null || context == null || context.CasterTransform == null || homeProjectiles == null)
        {
            return false;
        }

        homeProjectiles.EnsureDualFlightRoots(instance, mirrorDualFlight);
        return homeProjectiles.Launch(instance, context, BuildConfig());
    }

    CompositeHomeProjectileConfig BuildConfig()
    {
        return new CompositeHomeProjectileConfig
        {
            launchMode = launchMode,
            speed = speed,
            acceleration = acceleration,
            maxSpeed = maxSpeed,
            fadeStartDistance = fadeStartDistance,
            fadeOutSeconds = fadeOutSeconds,
            arriveDistance = arriveDistance,
            maxLifetime = maxLifetime,
            homeAttachmentPoint = homeAttachmentPoint,
            homeOffset = homeOffset,
            usePathArc = usePathArc,
            pathStartLineFactor = pathStartLineFactor,
            pathPeelAlongLine = pathPeelAlongLine,
            pathApexAlongLine = pathApexAlongLine,
            pathPeakHeightAlongLine = pathPeakHeightAlongLine,
            pathSideOffset = pathSideOffset,
            pathHeightOffset = pathHeightOffset,
            pathDistanceHeightFactor = pathDistanceHeightFactor,
            pathEarlyClimbFactor = pathEarlyClimbFactor,
            pathAscentSpeedScale = pathAscentSpeedScale,
            pathDescentSpeedScale = pathDescentSpeedScale,
            rotateToVelocity = rotateToVelocity,
            destroyOnArrive = destroyOnArrive,
            mirrorDualFlight = mirrorDualFlight
        };
    }

    bool ShouldHideUntilShoot()
    {
        if (spawnTiming == CompositePartSpawnTiming.OnHitCollider ||
            spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
        {
            return false;
        }

        return launchMode == ProjectileLaunchMode.OnAnimationShoot;
    }
}
