using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dev-only: logs a second Play/spawn of the same effect with both stacks.
/// Filter Console by <c>[FX_DOUBLE]</c>.
/// </summary>
public static class EffectDoublePlayLog
{
    public const string Tag = "[FX_DOUBLE]";
    const float ManagerWindowSec = 0.6f;

    struct RecentPlay
    {
        public float time;
        public int frame;
        public string stack;
        public string prefab;
    }

    static readonly Dictionary<int, RecentPlay> RecentByEffectId = new Dictionary<int, RecentPlay>(32);

    public static string CaptureStack() => Environment.StackTrace;

    public static void Repeat(string channel, UnityEngine.Object owner, float firstAt, string firstStack)
    {
        Debug.LogWarning(
            $"{Tag} REPEAT {channel} obj='{Name(owner)}' " +
            $"firstAt={firstAt:F3} now={Time.time:F3} dt={Time.time - firstAt:F3}s frame={Time.frameCount}\n" +
            $"--- first ---\n{NullStack(firstStack)}\n--- this ---\n{Environment.StackTrace}");
    }

    public static void Note(string channel, UnityEngine.Object owner, string detail)
    {
        Debug.LogWarning(
            $"{Tag} {channel} obj='{Name(owner)}' {detail} " +
            $"frame={Time.frameCount} t={Time.time:F3}\n{Environment.StackTrace}");
    }

    public static void TrackManagerPlay(int effectId, string prefabName)
    {
        float now = Time.time;
        if (RecentByEffectId.TryGetValue(effectId, out RecentPlay prev) &&
            now - prev.time <= ManagerWindowSec)
        {
            Debug.LogWarning(
                $"{Tag} REPEAT EffectManager id={effectId} prefab='{prefabName}' " +
                $"dt={now - prev.time:F3}s firstFrame={prev.frame} thisFrame={Time.frameCount} " +
                $"firstPrefab='{prev.prefab}'\n" +
                $"--- first ---\n{NullStack(prev.stack)}\n--- this ---\n{Environment.StackTrace}");
        }

        RecentByEffectId[effectId] = new RecentPlay
        {
            time = now,
            frame = Time.frameCount,
            stack = Environment.StackTrace,
            prefab = prefabName
        };
    }

    static string Name(UnityEngine.Object owner) => owner != null ? owner.name : "null";

    static string NullStack(string stack) => string.IsNullOrEmpty(stack) ? "(not stored)" : stack;
}
