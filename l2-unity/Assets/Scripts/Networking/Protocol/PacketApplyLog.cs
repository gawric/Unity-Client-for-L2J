using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Times top-level packet handlers on the main thread.
/// One file line and one console line when a batch takes at least 50 ms.
/// </summary>
public static class PacketApplyLog
{
    struct Stat
    {
        public int Count;
        public double Ms;
    }

    const double DumpMs = 50.0;
    const string LogPath = @"C:\unity\l2 client\decompile\log\packetApply.txt";

    static readonly Dictionary<string, Stat> Stats = new Dictionary<string, Stat>(64);
    static readonly List<KeyValuePair<string, Stat>> SortBuffer = new List<KeyValuePair<string, Stat>>(64);
    static int _depth;
    static int _packets;
    static double _ms;

    public static void Time(INetworkModel model, Action handle)
    {
        Time(model != null ? model.GetType().Name : "packet", handle);
    }

    public static void Time(string name, Action handle)
    {
        long start = Stopwatch.GetTimestamp();
        _depth++;
        try
        {
            handle();
        }
        finally
        {
            _depth--;
            if (_depth == 0)
            {
                double elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                Stat stat;
                Stats.TryGetValue(name, out stat);
                stat.Count++;
                stat.Ms += elapsed;
                Stats[name] = stat;
                _packets++;
                _ms += elapsed;
            }
        }
    }

    public static void EndBatch()
    {
        bool spawnBusy = SpawnFrameBudget.Deferred > 0 || SpawnFrameBudget.Spawned > 0;
        if (_packets == 0 && !spawnBusy)
            return;

        if (_ms >= DumpMs || SpawnFrameBudget.Deferred > 0)
            Dump(_ms >= DumpMs);

        Stats.Clear();
        _packets = 0;
        _ms = 0.0;
    }

    static void Dump(bool toConsole)
    {
        SortBuffer.Clear();
        foreach (KeyValuePair<string, Stat> pair in Stats)
            SortBuffer.Add(pair);
        SortBuffer.Sort(CompareMs);

        var text = new StringBuilder(512);
        text.Append("frame ").Append(UnityEngine.Time.frameCount);
        text.Append(" packets ").Append(_packets);
        text.Append(" applyMs ").Append(_ms.ToString("0.0"));
        text.Append(" spawn ").Append(SpawnFrameBudget.Spawned);
        text.Append(" deferred ").Append(SpawnFrameBudget.Deferred);
        text.Append(" reused ").Append(SpawnFrameBudget.Reused);

        int shownPackets = 0;
        double shownMs = 0.0;
        int limit = SortBuffer.Count < 12 ? SortBuffer.Count : 12;
        for (int i = 0; i < limit; i++)
        {
            KeyValuePair<string, Stat> row = SortBuffer[i];
            double avg = row.Value.Count > 0 ? row.Value.Ms / row.Value.Count : 0.0;
            text.Append("\n  ").Append(row.Key);
            text.Append(" x").Append(row.Value.Count);
            text.Append(" ").Append(row.Value.Ms.ToString("0.0")).Append("ms");
            text.Append(" avg ").Append(avg.ToString("0.00"));
            shownPackets += row.Value.Count;
            shownMs += row.Value.Ms;
        }

        if (SortBuffer.Count > limit)
        {
            text.Append("\n  other x").Append(_packets - shownPackets);
            text.Append(" ").Append((_ms - shownMs).ToString("0.0")).Append("ms");
        }

        string line = text.ToString();
        try
        {
            File.AppendAllText(LogPath, line + "\n\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PacketApplyLog file: " + ex.Message);
        }

        if (toConsole)
            Debug.Log(line);
    }

    static int CompareMs(KeyValuePair<string, Stat> a, KeyValuePair<string, Stat> b)
    {
        return b.Value.Ms.CompareTo(a.Value.Ms);
    }
}
