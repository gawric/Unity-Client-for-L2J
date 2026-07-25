using System.Collections.Generic;
using UnityEngine;

public static class HomeProjectileService
{
    public static bool Launch(
        BaseEffect effect,
        EffectResolveContext context,
        CompositeHomeProjectileConfig config,
        string debugPrefix)
    {
        if (effect == null || context == null || context.CasterTransform == null || config == null)
        {
            return false;
        }

        List<ParticleGroup> anchors = CollectFlightAnchors(effect, config);
        if (anchors.Count == 0)
        {
            return LaunchSingleMover(effect.transform, effect, context, config, null, ParticleGroupHomeFlightProfile.DefaultAnchor, debugPrefix);
        }

        HomeProjectileFlightCoordinator coordinator = effect.gameObject.GetComponent<HomeProjectileFlightCoordinator>();
        if (coordinator == null)
        {
            coordinator = effect.gameObject.AddComponent<HomeProjectileFlightCoordinator>();
        }

        bool anyLaunched = false;
        int launchedCount = 0;
        for (int i = 0; i < anchors.Count; i++)
        {
            ParticleGroup group = anchors[i];
            if (group == null)
            {
                continue;
            }

            group.TryGetHomeFlightProfile(out ParticleGroupHomeFlightProfile profile);
            if (LaunchSingleMover(group.transform, effect, context, config, coordinator, profile, debugPrefix))
            {
                anyLaunched = true;
                launchedCount++;
            }
        }

        coordinator.BeginFlight(effect, launchedCount);

        return anyLaunched;
    }

    private static List<ParticleGroup> CollectFlightAnchors(BaseEffect effect, CompositeHomeProjectileConfig config)
    {
        List<ParticleGroup> anchors = new List<ParticleGroup>();
        ParticleGroup[] groups = effect.GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            ParticleGroup group = groups[i];
            if (group != null && group.IsHomeFlightAnchor)
            {
                anchors.Add(group);
            }
        }

        return anchors;
    }

    private static bool LaunchSingleMover(
        Transform flightTransform,
        BaseEffect effect,
        EffectResolveContext context,
        CompositeHomeProjectileConfig config,
        HomeProjectileFlightCoordinator coordinator,
        ParticleGroupHomeFlightProfile groupProfile,
        string debugPrefix)
    {
        if (flightTransform == null)
        {
            return false;
        }

        HomeProjectileMover mover = flightTransform.GetComponent<HomeProjectileMover>();
        if (mover == null)
        {
            mover = flightTransform.gameObject.AddComponent<HomeProjectileMover>();
        }

        mover.Launch(effect, context, config, coordinator, groupProfile);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{debugPrefix} HomeProjectileService launch object='{flightTransform.name}' " +
            $"from={flightTransform.position} caster='{context.CasterTransform.name}' " +
            $"sideMul={groupProfile.pathSideOffsetMultiplier:F2} mirrorFlight={config.mirrorDualFlight}.");
#endif
        return true;
    }
}

public class HomeProjectileMover : MonoBehaviour
{
    private const string LogTag = "[HOME_PROJECTILE]";
    private const float LogIntervalSeconds = 0.25f;

    private static readonly IEffectAttachmentResolver AttachmentResolver = new DefaultEffectAttachmentResolver();

    private EffectResolveContext _context;
    private EffectAttachmentPoint _homeAttachmentPoint;
    private Vector3 _homeOffset;
    private float _speed;
    private float _acceleration;
    private float _fadeStartDistance;
    private float _fadeOutSeconds;
    private float _arriveDistance;
    private float _maxLifetime;
    private bool _usePathArc;
    private float _pathStartLineFactor;
    private float _pathPeelAlongLine;
    private float _pathApexAlongLine;
    private float _pathPeakHeightAlongLine;
    private float _pathSideOffset;
    private float _pathHeightOffset;
    private float _pathDistanceHeightFactor;
    private float _pathEarlyClimbFactor;
    private float _pathAscentSpeedScale = 1f;
    private float _pathDescentSpeedScale = 1f;
    private bool _rotateToVelocity;
    private bool _destroyOnArrive;
    private BaseEffect _effect;
    private HomeProjectileFlightCoordinator _coordinator;
    private float _pathSideOffsetMultiplier = 1f;
    private float _pathSideOffsetScale = 1f;
    private float _pathHeightOffsetScale = 1f;
    private EffectPart[] _effectParts;
    private Vector3 _startPosition;
    private float _pathDistanceEstimate;
    private float _traveledDistance;
    private float _startedAt;
    private bool _isLaunched;
    private bool _arrivalFadeStarted;
    private bool _pathCompleted;
    private float _fadeCompleteAt;
    private float _nextLogAt;
    private float _lastMoveDelta;

    public void Launch(
        BaseEffect effect,
        EffectResolveContext context,
        CompositeHomeProjectileConfig config,
        HomeProjectileFlightCoordinator coordinator,
        ParticleGroupHomeFlightProfile groupProfile)
    {
        if (context == null || context.CasterTransform == null || config == null)
        {
            enabled = false;
            return;
        }

        _effect = effect;
        _coordinator = coordinator;
        _context = context;
        _homeAttachmentPoint = config.homeAttachmentPoint;
        _homeOffset = config.homeOffset;
        _pathSideOffsetMultiplier = groupProfile.pathSideOffsetMultiplier;
        _pathSideOffsetScale = Mathf.Max(0.01f, groupProfile.pathSideOffsetScale);
        _pathHeightOffsetScale = Mathf.Max(0.01f, groupProfile.pathHeightOffsetScale);
        _speed = Mathf.Max(0.01f, config.speed * Mathf.Max(0.01f, groupProfile.speedScale));
        _acceleration = Mathf.Max(0f, config.acceleration);
        _fadeStartDistance = Mathf.Max(0.01f, config.fadeStartDistance);
        _fadeOutSeconds = Mathf.Max(0.05f, config.fadeOutSeconds);
        _arriveDistance = Mathf.Max(0.01f, config.arriveDistance);
        _maxLifetime = config.maxLifetime;
        _arrivalFadeStarted = false;
        _pathCompleted = false;
        _fadeCompleteAt = -1f;
        _usePathArc = config.usePathArc;
        _pathStartLineFactor = config.pathStartLineFactor;
        _pathPeelAlongLine = config.pathPeelAlongLine;
        _pathApexAlongLine = config.pathApexAlongLine;
        _pathPeakHeightAlongLine = config.pathPeakHeightAlongLine;
        _pathSideOffset = config.pathSideOffset * _pathSideOffsetScale;
        _pathHeightOffset = config.pathHeightOffset * _pathHeightOffsetScale;
        _pathDistanceHeightFactor = config.pathDistanceHeightFactor;
        _pathEarlyClimbFactor = Mathf.Clamp01(config.pathEarlyClimbFactor);
        _pathAscentSpeedScale = Mathf.Max(0.01f, config.pathAscentSpeedScale);
        _pathDescentSpeedScale = Mathf.Max(0.01f, config.pathDescentSpeedScale);
        _rotateToVelocity = config.rotateToVelocity;
        _destroyOnArrive = config.destroyOnArrive;
        _startPosition = transform.position;
        _pathDistanceEstimate = EstimatePathDistance(_startPosition, ResolveHomePosition());
        _traveledDistance = 0f;
        _startedAt = Time.time;
        _isLaunched = true;
        _nextLogAt = Time.time;
        _lastMoveDelta = 0f;

        CacheEffectParts();
        EnsureDetachedFromFollow();
        SyncOwnerWorldPosOverride();
        enabled = true;

        LogLaunchState(target: ResolveHomePosition());
    }

    private void LateUpdate()
    {
        if (!_isLaunched)
        {
            return;
        }

        EnsureDetachedFromFollow();

        if (_context == null || _context.CasterTransform == null)
        {
            LogState("Update abort: caster context lost", transform.position, Vector3.zero, 0f, 0f, 0f);
            Finish("caster_lost");
            return;
        }

        if (_maxLifetime > 0f && Time.time - _startedAt >= _maxLifetime)
        {
            LogState("Update abort: maxLifetime", transform.position, Vector3.zero, 0f, 0f, 0f);
            Finish("max_lifetime");
            return;
        }

        Vector3 target = ResolveHomePosition();
        Vector3 current = transform.position;
        float distanceToHome = Vector3.Distance(current, target);
        float distanceToCasterRoot = Vector3.Distance(current, _context.CasterTransform.position);
        float distance = Mathf.Min(distanceToHome, distanceToCasterRoot);

        LogStateThrottled(current, target, distanceToHome, distanceToCasterRoot, distance);

        if (_arrivalFadeStarted)
        {
            SyncOwnerWorldPosOverride();
            if (Time.time >= _fadeCompleteAt || distance <= _arriveDistance)
            {
                Finish(Time.time >= _fadeCompleteAt ? "fade_timeout" : "fade_arrive_distance");
            }

            return;
        }

        if (distance <= _fadeStartDistance)
        {
            BeginArrivalFade(distance, distanceToHome, distanceToCasterRoot);
            return;
        }

        Vector3 next = ComputeNextPosition(current, target);
        Vector3 velocity = next - current;
        transform.position = next;
        _lastMoveDelta = velocity.magnitude;
        SyncOwnerWorldPosOverride();

        if (_pathCompleted)
        {
            float nextDistanceToHome = Vector3.Distance(next, target);
            float nextDistanceToCasterRoot = Vector3.Distance(next, _context.CasterTransform.position);
            BeginArrivalFade(
                Mathf.Min(nextDistanceToHome, nextDistanceToCasterRoot),
                nextDistanceToHome,
                nextDistanceToCasterRoot);
            return;
        }

        if (_rotateToVelocity && velocity.sqrMagnitude > 0.000001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }
    }

    private void BeginArrivalFade(float distance, float distanceToHome, float distanceToCasterRoot)
    {
        _arrivalFadeStarted = true;
        _fadeCompleteAt = Time.time + _fadeOutSeconds;
        SyncOwnerWorldPosOverride();

        Debug.Log(
            $"{LogTag} BeginArrivalFade object='{name}' distMin={distance:F3} distHome={distanceToHome:F3} " +
            $"distCasterRoot={distanceToCasterRoot:F3} fadeStart<={_fadeStartDistance:F3} arrive<={_arriveDistance:F3} " +
            $"fadeOut={_fadeOutSeconds:F3}s destroyAt={_fadeCompleteAt:F3} effectNull={(_effect == null)} " +
            $"deferDestroy={(_effect != null && _effect.IsDestroyDeferredUntilHomeArrival)}");

        if (_coordinator != null)
        {
            HideAndDestroyFlightObject();
            _coordinator.NotifyMoverArrived();
        }
        else if (_effect != null)
        {
            _effect.DestroyHomeArrivalImmediate();
        }
        else
        {
            Debug.LogWarning($"{LogTag} BeginArrivalFade: BaseEffect missing, Destroy root immediately");
            Destroy(gameObject);
        }

        _isLaunched = false;
        enabled = false;
    }

    private void HideAndDestroyFlightObject()
    {
        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null)
            {
                parts[i].StopPart();
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        ClearOwnerWorldPosOverride();
        Destroy(gameObject);
    }

    private Vector3 ResolveHomePosition()
    {
        if (_context == null || _context.CasterTransform == null)
        {
            return _startPosition;
        }

        if (AttachmentResolver.Resolve(_homeAttachmentPoint, _context, out _, out Vector3 worldPosition))
        {
            if (_homeOffset.sqrMagnitude > 0.000001f && _context.CasterTransform != null)
            {
                worldPosition += _context.CasterTransform.TransformDirection(_homeOffset);
            }

            return worldPosition;
        }

        return _context.CasterTransform.position + _homeOffset;
    }

    private Vector3 ComputeNextPosition(Vector3 current, Vector3 target)
    {
        _speed += _acceleration * Time.deltaTime;
        _pathCompleted = false;

        if (!_usePathArc)
        {
            float linearStep = _speed * Time.deltaTime;
            return Vector3.MoveTowards(current, target, linearStep);
        }

        _pathDistanceEstimate = Mathf.Max(_pathDistanceEstimate, EstimatePathDistance(_startPosition, target));
        float currentPathT = _traveledDistance / Mathf.Max(0.01f, _pathDistanceEstimate);
        float step = _speed * ResolvePathSpeedScale(currentPathT) * Time.deltaTime;
        _traveledDistance += step;
        float pathT = _traveledDistance / Mathf.Max(0.01f, _pathDistanceEstimate);
        if (pathT >= 1f)
        {
            _pathCompleted = true;
            return target;
        }

        ResolvePathControlPoints(_startPosition, target, out Vector3 controlA, out Vector3 controlB);
        return EvaluateCubicBezier(_startPosition, controlA, controlB, target, pathT);
    }

    private float ResolvePathSpeedScale(float pathT)
    {
        float apexT = ResolvePathApexT();
        return pathT < apexT ? _pathAscentSpeedScale : _pathDescentSpeedScale;
    }

    private float ResolvePathApexT()
    {
        return _pathApexAlongLine > 0f
            ? Mathf.Clamp01(_pathApexAlongLine)
            : Mathf.Clamp01(0.46f + _pathStartLineFactor * 0.2f);
    }

    private void ResolvePathControlPoints(Vector3 start, Vector3 target, out Vector3 controlA, out Vector3 controlB)
    {
        Vector3 lateral = ResolvePathLateralDirection(start, target);
        Vector3 flat = target - start;
        flat.y = 0f;
        float horizontalDistance = flat.magnitude;
        float peakHeight = _pathHeightOffset + horizontalDistance * _pathDistanceHeightFactor * _pathHeightOffsetScale;
        float peelT = Mathf.Clamp01(_pathPeelAlongLine);
        float apexT = ResolvePathApexT();
        float peakHeightT = _pathPeakHeightAlongLine > 0f
            ? Mathf.Clamp01(_pathPeakHeightAlongLine)
            : 0.5f;

        // Sharp burst at spawn: quick vertical kick, then peel sideways near the monster.
        // Apex stays on the monster half of the chord; no mid-flight climb toward the player.
        controlA = Vector3.Lerp(start, target, peelT);
        controlA.y = Mathf.Max(controlA.y, start.y + peakHeight * _pathEarlyClimbFactor);

        Vector3 launchAnchor = Vector3.LerpUnclamped(start, target, apexT);
        float sideOffset = _pathSideOffset * _pathSideOffsetMultiplier;
        controlB = launchAnchor + lateral * sideOffset;

        Vector3 peakHeightAnchor = Vector3.Lerp(start, target, peakHeightT);
        float peakBaseY = Mathf.Max(start.y, peakHeightAnchor.y);
        controlB.y = Mathf.Max(controlB.y, peakBaseY + peakHeight);
    }

    /// <summary>
    /// Bulge the arc to caster's left (original vampiric touch enters from the side, not from above).
    /// </summary>
    private Vector3 ResolvePathLateralDirection(Vector3 start, Vector3 target)
    {
        if (_context?.CasterTransform != null)
        {
            Vector3 casterSide = -_context.CasterTransform.right;
            casterSide.y = 0f;
            if (casterSide.sqrMagnitude > 0.000001f)
            {
                return casterSide.normalized;
            }
        }

        Vector3 horizontal = target - start;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude > 0.000001f)
        {
            return Vector3.Cross(Vector3.up, horizontal.normalized);
        }

        return Vector3.left;
    }

    private float EstimatePathDistance(Vector3 start, Vector3 target)
    {
        ResolvePathControlPoints(start, target, out Vector3 controlA, out Vector3 controlB);
        return Vector3.Distance(start, controlA) +
               Vector3.Distance(controlA, controlB) +
               Vector3.Distance(controlB, target);
    }

    private static Vector3 EvaluateCubicBezier(Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float t)
    {
        float inv = 1f - t;
        return inv * inv * inv * start +
               3f * inv * inv * t * controlA +
               3f * inv * t * t * controlB +
               t * t * t * end;
    }

    private void CacheEffectParts()
    {
        _effectParts = GetComponentsInChildren<EffectPart>(true);
    }

    private void EnsureDetachedFromFollow()
    {
        if (transform.parent == null)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning(
            $"{LogTag} DetachFromFollow object='{name}' parent='{transform.parent.name}' " +
            $"worldPosBefore={transform.position}");
#endif
        transform.SetParent(null, true);
    }

    private void SyncOwnerWorldPosOverride()
    {
        if (_effectParts == null)
        {
            return;
        }

        Vector3 worldPosition = transform.position;
        for (int i = 0; i < _effectParts.Length; i++)
        {
            EffectPart part = _effectParts[i];
            if (part != null)
            {
                part.SetOwnerWorldPosOverride(true, worldPosition);
            }
        }
    }

    private void ClearOwnerWorldPosOverride()
    {
        if (_effectParts == null)
        {
            return;
        }

        for (int i = 0; i < _effectParts.Length; i++)
        {
            EffectPart part = _effectParts[i];
            if (part != null)
            {
                part.SetOwnerWorldPosOverride(false, Vector3.zero);
            }
        }
    }

    private void Finish(string reason)
    {
        Debug.Log(
            $"{LogTag} Finish object='{name}' reason={reason} fadeStarted={_arrivalFadeStarted} " +
            $"destroyOnArrive={_destroyOnArrive} elapsed={Time.time - _startedAt:F3}s");

        _isLaunched = false;
        enabled = false;
        ClearOwnerWorldPosOverride();

        if (!_destroyOnArrive)
        {
            return;
        }

        if (_arrivalFadeStarted)
        {
            return;
        }

        if (_effect != null)
        {
            _effect.CompleteHomeArrivalAndDestroy(_fadeOutSeconds);
            return;
        }

        Destroy(gameObject);
    }

    private void LogLaunchState(Vector3 target)
    {
        Debug.Log(
            $"{LogTag} Launch object='{name}' start={_startPosition} homeTarget={target} " +
            $"caster='{(_context.CasterTransform != null ? _context.CasterTransform.name : "null")}' " +
            $"casterPos={(_context.CasterTransform != null ? _context.CasterTransform.position.ToString() : "null")} " +
            $"homePoint={_homeAttachmentPoint} fadeStart={_fadeStartDistance:F3} arrive={_arriveDistance:F3} " +
            $"fadeOut={_fadeOutSeconds:F3}s speed={_speed:F3} arc={_usePathArc} sideMul={_pathSideOffsetMultiplier:F2} " +
            $"parent={(transform.parent != null ? transform.parent.name : "null")} " +
            $"shaderParts={(_effectParts != null ? _effectParts.Length : 0)} enabled={enabled}");
    }

    private void LogStateThrottled(
        Vector3 current,
        Vector3 homeTarget,
        float distanceToHome,
        float distanceToCasterRoot,
        float distanceMin)
    {
        if (Time.time < _nextLogAt)
        {
            return;
        }

        _nextLogAt = Time.time + LogIntervalSeconds;
        LogState("Update", current, homeTarget, distanceToHome, distanceToCasterRoot, distanceMin);
    }

    private void LogState(
        string phase,
        Vector3 current,
        Vector3 homeTarget,
        float distanceToHome,
        float distanceToCasterRoot,
        float distanceMin)
    {
        string casterName = _context?.CasterTransform != null ? _context.CasterTransform.name : "null";
        Vector3 casterPos = _context?.CasterTransform != null ? _context.CasterTransform.position : Vector3.zero;
        float pathT = _pathDistanceEstimate > 0f ? _traveledDistance / _pathDistanceEstimate : 0f;

        Debug.Log(
            $"{LogTag} {phase} object='{name}' t={Time.time:F3} elapsed={Time.time - _startedAt:F3}s " +
            $"effectPos={current} homeTarget={homeTarget} caster='{casterName}' casterPos={casterPos} " +
            $"distHome={distanceToHome:F3} distCasterRoot={distanceToCasterRoot:F3} distMin={distanceMin:F3} " +
            $"fadeStart<={_fadeStartDistance:F3} arrive<={_arriveDistance:F3} fadeStarted={_arrivalFadeStarted} " +
            $"isLaunched={_isLaunched} moverEnabled={enabled} pathT={pathT:F3} moveDelta={_lastMoveDelta:F3} " +
            $"sideMul={_pathSideOffsetMultiplier:F2} parent={(transform.parent != null ? transform.parent.name : "null")}");
    }
}
