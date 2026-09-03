using UnityEngine;

public sealed class HomeProjectilePath
{
    public bool useArc;
    public float startLineFactor;
    public float apexAlongLine;
    public float peakHeightAlongLine;
    public float sideOffset;
    public float heightOffset;
    public float distanceHeightFactor;
    public float earlyClimbFactor;
    public float ascentSpeedScale = 1f;
    public float descentSpeedScale = 1f;
    public float sideOffsetMultiplier = 1f;
    public float heightOffsetScale = 1f;
    public Transform caster;

    public void Configure(CompositeHomeProjectileConfig config, ParticleGroupHomeFlightProfile profile)
    {
        useArc = config.usePathArc;
        startLineFactor = config.pathStartLineFactor;
        apexAlongLine = config.pathApexAlongLine;
        peakHeightAlongLine = config.pathPeakHeightAlongLine;
        sideOffset = config.pathSideOffset * Mathf.Max(0.01f, profile.pathSideOffsetScale);
        heightOffset = config.pathHeightOffset * Mathf.Max(0.01f, profile.pathHeightOffsetScale);
        distanceHeightFactor = config.pathDistanceHeightFactor;
        earlyClimbFactor = Mathf.Clamp01(config.pathEarlyClimbFactor);
        ascentSpeedScale = Mathf.Max(0.01f, config.pathAscentSpeedScale);
        descentSpeedScale = Mathf.Max(0.01f, config.pathDescentSpeedScale);
        sideOffsetMultiplier = profile.pathSideOffsetMultiplier;
        heightOffsetScale = Mathf.Max(0.01f, profile.pathHeightOffsetScale);
    }

    public Vector3 Step(
        Vector3 start,
        Vector3 current,
        Vector3 target,
        float speed,
        float deltaTime,
        ref float traveledDistance,
        ref float pathDistanceEstimate,
        out bool pathCompleted)
    {
        pathCompleted = false;
        if (!useArc)
        {
            return Vector3.MoveTowards(current, target, speed * deltaTime);
        }

        pathDistanceEstimate = Mathf.Max(pathDistanceEstimate, Estimate(start, target));
        float currentPathT = traveledDistance / Mathf.Max(0.01f, pathDistanceEstimate);
        float step = speed * ResolveSpeedScale(currentPathT) * deltaTime;
        traveledDistance += step;
        float pathT = traveledDistance / Mathf.Max(0.01f, pathDistanceEstimate);
        if (pathT >= 1f)
        {
            pathCompleted = true;
            return target;
        }

        ResolveControlPoints(start, target, out Vector3 controlA, out Vector3 controlB);
        return EvaluateCubicBezier(start, controlA, controlB, target, pathT);
    }

    public float Estimate(Vector3 start, Vector3 target)
    {
        ResolveControlPoints(start, target, out Vector3 controlA, out Vector3 controlB);
        return Vector3.Distance(start, controlA) +
               Vector3.Distance(controlA, controlB) +
               Vector3.Distance(controlB, target);
    }

    float ResolveSpeedScale(float pathT)
    {
        return pathT < ResolveApexT() ? ascentSpeedScale : descentSpeedScale;
    }

    float ResolveApexT()
    {
        return apexAlongLine > 0f
            ? Mathf.Clamp01(apexAlongLine)
            : Mathf.Clamp01(0.46f + startLineFactor * 0.2f);
    }

    void ResolveControlPoints(Vector3 start, Vector3 target, out Vector3 controlA, out Vector3 controlB)
    {
        Vector3 lateral = ResolveLateralDirection(start, target);
        Vector3 flat = target - start;
        flat.y = 0f;
        float horizontalDistance = flat.magnitude;
        float peakHeight = heightOffset + horizontalDistance * distanceHeightFactor * heightOffsetScale;
        float apexT = ResolveApexT();
        float peakHeightT = peakHeightAlongLine > 0f
            ? Mathf.Clamp01(peakHeightAlongLine)
            : 0.5f;
        float signedSide = sideOffset * sideOffsetMultiplier;

        controlA = Vector3.LerpUnclamped(start, target, startLineFactor);
        controlA += lateral * signedSide;
        controlA.y = Mathf.Max(controlA.y, start.y + peakHeight * earlyClimbFactor);

        Vector3 launchAnchor = Vector3.LerpUnclamped(start, target, apexT);
        controlB = launchAnchor + lateral * signedSide;

        Vector3 peakHeightAnchor = Vector3.Lerp(start, target, peakHeightT);
        float peakBaseY = Mathf.Max(start.y, peakHeightAnchor.y);
        controlB.y = Mathf.Max(controlB.y, peakBaseY + peakHeight);
    }

    Vector3 ResolveLateralDirection(Vector3 start, Vector3 target)
    {
        if (caster != null)
        {
            Vector3 casterSide = -caster.right;
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

    static Vector3 EvaluateCubicBezier(Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float t)
    {
        float inv = 1f - t;
        return inv * inv * inv * start +
               3f * inv * inv * t * controlA +
               3f * inv * t * t * controlB +
               t * t * t * end;
    }
}
