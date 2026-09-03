using System;
using UnityEngine;

[Serializable]
public class CompositeHomeProjectileConfig
{
    public ProjectileLaunchMode launchMode = ProjectileLaunchMode.Disabled;
    public float speed = 4.5f;
    public float acceleration = 0f;
    // 0 = no cap. Live m_u003_b: dirMul/FNMover+0x04 = 450 UU/s = 8.57 m/s.
    public float maxSpeed = 0f;
    // When distance to caster (home point or root) <= this, start fade-out.
    public float fadeStartDistance = 0.5f;
    public float fadeOutSeconds = 0.35f;
    // Optional hard finish if still within this range after fade started.
    public float arriveDistance = 0.2f;
    // 0 = fly until arrival at caster; >0 = safety cap only.
    public float maxLifetime = 0f;
    public EffectAttachmentPoint homeAttachmentPoint = EffectAttachmentPoint.CasterCenter;
    public Vector3 homeOffset = new Vector3(0f, 0.1f, 0f);
    public bool usePathArc = true;
    [Tooltip("Legacy apex shift. Used when pathApexAlongLine <= 0 (apex = 0.46 + factor*0.2).")]
    public float pathStartLineFactor = -0.15f;
    [Tooltip("Along-line peel from spawn (0=monster, 1=player).")]
    [Range(0f, 1f)]
    public float pathPeelAlongLine = 0.16f;
    [Tooltip("Along-line apex of side arc (0=monster, 1=player). 0 = use pathStartLineFactor legacy.")]
    [Range(0f, 1f)]
    public float pathApexAlongLine = 0f;
    [Tooltip("Along-line height reference for arc peak (0=monster, 1=player). 0 = midpoint (0.5).")]
    [Range(0f, 1f)]
    public float pathPeakHeightAlongLine = 0f;
    // Lateral bulge toward caster's left (original side arc).
    public float pathSideOffset = 1.25f;
    // Extra height above chord midpoint (climb then descend toward caster).
    public float pathHeightOffset = 0.44f;
    [Tooltip("Extra peak height per meter of horizontal travel monster→caster.")]
    public float pathDistanceHeightFactor = 0.112f;
    [Tooltip("Share of peak height applied at peel (early climb while spreading sideways).")]
    [Range(0f, 1f)]
    public float pathEarlyClimbFactor = 0.2f;
    [Tooltip("Speed multiplier before the orb reaches the arc apex.")]
    public float pathAscentSpeedScale = 1f;
    [Tooltip("Speed multiplier after the orb reaches the arc apex.")]
    public float pathDescentSpeedScale = 1f;
    public bool rotateToVelocity = true;
    public bool destroyOnArrive = true;
    [Tooltip("Spawn mirrored duplicate of each home flight anchor ParticleGroup (original m_u003_b x2).")]
    public bool mirrorDualFlight = false;

    public bool IsEnabled => launchMode != ProjectileLaunchMode.Disabled;

    public bool ShouldLaunchOnShoot => launchMode == ProjectileLaunchMode.OnAnimationShoot;

    public bool ShouldLaunchImmediately => launchMode == ProjectileLaunchMode.Immediate;
}
