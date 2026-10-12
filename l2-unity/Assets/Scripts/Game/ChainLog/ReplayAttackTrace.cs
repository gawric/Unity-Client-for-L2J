using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class ReplayAttackTrace
{
    static readonly string PathName =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\ChainLogs\unity\replay_attack_trace.log";

    public const string VisTag = "[VIS_DIST]";

    /// <summary>
    /// Mesh gap is the distance on screen. Packet gap is the server coordinates in the same packet.
    /// </summary>
    public static void LogHit(Entity attacker, Entity target, AttackDto dto)
    {
        if (!InvolvesPlayer(attacker, target) || attacker == null || target == null || dto == null)
            return;

        float meshUu = MeshUu(attacker.transform.position, target.transform.position);
        float packetUu = MeshUu(dto.AttackerPos, dto.TargetPos);
        string line =
            "HIT dmg=" + dto.Damage +
            " mesh=" + F(meshUu) + "uu" +
            " packet=" + F(packetUu) + "uu" +
            " meshAtk=" + Xy(attacker.transform.position) +
            " meshTgt=" + Xy(target.transform.position) +
            " packetAtk=" + Xy(dto.AttackerPos) +
            " packetTgt=" + Xy(dto.TargetPos) +
            " " + Describe(attacker) + " -> " + Describe(target);
        Emit(line);
    }

    public static void LogPawn(Entity mover, Entity pawn, int distanceUu)
    {
        PlayerEntity player = PlayerEntity.Instance;
        if (player == null || mover != player || pawn == null)
            return;

        float meshUu = MeshUu(mover.transform.position, pawn.transform.position);
        string line =
            "PAWN mesh=" + F(meshUu) + "uu" +
            " packetDist=" + distanceUu + "uu" +
            " meshPlayer=" + Xy(mover.transform.position) +
            " meshPawn=" + Xy(pawn.transform.position) +
            " " + Describe(pawn);
        Emit(line);
    }

    public static void LogStop(Entity entity, StopMoveDto dto)
    {
        PlayerEntity player = PlayerEntity.Instance;
        if (player == null || entity == null || dto == null)
            return;

        bool isPlayer = entity == player;
        bool isTarget = entity.Identity != null && entity.Identity.Id == player.TargetId;
        if (!isPlayer && !isTarget)
            return;

        Vector3 meshL2 = VectorUtils.ConvertPosUnityToL2j(entity.transform.position);
        float snapUu = FlatUu(meshL2.x, meshL2.y, dto.X, dto.Y);
        string line =
            "STOP who=" + (isPlayer ? "player" : "target") +
            " meshToPacket=" + F(snapUu) + "uu" +
            " mesh=" + Mathf.RoundToInt(meshL2.x) + "," + Mathf.RoundToInt(meshL2.y) +
            " packet=" + dto.X + "," + dto.Y +
            " " + Describe(entity);
        if (!isPlayer)
        {
            float meshUu = MeshUu(player.transform.position, entity.transform.position);
            line += " meshPlayerToThis=" + F(meshUu) + "uu meshPlayer=" + Xy(player.transform.position);
        }
        else if (player.TargetId != 0)
        {
            line += " targetId=" + player.TargetId;
        }
        Emit(line);
    }

    static void Emit(string line)
    {
        Debug.Log(VisTag + " " + line);
        Write("VIS_DIST", null, null, line);
    }

    static bool InvolvesPlayer(Entity a, Entity b)
    {
        PlayerEntity player = PlayerEntity.Instance;
        return player != null && (a == player || b == player);
    }

    static float MeshUu(Vector3 a, Vector3 b)
    {
        return VectorUtils.Distance2D(a, b) * 52.5f;
    }

    static float FlatUu(float ax, float ay, float bx, float by)
    {
        float dx = ax - bx;
        float dy = ay - by;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static string Xy(Vector3 unity)
    {
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(unity);
        return Mathf.RoundToInt(l2.x) + "," + Mathf.RoundToInt(l2.y);
    }

    static string F(float value)
    {
        return value.ToString("F0", CultureInfo.InvariantCulture);
    }

    public static void Write(string stage, Entity actor = null, Entity target = null, string details = "")
    {
        string actorText = Describe(actor);
        string targetText = Describe(target);
        string line =
            DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) +
            " unity=" + Time.realtimeSinceStartup.ToString("F3", CultureInfo.InvariantCulture) +
            " frame=" + Time.frameCount +
            " " + stage +
            " actor=" + actorText +
            " target=" + targetText +
            (string.IsNullOrEmpty(details) ? "" : " " + details) +
            Environment.NewLine;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));
            File.AppendAllText(PathName, line);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ReplayAttackTrace] " + e.Message);
        }
    }

    static string Describe(Entity entity)
    {
        if (entity == null)
            return "null";
        int id = entity.Identity != null ? entity.Identity.Id : 0;
        string name = entity.Identity != null ? entity.Identity.Name : entity.name;
        return id + ":" + name;
    }
}
