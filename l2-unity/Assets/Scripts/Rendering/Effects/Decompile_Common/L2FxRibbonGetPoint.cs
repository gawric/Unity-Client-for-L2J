using UnityEngine;

/// <summary>
/// C# mirror of Decompile_Common/L2FxRibbonGetPoint.hlsl.
/// URibbonEmitter::GetNewRibbonPoint — live-verified Interlude RibbonSet (CoordSys==1).
/// Keep in sync with the HLSL module; do not fork formulas in effect scripts.
/// </summary>
public static class L2FxRibbonGetPoint
{
    public struct Edges
    {
        public Vector3 A2;
        public Vector3 A3;
        public float A4;
    }

    /// <summary>
    /// CoordSys == 1: a3 = Pos - N*S*R ; a2 = Pos + N*S*(1-R) ; |a2-a3| == S.
    /// </summary>
    public static Edges GetPointCoordSys1(Vector3 pos, Vector3 normalUnit, float scaleRatio, float edgeRatio)
    {
        Vector3 n = normalUnit;
        float nLenSq = n.sqrMagnitude;
        if (nLenSq > 1e-12f)
        {
            n *= 1f / Mathf.Sqrt(nLenSq);
        }
        else
        {
            n = Vector3.right;
        }

        float s = scaleRatio;
        float r = edgeRatio;
        Edges e;
        e.A3 = pos - n * (s * r);
        e.A2 = pos + n * (s * (1f - r));
        e.A4 = s;
        return e;
    }

    /// <summary>
    /// Blade ends as ribbon cross-section (Unity Sword_Tip / Sword_Base).
    /// Matches live |a2-a3| == blade length. a2 = tip, a3 = base.
    /// </summary>
    public static Edges GetPointFromBladeEnds(Vector3 tipWorld, Vector3 baseWorld, float edgeRatio = 0.5f)
    {
        Vector3 tip = tipWorld;
        Vector3 hilt = baseWorld;
        Vector3 axis = tip - hilt;
        float len = axis.magnitude;
        if (len < 1e-6f)
        {
            Edges degenerated;
            degenerated.A2 = tip;
            degenerated.A3 = hilt;
            degenerated.A4 = 0f;
            return degenerated;
        }

        if (Mathf.Abs(edgeRatio - 0.5f) < 1e-4f)
        {
            Edges full;
            full.A2 = tip;
            full.A3 = hilt;
            full.A4 = len;
            return full;
        }

        Vector3 mid = (tip + hilt) * 0.5f;
        Vector3 n = axis / len;
        return GetPointCoordSys1(mid, n, len, edgeRatio);
    }

    /// <summary>
    /// World tip/hilt from weapon mesh local bounds along the longest axis,
    /// transformed through the MeshFilter (or SkinnedMeshRenderer) transform.
    /// SwordSetup often parents Sword_Tip/Base to the weapon root while writing
    /// mesh-local bounds — that under-spans the blade when the mesh is a child.
    /// </summary>
    public static bool TryGetMeshBladeEnds(Transform searchRoot, out Vector3 tipWorld, out Vector3 hiltWorld)
    {
        tipWorld = default;
        hiltWorld = default;
        if (searchRoot == null)
        {
            return false;
        }

        Transform meshXf = null;
        Bounds localBounds = default;
        bool found = false;

        MeshFilter mf = searchRoot.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            meshXf = mf.transform;
            localBounds = mf.sharedMesh.bounds;
            found = true;
        }
        else
        {
            SkinnedMeshRenderer smr = searchRoot.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                meshXf = smr.transform;
                localBounds = smr.sharedMesh.bounds;
                found = true;
            }
        }

        if (!found || meshXf == null)
        {
            return false;
        }

        Vector3 size = localBounds.size;
        Vector3 minLocal;
        Vector3 maxLocal;
        if (size.x >= size.y && size.x >= size.z)
        {
            minLocal = new Vector3(localBounds.min.x, localBounds.center.y, localBounds.center.z);
            maxLocal = new Vector3(localBounds.max.x, localBounds.center.y, localBounds.center.z);
        }
        else if (size.z >= size.x && size.z >= size.y)
        {
            minLocal = new Vector3(localBounds.center.x, localBounds.center.y, localBounds.min.z);
            maxLocal = new Vector3(localBounds.center.x, localBounds.center.y, localBounds.max.z);
        }
        else
        {
            minLocal = new Vector3(localBounds.center.x, localBounds.min.y, localBounds.center.z);
            maxLocal = new Vector3(localBounds.center.x, localBounds.max.y, localBounds.center.z);
        }

        // Convention: max = tip, min = hilt (matches SwordSetup max→tip naming intent).
        tipWorld = meshXf.TransformPoint(maxLocal);
        hiltWorld = meshXf.TransformPoint(minLocal);
        return (tipWorld - hiltWorld).sqrMagnitude > 1e-10f;
    }

    /// <summary>
    /// Keep a2/a3 from flipping end-for-end between samples (common when
    /// tip/base markers disagree with mesh-bounds orientation). Live ribbon
    /// width axis stays continuous; a flipped axis tears the strip.
    /// </summary>
    public static Edges StabilizeEdgePolarity(in Edges current, in Edges reference)
    {
        Vector3 curAxis = current.A2 - current.A3;
        Vector3 refAxis = reference.A2 - reference.A3;
        if (curAxis.sqrMagnitude < 1e-12f || refAxis.sqrMagnitude < 1e-12f)
        {
            return current;
        }

        if (Vector3.Dot(curAxis, refAxis) >= 0f)
        {
            return current;
        }

        Edges swapped;
        swapped.A2 = current.A3;
        swapped.A3 = current.A2;
        swapped.A4 = current.A4;
        return swapped;
    }

    /// <summary>
    /// Prefer proximity matching over axis-dot polarity (Dot&lt;0 false-swaps on &gt;90° swings).
    /// </summary>
    public static Edges StabilizeEdgeEndsByProximity(in Edges current, in Edges reference)
    {
        float keep =
            Vector3.Distance(current.A2, reference.A2) +
            Vector3.Distance(current.A3, reference.A3);
        float swap =
            Vector3.Distance(current.A2, reference.A3) +
            Vector3.Distance(current.A3, reference.A2);

        if (swap + 1e-5f < keep)
        {
            Edges swapped;
            swapped.A2 = current.A3;
            swapped.A3 = current.A2;
            swapped.A4 = current.A4;
            return swapped;
        }

        return current;
    }

    public static Vector3 EdgeMid(Vector3 a2, Vector3 a3)
    {
        return (a2 + a3) * 0.5f;
    }

    /// <summary>
    /// Linear sheet between two cross-sections (a2/a3/a4).
    /// Prefer <see cref="LerpEdgesArc"/> for swing trails — linear chords look
    /// "скошенными" on large per-frame tip motion.
    /// </summary>
    public static Edges LerpEdges(in Edges from, in Edges to, float t)
    {
        Edges e;
        e.A2 = Vector3.Lerp(from.A2, to.A2, t);
        e.A3 = Vector3.Lerp(from.A3, to.A3, t);
        e.A4 = Mathf.Lerp(from.A4, to.A4, t);
        return e;
    }

    /// <summary>
    /// Arc sheet: Slerp tip and base around <paramref name="pivot"/> (weapon/hand).
    /// Unity only has one animated pose per frame; linear Lerp of endpoints
    /// draws a chord. Pivot-Slerp approximates the real swing arc so a top-down
    /// cut reads as a curve instead of a flat cut.
    /// </summary>
    public static Edges LerpEdgesArc(in Edges from, in Edges to, float t, Vector3 pivot)
    {
        t = Mathf.Clamp01(t);
        Vector3 a2 = PivotSlerp(from.A2, to.A2, pivot, t);
        Vector3 a3 = PivotSlerp(from.A3, to.A3, pivot, t);
        float width = Mathf.Lerp(
            Mathf.Max(1e-6f, from.A4),
            Mathf.Max(1e-6f, to.A4),
            t);

        Vector3 mid = EdgeMid(a2, a3);
        Vector3 axis = a2 - a3;
        float axisLen = axis.magnitude;
        if (axisLen > 1e-6f)
        {
            axis /= axisLen;
            // Keep CS1-style R=0.5 cross-section at interpolated ScaleRatio.
            a2 = mid + axis * (width * 0.5f);
            a3 = mid - axis * (width * 0.5f);
        }

        Edges e;
        e.A2 = a2;
        e.A3 = a3;
        e.A4 = width;
        return e;
    }

    static Vector3 PivotSlerp(Vector3 from, Vector3 to, Vector3 pivot, float t)
    {
        Vector3 a = from - pivot;
        Vector3 b = to - pivot;
        float ra = a.magnitude;
        float rb = b.magnitude;
        if (ra < 1e-5f || rb < 1e-5f)
        {
            return Vector3.Lerp(from, to, t);
        }

        Vector3 dir = Vector3.Slerp(a / ra, b / rb, t);
        return pivot + dir * Mathf.Lerp(ra, rb, t);
    }

    /// <summary>
    /// Sheet count from the longest of tip/base/mid travel (tip often moves
    /// farther than mid on a swing).
    /// </summary>
    public static int CountInterpSheetsForEdges(
        in Edges from,
        in Edges to,
        float maxSegmentMeters,
        int maxSheets)
    {
        float tipD = Vector3.Distance(from.A2, to.A2);
        float baseD = Vector3.Distance(from.A3, to.A3);
        float midD = Vector3.Distance(EdgeMid(from.A2, from.A3), EdgeMid(to.A2, to.A3));
        float path = Mathf.Max(tipD, Mathf.Max(baseD, midD));
        return CountInterpSheets(path, maxSegmentMeters, maxSheets);
    }

    /// <summary>
    /// How many intermediate sheets to insert so consecutive midpoints are
    /// at most <paramref name="maxSegmentMeters"/> apart.
    /// </summary>
    public static int CountInterpSheets(float midDistanceMeters, float maxSegmentMeters, int maxSheets)
    {
        if (midDistanceMeters <= 1e-6f || maxSegmentMeters <= 1e-6f)
        {
            return 0;
        }

        int segments = Mathf.CeilToInt(midDistanceMeters / maxSegmentMeters);
        int sheets = Mathf.Max(0, segments - 1);
        return Mathf.Min(sheets, Mathf.Max(0, maxSheets));
    }
}
