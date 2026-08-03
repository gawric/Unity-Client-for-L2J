using UnityEngine;

public static class ProjectileFlightTimeCalculator
{
    // -------------------------------------------------------------------------
    // Shared L2 → Unity position scale (VectorUtils.ConvertPosToUnity): /52.5
    // -------------------------------------------------------------------------
    public const float L2UuToUnity = 1f / 52.5f;

    // -------------------------------------------------------------------------
    // ANProjectile (NArrow + skill bolt): dirMul=3000 from rest.
    //   flySec = sqrt(2 * DistUU / 3000), path progress = (t/T)².
    // Speed≈1500 with state=0 is stuck arrow after hit, not cruise velocity.
    // -------------------------------------------------------------------------
    public const float L2ProjectileAccelUuPerSec2 = 3000f;
    public const float L2ProjectileAccelUnityPerSec2 =
        L2ProjectileAccelUuPerSec2 * L2UuToUnity; // ≈ 57.143

    public const float L2SkillProjectileAccelUuPerSec2 = L2ProjectileAccelUuPerSec2;
    public const float L2SkillProjectileAccelUnityPerSec2 = L2ProjectileAccelUnityPerSec2;

    // Diagnostic only (stuck-arrow / old Dist/1500 experiments).
    public const float L2ArrowSpeedUuPerSec = 1500f;
    public const float L2ArrowSpeedUnityPerSec = L2ArrowSpeedUuPerSec * L2UuToUnity;
    public const float L2ArrowSpeedMetersPerSec = L2ArrowSpeedUnityPerSec;
    public const float L2ArrowSpeedIfCmScale = L2ArrowSpeedUuPerSec * 0.01f;

    public const float L2SkillProjectileSpeedUuPerSec = 1000f;
    public const float L2SkillProjectileSpeedUnityPerSec =
        L2SkillProjectileSpeedUuPerSec * L2UuToUnity;

    /// <summary>NArrow / m_u*: t = sqrt(2·Dist / accel3000).</summary>
    public static float CalculateL2ArrowFlightTimeSeconds(float distanceUnity)
    {
        return CalculateL2AccelFlightTimeSeconds(distanceUnity);
    }

    public static float CalculateL2SkillFlightTimeSeconds(float distanceUnity)
    {
        return CalculateL2AccelFlightTimeSeconds(distanceUnity);
    }

    public static float CalculateL2ProjectileFlightTimeSeconds(float distanceUnity)
    {
        return CalculateL2AccelFlightTimeSeconds(distanceUnity);
    }

    public static float CalculateL2AccelFlightTimeSeconds(float distanceUnity)
    {
        if (distanceUnity <= 0f)
        {
            return 0.01f;
        }

        float flySec = Mathf.Sqrt((2f * distanceUnity) / L2ProjectileAccelUnityPerSec2);
        return Mathf.Max(flySec, 0.01f);
    }

    /// <summary>Legacy Dist/1500 for logs only.</summary>
    public static float CalculateL2ArrowFlightTimeIfConstantSpeed(float distanceUnity)
    {
        if (distanceUnity <= 0f)
        {
            return 0.01f;
        }

        return Mathf.Max(distanceUnity / L2ArrowSpeedUnityPerSec, 0.01f);
    }

    public static float CalculateL2AccelJourneyProgress(float elapsedSec, float flyTimeSec)
    {
        float t = Mathf.Clamp01(elapsedSec / Mathf.Max(flyTimeSec, 0.01f));
        return t * t;
    }

    public static float CalculateL2SkillJourneyProgress(float elapsedSec, float flyTimeSec)
    {
        return CalculateL2AccelJourneyProgress(elapsedSec, flyTimeSec);
    }
}
