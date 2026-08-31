using UnityEngine;

/// <summary>
/// Converts leftover Legacy ParticleGroup/ParticleSingle children to V2 stream drivers.
/// Prefabs that already have ParticleGroupV2 are left alone.
/// </summary>
public static class ParticleStreamHijack
{
    public static void Convert(BaseEffect instance)
    {
        if (instance == null)
        {
            return;
        }

        ParticleGroup[] groups = instance.GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            ConvertGroup(groups[i]);
        }

        ParticleSingle[] singles = instance.GetComponentsInChildren<ParticleSingle>(true);
        for (int i = 0; i < singles.Length; i++)
        {
            ConvertSingle(singles[i]);
        }
    }

    static bool HasV2Emitter(Component host)
    {
        return host != null && host.GetComponent<ParticleGroupV2>() != null;
    }

    static void ConvertGroup(ParticleGroup group)
    {
        if (group == null || HasV2Emitter(group))
        {
            return;
        }

        ParticleGroupAuthoring authoring = group.CaptureAuthoring();
        ParticleStreamDriver driver = BindDriver(group.gameObject, authoring);
        driver.OwnerTarget = group.OwnerTarget;
        driver.FollowTarget = group.FollowTarget;
        driver.SurfaceNormal = group.SurfaceNormal;
        group.enabled = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CompositeEffectV2] Hijack group='{group.name}' rate={authoring.countPerSecond}/s max={authoring.maxCount}");
#endif
    }

    static void ConvertSingle(ParticleSingle single)
    {
        if (single == null || HasV2Emitter(single))
        {
            return;
        }

        ParticleGroupAuthoring authoring = single.CaptureAuthoring();
        ParticleStreamDriver driver = BindDriver(single.gameObject, authoring);
        driver.OwnerTarget = single.OwnerTarget;
        driver.FollowTarget = single.FollowTarget;
        driver.SurfaceNormal = single.SurfaceNormal;
        single.enabled = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CompositeEffectV2] Hijack single='{single.name}' max={authoring.maxCount}");
#endif
    }

    static ParticleStreamDriver BindDriver(GameObject host, ParticleGroupAuthoring authoring)
    {
        ParticleStreamDriver driver = host.GetComponent<ParticleStreamDriver>();
        if (driver == null)
        {
            driver = host.AddComponent<ParticleStreamDriver>();
        }

        driver.Bind(authoring);
        return driver;
    }
}
