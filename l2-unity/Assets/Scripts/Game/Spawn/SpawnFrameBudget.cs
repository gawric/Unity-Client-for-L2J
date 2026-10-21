using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Caps expensive CharInfo/NpcInfo Instantiates per render frame so EventProcessor
/// does not drain a 500-spawn hitch into the next batch.
/// Move/Attack/StopMove are not counted. Original Engine.dll has no cap; spawn there is cheap.
/// </summary>
public static class SpawnFrameBudget
{
    public const int MaxSpawnsPerFrame = 6;

    static readonly HashSet<int> Cancelled = new HashSet<int>();
    static int _frame = -1;
    static int _used;
    static int _spawned;
    static int _deferred;
    static int _reused;

    public static int Spawned { get { Sync(); return _spawned; } }
    public static int Deferred { get { Sync(); return _deferred; } }
    public static int Reused { get { Sync(); return _reused; } }

    public static void BeginFrame()
    {
        int frame = Time.frameCount;
        if (_frame == frame)
            return;
        _frame = frame;
        _used = 0;
        _spawned = 0;
        _deferred = 0;
        _reused = 0;
    }

    public static bool TryConsumeSpawn()
    {
        Sync();
        if (_used >= MaxSpawnsPerFrame)
        {
            _deferred++;
            return false;
        }

        _used++;
        _spawned++;
        return true;
    }

    public static void NoteReused()
    {
        Sync();
        _reused++;
    }

    public static void ClearCancel(int objectId)
    {
        Cancelled.Remove(objectId);
    }

    public static void Cancel(int objectId)
    {
        Cancelled.Add(objectId);
    }

    public static bool IsCancelled(int objectId)
    {
        return Cancelled.Contains(objectId);
    }

    static void Sync()
    {
        if (_frame != Time.frameCount)
            BeginFrame();
    }
}
