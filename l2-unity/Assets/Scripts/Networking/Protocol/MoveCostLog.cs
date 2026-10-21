using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Same shape as Frida move_tick_probe: packets this frame vs CC.Move cost.
/// File: C:\unity\l2 client\decompile\log\moveTick.txt
/// Console: one line per second.
/// </summary>
public static class MoveCostLog
{
    const string LogPath = @"C:\unity\l2 client\decompile\log\moveTick.txt";
    const float FlushSec = 1f;

    static int _fMove;
    static int _fStop;
    static int _fPawn;
    static int _fChar;
    static int _fFlushUnique;
    static int _fCc;
    static double _fStepMs;
    static double _fFlushMs;

    static int _wMove;
    static int _wStop;
    static int _wPawn;
    static int _wChar;
    static int _wFlushUnique;
    static int _wCc;
    static int _wFrames;
    static int _wTicksMove;
    static int _wTicksStop;
    static int _wTicksChar;
    static int _maxMove;
    static int _maxStop;
    static int _maxChar;
    static int _maxCc;
    static double _wStepMs;
    static double _wFlushMs;
    static int _moving;
    static float _nextFlush = -1f;
    static readonly HashSet<int> Unique = new HashSet<int>(1024);
    static readonly StringBuilder Line = new StringBuilder(384);

    public static bool Enabled { get; set; } = true;

    public static void NoteMovePacket(int objId)
    {
        if (!Enabled)
            return;
        _fMove++;
        if (objId != 0)
            Unique.Add(objId);
    }

    public static void NoteStopPacket(int objId)
    {
        if (!Enabled)
            return;
        _fStop++;
        if (objId != 0)
            Unique.Add(objId);
    }

    public static void NotePawnPacket(int objId)
    {
        if (!Enabled)
            return;
        _fPawn++;
        if (objId != 0)
            Unique.Add(objId);
    }

    public static void NoteCharInfo()
    {
        if (!Enabled)
            return;
        _fChar++;
    }

    public static void NoteFlush(int unique, double ms)
    {
        if (!Enabled)
            return;
        _fFlushUnique = unique;
        _fFlushMs += ms;
    }

    public static void NoteStep(int moving, int ccMoves, double ms)
    {
        if (!Enabled)
            return;
        _moving = moving;
        _fCc += ccMoves;
        _fStepMs += ms;
    }

    public static void EndFrame()
    {
        if (!Enabled)
            return;
        _wFrames++;
        _wMove += _fMove;
        _wStop += _fStop;
        _wPawn += _fPawn;
        _wChar += _fChar;
        _wFlushUnique += _fFlushUnique;
        _wCc += _fCc;
        _wStepMs += _fStepMs;
        _wFlushMs += _fFlushMs;
        if (_fMove > _maxMove)
            _maxMove = _fMove;
        if (_fStop > _maxStop)
            _maxStop = _fStop;
        if (_fChar > _maxChar)
            _maxChar = _fChar;
        if (_fCc > _maxCc)
            _maxCc = _fCc;
        if (_fMove > 0)
            _wTicksMove++;
        if (_fStop > 0)
            _wTicksStop++;
        if (_fChar > 0)
            _wTicksChar++;

        _fMove = 0;
        _fStop = 0;
        _fPawn = 0;
        _fChar = 0;
        _fFlushUnique = 0;
        _fCc = 0;
        _fStepMs = 0;
        _fFlushMs = 0;

        if (_nextFlush < 0f)
            _nextFlush = Time.unscaledTime + FlushSec;
        if (Time.unscaledTime < _nextFlush)
            return;

        Dump();
        _nextFlush = Time.unscaledTime + FlushSec;
        Unique.Clear();
        _wMove = 0;
        _wStop = 0;
        _wPawn = 0;
        _wChar = 0;
        _wFlushUnique = 0;
        _wCc = 0;
        _wFrames = 0;
        _wTicksMove = 0;
        _wTicksStop = 0;
        _wTicksChar = 0;
        _maxMove = 0;
        _maxStop = 0;
        _maxChar = 0;
        _maxCc = 0;
        _wStepMs = 0;
        _wFlushMs = 0;
    }

    static int WorldCount()
    {
        World world = IncomingPacketActions.GameWorld;
        if (world == null)
            return 0;
        int n = 0;
        world.ForEachEntity(_ => n++);
        return n;
    }

    static void Dump()
    {
        double avgFlushUs = _wMove > 0 ? (_wFlushMs * 1000.0 / _wMove) : 0;
        Line.Length = 0;
        Line.Append("unity ");
        Line.Append("frames ").Append(_wFrames);
        Line.Append(" world ").Append(WorldCount());
        Line.Append(" unique ").Append(Unique.Count);
        Line.Append(" movePkts ").Append(_wMove);
        Line.Append(" stop ").Append(_wStop);
        Line.Append(" pawn ").Append(_wPawn);
        Line.Append(" charInfo ").Append(_wChar);
        Line.Append(" flushUnique ").Append(_wFlushUnique);
        Line.Append(" flushMs ").Append(_wFlushMs.ToString("0.000"));
        Line.Append(" avgFlushUs ").Append(avgFlushUs.ToString("0.00"));
        Line.Append(" moving ").Append(_moving);
        Line.Append(" ccMoves ").Append(_wCc);
        Line.Append(" stepMs ").Append(_wStepMs.ToString("0.000"));
        Line.Append(" maxMove ").Append(_maxMove);
        Line.Append(" maxStop ").Append(_maxStop);
        Line.Append(" maxChar ").Append(_maxChar);
        Line.Append(" maxCc ").Append(_maxCc);
        Line.Append(" ticksMove ").Append(_wTicksMove);
        Line.Append(" ticksStop ").Append(_wTicksStop);
        Line.Append(" ticksChar ").Append(_wTicksChar);
        string text = Line.ToString();
        try
        {
            string dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, text + "\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("MoveCostLog file: " + ex.Message);
        }
        Debug.Log(text);
    }
}
