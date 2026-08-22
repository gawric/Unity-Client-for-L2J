using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// L2 canvas pixel lock with hysteresis (shared by world and lobby nameplates).
/// </summary>
public sealed class NameplatePixelSnap
{
    private readonly Dictionary<int, Vector2> _snapPixels;

    public float HysteresisPx { get; set; } = 0.75f;

    public NameplatePixelSnap(int capacity = 64)
    {
        _snapPixels = new Dictionary<int, Vector2>(capacity);
    }

    public float Snap(int id, float raw, bool isX, float distanceAlongView)
    {
        float hold = Mathf.Max(0.51f, HysteresisPx);
        if (distanceAlongView > 0.01f && distanceAlongView < 2.5f)
        {
            hold = Mathf.Max(hold, 1.4f / Mathf.Max(0.45f, distanceAlongView));
        }

        float candidate = Mathf.Round(raw);

        if (!_snapPixels.TryGetValue(id, out Vector2 last))
        {
            last = new Vector2(candidate, candidate);
            _snapPixels[id] = last;
            return candidate;
        }

        float prev = isX ? last.x : last.y;
        float snapped = Mathf.Abs(raw - prev) < hold ? prev : candidate;
        if (isX)
        {
            last.x = snapped;
        }
        else
        {
            last.y = snapped;
        }

        _snapPixels[id] = last;
        return snapped;
    }

    public void Clear(int id)
    {
        _snapPixels.Remove(id);
    }

    public void ClearAll()
    {
        _snapPixels.Clear();
    }
}
