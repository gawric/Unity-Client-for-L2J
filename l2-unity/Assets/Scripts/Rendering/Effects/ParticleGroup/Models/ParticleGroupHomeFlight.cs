using System;
using UnityEngine;

/// <summary>
/// Per-ParticleGroup overrides for home-projectile flight (path side mirror, speed, height).
/// Applied from composite spawn or set in prefab on each flight anchor group.
/// </summary>
[Serializable]
public struct ParticleGroupHomeFlightProfile
{
    public bool isFlightAnchor;
    [Tooltip("Multiplies composite pathSideOffset. Use -1 for mirrored arc (right side).")]
    public float pathSideOffsetMultiplier;
    [Tooltip("Scales composite pathSideOffset before multiplier.")]
    public float pathSideOffsetScale;
    [Tooltip("Scales composite pathHeightOffset and distance-based peak.")]
    public float pathHeightOffsetScale;
    [Tooltip("Scales composite flight speed.")]
    public float speedScale;

    public static ParticleGroupHomeFlightProfile DefaultAnchor =>
        new ParticleGroupHomeFlightProfile
        {
            isFlightAnchor = true,
            pathSideOffsetMultiplier = 1f,
            pathSideOffsetScale = 1f,
            pathHeightOffsetScale = 1f,
            speedScale = 1f
        };

    public static ParticleGroupHomeFlightProfile MirroredAnchor =>
        new ParticleGroupHomeFlightProfile
        {
            isFlightAnchor = true,
            pathSideOffsetMultiplier = -1f,
            pathSideOffsetScale = 1f,
            pathHeightOffsetScale = 1f,
            speedScale = 1f
        };
}

public class HomeProjectileFlightCoordinator : MonoBehaviour
{
    private BaseEffect _effect;
    private int _activeMovers;
    private bool _destroyRequested;

    public void BeginFlight(BaseEffect effect, int moverCount)
    {
        _effect = effect;
        _activeMovers = Mathf.Max(1, moverCount);
        _destroyRequested = false;
    }

    public void NotifyMoverArrived()
    {
        if (_destroyRequested)
        {
            return;
        }

        _activeMovers--;
        if (_activeMovers > 0 || _effect == null)
        {
            return;
        }

        _destroyRequested = true;
        _effect.DestroyHomeArrivalImmediate();
    }
}
