using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Records Unity MoveToPawn / Attack into ChainLogs/unity.
/// Live pose/speed/turn come from PlayerController / MovementData, not packet DTOs.
/// Attack cycle comes from PlayerStateJAtk / AttackTimingHelper. Packets stay as pkt.
/// </summary>
public static class UnityChainLog
{
    public const string Root =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\ChainLogs\unity";

    const float TickSec = 0.033f;
    const float IdleWriteSec = 8f;
    const float DieWriteSec = 1.5f;

    static bool _active;
    static string _phase = "MoveToPawn";
    static float _sessionStart;
    static float _lastEvent;
    static float _lastTick;
    static float _dieAt = -1f;
    static int _seq;
    static int _targetId;
    static int _lastMoveLogical;
    static int _attackPauseMs;
    static readonly List<string> _moveRecords = new List<string>();
    static readonly List<string> _attackRecords = new List<string>();
    static readonly Dictionary<string, string> _lastField = new Dictionary<string, string>();

    public static bool IsActive { get { return _active; } }

    public static void Toggle()
    {
        if (_active)
            Stop("F4");
        else
            Start("MoveToPawn", "F4");
    }

    public static void Tick()
    {
        if (!_active)
            return;

        float now = Time.realtimeSinceStartup;
        if (now - _lastTick >= TickSec)
        {
            _lastTick = now;
            AppendTick();
        }

        if (_dieAt > 0f && now >= _dieAt)
            Stop("Die");
        else if (now - _lastEvent >= IdleWriteSec)
            Stop("idle");
    }

    public static void OnTeleport(int objId, int x, int y, int z)
    {
        EnsureStarted("MoveToPawn", "Teleport");
        RememberTarget(0);
        AppendPacket("OnTeleportToLocation", BuildTeleportArgs(objId, x, y, z), objId, 0);
    }

    public static void OnMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        if (dto == null)
            return;
        EnsureStarted("MoveToPawn", "MoveToPawn");
        if (_phase != "MoveToPawn")
            SwitchPhase("MoveToPawn");
        RememberTarget(dto.TarObjid);
        AppendPacket("OnMoveToPawn", BuildMoveToPawnArgs(dto), dto.ObjId, dto.TarObjid);
    }

    public static void OnStopMove(Entity entity, StopMoveDto dto)
    {
        if (dto == null || !_active)
            return;
        AppendPacket("OnStopMove", BuildStopMoveArgs(dto), dto.ObjId, 0);
    }

    public static void OnAttack(Entity attacker, Entity target, AttackDto dto)
    {
        if (dto == null)
            return;
        EnsureStarted("Attack", "AttackPkt");
        if (_phase != "Attack")
            SwitchPhase("Attack");
        RememberTarget(dto.TargetId);
        AppendPacket("OnAttackPkt", BuildAttackArgs(dto), dto.AttackerObjId, dto.TargetId);
    }

    public static void OnJAtkEnter(Entity entity, string anim, float timeAtkMs, float hitAtMs, float pAtkSpd, float patkRate, float clipLenSec)
    {
        if (entity == null || entity.Identity == null)
            return;
        EnsureStarted("Attack", "JAtk");
        if (_phase != "Attack")
            SwitchPhase("Attack");
        RememberTarget(entity.TargetId);
        AppendPacket("OnJAtkEnter", BuildJAtkArgs("JAtkEnter", entity, anim, timeAtkMs, hitAtMs, pAtkSpd, patkRate, clipLenSec, 0f, 0f, default(Vector3)), entity.Identity.Id, entity.TargetId);
    }

    public static void OnJAtkHit(Entity entity, string anim, float timeAtkMs, float elapsedMs, float animNorm, Vector3 hitUnity)
    {
        if (entity == null || entity.Identity == null || !_active)
            return;
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(hitUnity);
        string args = BuildJAtkArgs("JAtkHit", entity, anim, timeAtkMs, timeAtkMs * 0.5f, AttackTimingHelper.ResolvePAtkSpd(entity), 0f, 0f, elapsedMs, animNorm, l2);
        AppendPacket("OnJAtkHit", args, entity.Identity.Id, entity.TargetId);
    }

    public static void OnJAtkExit(Entity entity, string anim, float timeAtkMs, float elapsedMs, float animNorm)
    {
        if (entity == null || entity.Identity == null || !_active)
            return;
        AppendPacket("OnJAtkExit", BuildJAtkArgs("JAtkExit", entity, anim, timeAtkMs, timeAtkMs * 0.5f, AttackTimingHelper.ResolvePAtkSpd(entity), 0f, 0f, elapsedMs, animNorm, default(Vector3)), entity.Identity.Id, entity.TargetId);
    }

    public static void OnAutoAttackStart(Entity entity, AutoAttackStartDto dto)
    {
        if (dto == null)
            return;
        EnsureStarted("Attack", "AutoAttackStart");
        if (_phase != "Attack")
            SwitchPhase("Attack");
        AppendPacket("OnAutoAttackStart", "{\"pkt\":\"AutoAttackStart\",\"user\":" + HexId(dto.EntityId) + "}", dto.EntityId, 0);
    }

    public static void OnAutoAttackStop(Entity entity, AutoAttackStopDto dto)
    {
        if (dto == null || !_active)
            return;
        AppendPacket("OnAutoAttackStop", "{\"pkt\":\"AutoAttackStop\",\"user\":" + HexId(dto.EntityId) + "}", dto.EntityId, 0);
    }

    public static void OnDie(Entity entity, DieDto dto)
    {
        if (dto == null || !_active)
            return;
        AppendPacket("OnDie", "{\"pkt\":\"Die\",\"user\":" + HexId(dto.ObjectId) + "}", dto.ObjectId, 0);
        _dieAt = Time.realtimeSinceStartup + DieWriteSec;
    }

    public static void OnStatusUpdate(int objectId, List<StatusUpdate.Attribute> attributes)
    {
        if (!_active || attributes == null)
            return;

        for (int i = 0; i < attributes.Count; i++)
        {
            StatusUpdate.Attribute attr = attributes[i];
            string name = StatusName(attr.id);
            if (name == null)
                continue;
            string args = "{\"pkt\":\"StatusUpdate\",\"user\":" + HexId(objectId) + ",\"value\":" + attr.value + "}";
            AppendPacket(name, args, objectId, 0);
        }
    }

    static void Start(string phase, string reason)
    {
        _active = true;
        _phase = phase;
        _sessionStart = Time.realtimeSinceStartup;
        _lastEvent = _sessionStart;
        _lastTick = 0f;
        _dieAt = -1f;
        _seq = 0;
        _targetId = 0;
        _lastMoveLogical = 0;
        _attackPauseMs = 0;
        _moveRecords.Clear();
        _attackRecords.Clear();
        _lastField.Clear();
        Debug.Log("[UnityChain] START " + phase + " reason=" + reason);
        WriteStatus("ACTIVE " + phase);
    }

    static void EnsureStarted(string phase, string reason)
    {
        if (!_active)
            Start(phase, reason);
    }

    static void SwitchPhase(string phase)
    {
        if (_phase == "MoveToPawn" && phase == "Attack")
        {
            int now = NowMs();
            _attackPauseMs = Math.Max(0, now - _lastMoveLogical);
            WritePhase("MoveToPawn", 0, UnityChainJson.MoveToPawnSlices, _moveRecords);
        }
        _phase = phase;
        _lastField.Clear();
        Debug.Log("[UnityChain] PHASE " + phase + " pauseMs=" + _attackPauseMs);
    }

    static void Stop(string reason)
    {
        if (!_active)
            return;
        _active = false;
        WritePhase("MoveToPawn", 0, UnityChainJson.MoveToPawnSlices, _moveRecords);
        WritePhase("Attack", _attackPauseMs, UnityChainJson.AttackSlices, _attackRecords);
        WriteStatus("STOPPED " + reason + " move=" + _moveRecords.Count + " attack=" + _attackRecords.Count);
        Debug.Log("[UnityChain] STOP reason=" + reason + " move=" + _moveRecords.Count + " attack=" + _attackRecords.Count);
    }

    static void WritePhase(string chain, int pause, string[] slices, List<string> records)
    {
        if (records.Count == 0)
            return;
        UnityChainJson.WriteSlice(Path.Combine(Root, chain), chain, pause, slices, records);
    }

    static void WriteStatus(string line)
    {
        try
        {
            Directory.CreateDirectory(Root);
            File.AppendAllText(Path.Combine(Root, "CallTrace.txt"),
                DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[UnityChain] status write failed: " + e.Message);
        }
    }

    static void AppendTick()
    {
        Entity player = PlayerEntity.Instance;
        AppendEntityTick(player);
        Entity target = FindEntity(_targetId);
        if (target != null && target != player)
            AppendEntityTick(target);
    }

    static void AppendEntityTick(Entity entity)
    {
        if (entity == null || entity.Identity == null)
            return;

        int ms = NowMs();
        string objects = BuildObjects(entity, true);
        string anim = BuildAnim(entity);
        AppendRecord("?Tick@APawn@@UAEHMW4ELevelTick@@@Z_0", ms, objects, anim, null);
    }

    static void AppendPacket(string name, string args, int userId, int targetId)
    {
        int ms = NowMs();
        _lastEvent = Time.realtimeSinceStartup;
        if (_phase == "MoveToPawn")
            _lastMoveLogical = ms;

        Entity user = FindEntity(userId);
        Entity target = FindEntity(targetId);
        StringBuilder objects = new StringBuilder();
        objects.Append("[");
        bool any = false;
        if (user != null)
        {
            string chunk = BuildObjects(user, false);
            if (chunk.Length > 2)
            {
                objects.Append(chunk, 1, chunk.Length - 2);
                any = true;
            }
        }
        if (target != null && target != user)
        {
            string chunk = BuildObjects(target, false);
            if (chunk.Length > 2)
            {
                if (any)
                    objects.Append(",");
                objects.Append(chunk, 1, chunk.Length - 2);
            }
        }
        objects.Append("]");
        AppendRecord("?" + name + "@Unity@@QAEXXZ", ms, objects.ToString(), null, args);
    }

    static void AppendRecord(string fn, int ms, string objects, string anim, string args)
    {
        _seq++;
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"seq\": ").Append(_seq);
        sb.Append(", \"tMs\": ").Append(ms);
        sb.Append(", \"logicalMs\": ").Append(ms);
        sb.Append(", \"rva\": \"unity\"");
        sb.Append(", \"fn\": ").Append(UnityChainJson.Quote(fn));
        sb.Append(", \"objects\": ").Append(objects ?? "[]");
        if (!string.IsNullOrEmpty(anim))
            sb.Append(", \"anim\": ").Append(anim);
        if (!string.IsNullOrEmpty(args))
            sb.Append(", \"args\": ").Append(args);
        sb.Append("}");
        CurrentRecords().Add(sb.ToString());
    }

    static List<string> CurrentRecords()
    {
        return _phase == "Attack" ? _attackRecords : _moveRecords;
    }

    static string BuildObjects(Entity entity, bool tick)
    {
        Dictionary<string, string> fields = Snapshot(entity);
        string ptr = Ptr(entity);
        StringBuilder changed = new StringBuilder();
        bool any = false;
        foreach (KeyValuePair<string, string> pair in fields)
        {
            string key = ptr + "." + pair.Key;
            string prev;
            if (_lastField.TryGetValue(key, out prev) && prev == pair.Value)
                continue;
            _lastField[key] = pair.Value;
            if (any)
                changed.Append(",");
            changed.Append(UnityChainJson.Quote(pair.Key)).Append(": ").Append(pair.Value);
            any = true;
        }

        if (!any)
            return tick ? "[]" : "[]";

        return "[{\"struct\": \"APawn\", \"role\": \"this\", \"ptr\": " + UnityChainJson.Quote(ptr) +
               ", \"fields\": {" + changed + "}}]";
    }

    static Dictionary<string, string> Snapshot(Entity entity)
    {
        Dictionary<string, string> fields = new Dictionary<string, string>();
        EntityIdentity id = entity.Identity;
        bool npc = entity is NpcEntity || entity is MonsterEntity;
        Vector3 unityPos = entity.transform.position;
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(unityPos);
        int heading = HeadingOf(entity.transform);
        MovementData move = GetMove(id.Id);
        PlayerController local = entity is PlayerEntity ? entity.GetComponent<PlayerController>() : null;
        bool walk;
        float moveTimer;
        float liveUnitySpeed;
        float rotateSpeed = 0f;
        int desired = heading;
        string src;
        if (local != null)
        {
            walk = local.IsWalkGait();
            moveTimer = local.GetMoveTimer();
            liveUnitySpeed = local.GetLiveMoveSpeed();
            rotateSpeed = local.GetLiveRotateSpeed();
            if (local.RunningToDestination)
                desired = DegToHeading(local.GetDesiredYawDegrees());
            src = "PlayerController";
        }
        else if (move != null)
        {
            walk = move.IsWalkGait();
            moveTimer = move.GetMoveTimer();
            liveUnitySpeed = move.GetSpeed();
            src = "MovementData";
        }
        else
        {
            walk = !entity.Running;
            moveTimer = 0f;
            liveUnitySpeed = 0f;
            src = "none";
        }

        float liveUu = UnitySpeedToUu(liveUnitySpeed);
        float walkUu = SpeedStatUu(entity, true);
        float runUu = SpeedStatUu(entity, false);
        float radius = entity.Appearance != null ? entity.Appearance.CollisionRadius : 9f;
        float height = entity.Appearance != null ? entity.Appearance.CollisionHeight : 23f;
        float pAtk = AttackTimingHelper.ResolvePAtkSpd(entity);
        float timeAtk = AttackTimingHelper.ResolveAttackCycleMs(entity, AnimName(entity));

        fields["Name"] = UnityChainJson.Quote(id.Name);
        fields["Title"] = UnityChainJson.Quote(id.Title);
        fields["CreatureID"] = id.Id.ToString();
        fields["L2ServerObjectID"] = id.Id.ToString();
        fields["L2ServerObjectRealID"] = id.Id.ToString();
        fields["NpcClassID"] = id.NpcId.ToString();
        fields["CharClassID"] = id.PlayerClass.ToString();
        fields["bNpc"] = npc ? "true" : "false";
        fields["Location"] = UnityChainJson.Vec3(l2.x, l2.y, l2.z);
        fields["UnityLocation"] = UnityChainJson.Vec3(unityPos.x, unityPos.y, unityPos.z);
        fields["Rotation"] = "[0," + heading + ",0]";
        fields["DesiredRotation"] = "[0," + desired + ",0]";
        fields["ColLocation"] = UnityChainJson.Vec3(l2.x, l2.y, l2.z);
        fields["CollisionRadius"] = UnityChainJson.F32(radius);
        fields["CollisionHeight"] = UnityChainJson.F32(height);
        fields["GroundSpeed"] = UnityChainJson.F32(liveUu);
        fields["UnityMoveSpeed"] = UnityChainJson.F32(liveUnitySpeed);
        fields["RotateSpeed"] = UnityChainJson.F32(rotateSpeed);
        fields["GroundMaxSpeed"] = UnityChainJson.F32(runUu);
        fields["GroundMinSpeed"] = UnityChainJson.F32(walkUu);
        fields["NonAttackSpeedRate"] = UnityChainJson.F32(1.1f);
        fields["AttackSpeedRate"] = UnityChainJson.F32(pAtk);
        fields["TimeAtkMs"] = UnityChainJson.F32(timeAtk);
        fields["Health"] = Mathf.RoundToInt((float)entity.Hp()).ToString();
        fields["MoveTimer"] = UnityChainJson.F32(moveTimer);
        fields["WalkAnimFrameInMove"] = "12";
        fields["bHasActorTarget"] = entity.Target != null ? "true" : "false";
        fields["SpeedSrc"] = UnityChainJson.Quote(src);
        return fields;
    }

    static string BuildAnim(Entity entity)
    {
        bool walk;
        PlayerController local = entity is PlayerEntity ? entity.GetComponent<PlayerController>() : null;
        if (local != null)
            walk = local.IsWalkGait();
        else
        {
            MovementData move = entity.Identity != null ? GetMove(entity.Identity.Id) : null;
            walk = move != null ? move.IsWalkGait() : !entity.Running;
        }
        string animName = AnimName(entity);
        if (string.IsNullOrEmpty(animName))
            animName = walk ? "Walk" : "Run";
        return "{\"gait\": " + UnityChainJson.Quote(walk ? "walk" : "run") +
               ", \"name\": " + UnityChainJson.Quote(animName) +
               ", \"rate\": 1.10000002}";
    }

    static string AnimName(Entity entity)
    {
        PlayerEntity player = entity as PlayerEntity;
        if (player != null)
        {
            string current = player.GetCurrentAnimName();
            if (!string.IsNullOrEmpty(current))
                return current;
            if (player.RandomName != null)
                return player.RandomName.ToString();
        }
        return entity != null ? entity.name : "";
    }

    static float UnitySpeedToUu(float unitySpeed)
    {
        if (unitySpeed <= 0f)
            return 0f;
        return unitySpeed / 0.0189f;
    }

    static float SpeedStatUu(Entity entity, bool walk)
    {
        if (entity.Stats == null)
            return walk ? 80f : 115f;
        float real = walk ? entity.Stats.WalkRealSpeed : entity.Stats.RunRealSpeed;
        if (real > 1f)
            return real;
        int bas = walk ? entity.Stats.BaseWalkingSpeed : entity.Stats.BaseRunSpeed;
        if (bas > 0)
            return bas;
        return walk ? 80f : 115f;
    }

    static int DegToHeading(float degrees)
    {
        int heading = Mathf.RoundToInt(degrees / 360f * 65536f) % 65536;
        if (heading < 0)
            heading += 65536;
        return heading;
    }

    static int HeadingOf(Transform transform)
    {
        float yaw = transform.eulerAngles.y;
        int heading = Mathf.RoundToInt(yaw / 360f * 65536f) % 65536;
        if (heading < 0)
            heading += 65536;
        return heading;
    }

    static MovementData GetMove(int objectId)
    {
        IMoveAllCharacters moves = IncomingPacketActions.Moves;
        return moves != null ? moves.GetMove(objectId) : null;
    }

    static Entity FindEntity(int objectId)
    {
        if (objectId <= 0)
            return null;
        World world = IncomingPacketActions.GameWorld;
        if (world == null)
            return PlayerEntity.Instance != null && PlayerEntity.Instance.Identity != null &&
                   PlayerEntity.Instance.Identity.Id == objectId
                ? PlayerEntity.Instance
                : null;
        return world.GetEntityNoLockSync(objectId);
    }

    static void RememberTarget(int objectId)
    {
        if (objectId > 0)
            _targetId = objectId;
    }

    static int NowMs()
    {
        return Mathf.Max(0, Mathf.RoundToInt((Time.realtimeSinceStartup - _sessionStart) * 1000f));
    }

    static string Ptr(Entity entity)
    {
        return entity.Identity.Id.ToString("X8");
    }

    static string HexId(int objectId)
    {
        return UnityChainJson.Quote(objectId.ToString("X8"));
    }

    static string StatusName(int id)
    {
        switch ((StatusUpdate.AttributeType)id)
        {
            case StatusUpdate.AttributeType.CUR_HP:
                return "OnHpUpdate";
            case StatusUpdate.AttributeType.CUR_MP:
                return "OnMpUpdate";
            case StatusUpdate.AttributeType.CUR_CP:
                return "OnCpUpdate";
            case StatusUpdate.AttributeType.MAX_CP:
                return "OnMaxCpUpdate";
            default:
                return null;
        }
    }

    static string BuildMoveToPawnArgs(MoveToPawnDto dto)
    {
        return "{\"pkt\":\"MoveToPawn\",\"user\":" + HexId(dto.ObjId) +
               ",\"target\":" + HexId(dto.TarObjid) +
               ",\"distance\":" + dto.DistanceUu +
               ",\"origin\":{\"x\":" + dto.X + ",\"y\":" + dto.Y + ",\"z\":" + dto.Z + "}}";
    }

    static string BuildStopMoveArgs(StopMoveDto dto)
    {
        return "{\"pkt\":\"StopMove\",\"user\":" + HexId(dto.ObjId) +
               ",\"pos\":{\"x\":" + dto.X + ",\"y\":" + dto.Y + ",\"z\":" + dto.Z +
               "},\"heading\":" + dto.Heading + "}";
    }

    static string BuildAttackArgs(AttackDto dto)
    {
        int miss = dto.FirstHit != null && dto.FirstHit.isMiss() ? 1 : 0;
        return "{\"src\":\"pkt\",\"pkt\":\"Attack\",\"user\":" + HexId(dto.AttackerObjId) +
               ",\"target\":" + HexId(dto.TargetId) +
               ",\"i0\":" + dto.Damage + ",\"i1\":" + miss +
               ",\"hit\":{\"x\":" + dto.AX + ",\"y\":" + dto.AY + ",\"z\":" + dto.AZ + "}}";
    }

    static string BuildJAtkArgs(string kind, Entity entity, string anim, float timeAtkMs, float hitAtMs, float pAtkSpd, float patkRate, float clipLenSec, float elapsedMs, float animNorm, Vector3 hitL2)
    {
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(entity.transform.position);
        string json = "{\"src\":\"PlayerStateJAtk\",\"kind\":" + UnityChainJson.Quote(kind) +
               ",\"user\":" + HexId(entity.Identity.Id) +
               ",\"name\":" + UnityChainJson.Quote(entity.Identity.Name) +
               ",\"anim\":" + UnityChainJson.Quote(anim ?? "") +
               ",\"timeAtkMs\":" + UnityChainJson.Num(timeAtkMs) +
               ",\"hitAtMs\":" + UnityChainJson.Num(hitAtMs) +
               ",\"pAtkSpd\":" + UnityChainJson.Num(pAtkSpd) +
               ",\"patkRate\":" + UnityChainJson.Num(patkRate) +
               ",\"clipLenSec\":" + UnityChainJson.Num(clipLenSec) +
               ",\"elapsedMs\":" + UnityChainJson.Num(elapsedMs) +
               ",\"animNorm\":" + UnityChainJson.Num(animNorm) +
               ",\"origin\":{\"x\":" + Mathf.RoundToInt(l2.x) + ",\"y\":" + Mathf.RoundToInt(l2.y) + ",\"z\":" + Mathf.RoundToInt(l2.z) + "}";
        if (hitL2.sqrMagnitude > 0.01f)
            json += ",\"hit\":{\"x\":" + Mathf.RoundToInt(hitL2.x) + ",\"y\":" + Mathf.RoundToInt(hitL2.y) + ",\"z\":" + Mathf.RoundToInt(hitL2.z) + "}";
        return json + "}";
    }

    static string BuildTeleportArgs(int objId, int x, int y, int z)
    {
        return "{\"pkt\":\"TeleportToLocation\",\"user\":" + HexId(objId) +
               ",\"pos\":{\"x\":" + x + ",\"y\":" + y + ",\"z\":" + z + "}}";
    }
}
