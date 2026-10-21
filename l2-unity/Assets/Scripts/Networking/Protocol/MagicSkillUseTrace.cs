using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Phase times inside one MagicSkillUse apply. Dumps when the packet takes at least 8 ms.
/// </summary>
public static class MagicSkillUseTrace
{
    struct Row
    {
        public int Level;
        public string Name;
        public double Ms;
    }

    const double DumpMs = 8.0;
    const string LogPath = @"C:\unity\l2 client\decompile\log\magicSkillUse.txt";

    static readonly List<Row> Rows = new List<Row>(24);
    static bool _active;
    static int _suppress;
    static int _level;
    static int _skillId;
    static long _started;

    public static void Begin(int skillId)
    {
        if (_active)
        {
            _suppress++;
            return;
        }

        _active = true;
        _skillId = skillId;
        _level = 0;
        _started = Stopwatch.GetTimestamp();
        Rows.Clear();
    }

    public static void End()
    {
        if (_suppress > 0)
        {
            _suppress--;
            return;
        }

        if (!_active)
            return;

        _active = false;
        double total = Elapsed(_started);
        if (total >= DumpMs)
            Dump(total);
        Rows.Clear();
    }

    public static Span Measure(string name)
    {
        return new Span(name);
    }

    public struct Span : IDisposable
    {
        readonly string _name;
        readonly long _started;
        readonly int _level;
        readonly bool _on;

        public Span(string name)
        {
            _on = _active;
            _name = name;
            _level = MagicSkillUseTrace._level;
            _started = Stopwatch.GetTimestamp();
            if (_on)
                MagicSkillUseTrace._level++;
        }

        public void Dispose()
        {
            if (!_on)
                return;

            MagicSkillUseTrace._level--;
            Rows.Add(new Row
            {
                Level = _level,
                Name = _name,
                Ms = Elapsed(_started)
            });
        }
    }

    static void Dump(double total)
    {
        double accounted = 0.0;
        var text = new StringBuilder(256);
        text.Append("skill ").Append(_skillId);
        text.Append(" frame ").Append(UnityEngine.Time.frameCount);
        text.Append(" applyMs ").Append(total.ToString("0.0"));
        for (int i = 0; i < Rows.Count; i++)
        {
            Row row = Rows[i];
            if (row.Level == 0)
                accounted += row.Ms;
            text.Append("\n  ");
            for (int pad = 0; pad < row.Level; pad++)
                text.Append("  ");
            text.Append(row.Name).Append(" ").Append(row.Ms.ToString("0.0")).Append("ms");
        }

        double other = total - accounted;
        if (other > 0.5)
            text.Append("\n  other ").Append(other.ToString("0.0")).Append("ms");

        string line = text.ToString();
        try
        {
            File.AppendAllText(LogPath, line + "\n\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("MagicSkillUseTrace file: " + ex.Message);
        }

        Debug.Log(line);
    }

    static double Elapsed(long started)
    {
        return (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
    }
}
