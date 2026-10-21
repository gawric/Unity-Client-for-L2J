using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// CharInfo (UserEntity) walk/run gait regression diagnostics.
/// Traces the walk->run transition decision so we can see why CharInfo
/// "stay walking" when they should run.
/// File: C:\unity\l2 client\decompile\log\gait.txt
/// Console: one line per second (rolling).
/// </summary>
public static class GaitLog
{
    const string LogPath = @"C:\unity\l2 client\decompile\log\gait.txt";
    const float FlushSec = 1f;

    // Per-object throttle: avoid spamming while still capturing every transition.
    static readonly Dictionary<int, float> LastTransitionAt = new Dictionary<int, float>(64);
    static readonly StringBuilder Line = new StringBuilder(512);
    static float _nextFlush = -1f;

    public static bool Enabled { get; set; } = false;

    public static void Log(Entity entity, string tag, string detail)
    {
        if (!Enabled)
            return;
        if (entity == null || entity.Identity == null)
            return;

        int id = entity.Identity.Id;
        string nick = entity.Identity.Name ?? ("#" + id);
        float now = Time.unscaledTime;
        float last;
        if (LastTransitionAt.TryGetValue(id, out last) && now - last < 0.2f)
            return;
        LastTransitionAt[id] = now;

        Line.Length = 0;
        Line.Append("[GAIT] ")
            .Append(tag)
            .Append(" nick=").Append(nick)
            .Append(" id=").Append(id)
            .Append(" ").Append(detail ?? string.Empty);

        string text = Line.ToString();
        Debug.Log(text);

        try
        {
            string dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, text + "\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GaitLog file: " + ex.Message);
        }
    }

    public static void Reset()
    {
        LastTransitionAt.Clear();
    }
}