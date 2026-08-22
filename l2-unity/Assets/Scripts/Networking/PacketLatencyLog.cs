using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Combat packet RX (socket thread) vs Apply (Unity Update).
/// Filter Console: [PKTQ]
/// </summary>
public static class PacketLatencyLog
{
    const string Tag = "[PKTQ]";

    [ThreadStatic]
    static int _incomingRemain;

    static readonly object Gate = new object();
    static readonly Dictionary<object, Stamp> Pending = new Dictionary<object, Stamp>();
    static long _distTick;
    static float _distRealtime;

    struct Stamp
    {
        public long RecvTick;
        public long ParseTick;
        public long QueueTick;
        public int IncomingAhead;
        public int IncomingRemain;
        public int MainPending;
        public string Name;
    }

    public static void SetIncomingRemain(int remain)
    {
        _incomingRemain = remain;
    }

    public static void MarkDist()
    {
        _distTick = Stopwatch.GetTimestamp();
        _distRealtime = Time.realtimeSinceStartup;
    }

    public static void OnParsed(INetworkModel model, long recvTick, int incomingAhead)
    {
        if (!Watch(model))
            return;

        Stamp stamp = new Stamp
        {
            RecvTick = recvTick > 0 ? recvTick : Stopwatch.GetTimestamp(),
            ParseTick = Stopwatch.GetTimestamp(),
            IncomingAhead = incomingAhead,
            IncomingRemain = _incomingRemain,
            Name = NameOf(model)
        };
        lock (Gate)
            Pending[model] = stamp;

        UnityEngine.Debug.Log(Tag + " RX " + stamp.Name +
            " recvToParse=" + Ms(stamp.ParseTick - stamp.RecvTick).ToString("F2") + "ms" +
            " qInAhead=" + stamp.IncomingAhead +
            " qInRemain=" + stamp.IncomingRemain);
    }

    public static void OnQueued(INetworkModel model, int mainPending)
    {
        if (!Watch(model))
            return;

        lock (Gate)
        {
            Stamp stamp;
            if (!Pending.TryGetValue(model, out stamp))
                return;
            stamp.QueueTick = Stopwatch.GetTimestamp();
            stamp.MainPending = mainPending;
            Pending[model] = stamp;
        }
    }

    public static void OnApply(INetworkModel model)
    {
        if (!Watch(model))
            return;

        Stamp stamp;
        lock (Gate)
        {
            if (!Pending.TryGetValue(model, out stamp))
                return;
            Pending.Remove(model);
        }

        long now = Stopwatch.GetTimestamp();
        float recvToApply = Ms(now - stamp.RecvTick);
        float parseToApply = stamp.ParseTick > 0 ? Ms(now - stamp.ParseTick) : -1f;
        float queueToApply = stamp.QueueTick > 0 ? Ms(now - stamp.QueueTick) : -1f;
        float sinceDist = _distTick > 0 ? Ms(now - _distTick) : -1f;

        UnityEngine.Debug.Log(Tag + " APPLY " + stamp.Name +
            Who(model) +
            " recvToApply=" + recvToApply.ToString("F2") + "ms" +
            " parseToApply=" + parseToApply.ToString("F2") + "ms" +
            " queueToApply=" + queueToApply.ToString("F2") + "ms" +
            " qInAhead=" + stamp.IncomingAhead +
            " qInRemain=" + stamp.IncomingRemain +
            " qMain=" + stamp.MainPending +
            " frameDt=" + Time.deltaTime.ToString("F4") +
            " sinceDist=" + sinceDist.ToString("F1") + "ms" +
            " distAge=" + (_distRealtime > 0f
                ? ((Time.realtimeSinceStartup - _distRealtime) * 1000f).ToString("F1")
                : "-") + "ms");
    }

    public static string DistAgeMs()
    {
        if (_distTick <= 0)
            return "-";
        return Ms(Stopwatch.GetTimestamp() - _distTick).ToString("F1");
    }

    static bool Watch(INetworkModel model)
    {
        if (model == null)
            return false;
        Type t = model.GetType();
        return t == typeof(AttackDto)
            || t == typeof(StopMoveDto)
            || t == typeof(MoveToPawnDto)
            || t == typeof(AutoAttackStartDto);
    }

    static string NameOf(INetworkModel model)
    {
        if (model is AttackDto atk)
            return "AttackDto a=" + atk.AttackerObjId + " t=" + atk.TargetId;
        if (model is StopMoveDto stop)
            return "StopMoveDto id=" + stop.ObjId;
        if (model is MoveToPawnDto pawn)
            return "MoveToPawnDto id=" + pawn.ObjId + " pawn=" + pawn.TarObjid;
        if (model is AutoAttackStartDto auto)
            return "AutoAttackStartDto id=" + auto.EntityId;
        return model != null ? model.GetType().Name : "-";
    }

    static string Who(INetworkModel model)
    {
        if (model is AttackDto atk)
            return " a=" + Describe(atk.AttackerObjId) + " t=" + Describe(atk.TargetId);
        if (model is StopMoveDto stop)
            return " who=" + Describe(stop.ObjId);
        if (model is MoveToPawnDto pawn)
            return " who=" + Describe(pawn.ObjId) + " pawn=" + Describe(pawn.TarObjid);
        if (model is AutoAttackStartDto auto)
            return " who=" + Describe(auto.EntityId);
        return "";
    }

    static string Describe(int id)
    {
        World world = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld
            : World.Instance;
        Entity entity = world != null ? world.GetEntityNoLockSync(id) : null;
        if (entity == null)
            return id.ToString();
        return EntityActionCombatLog.Describe(entity);
    }

    static float Ms(long ticks)
    {
        if (ticks < 0)
            return 0f;
        return (float)(ticks * 1000.0 / Stopwatch.Frequency);
    }
}
