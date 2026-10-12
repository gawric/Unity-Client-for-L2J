using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class NpcCombatTrace
{
    static readonly string PathName =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\ChainLogs\unity\npc_combat.log";

    public static bool IsCombatNpc(Entity entity)
    {
        return entity is MonsterEntity || entity is NpcEntity;
    }

    public static void Write(string stage, Entity actor, Entity target, string details)
    {
        string line =
            DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) +
            " unity=" + Time.realtimeSinceStartup.ToString("F3", CultureInfo.InvariantCulture) +
            " frame=" + Time.frameCount +
            " " + stage +
            " npc=" + Describe(actor) +
            " other=" + Describe(target) +
            (string.IsNullOrEmpty(details) ? "" : " " + details) +
            Environment.NewLine;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));
            File.AppendAllText(PathName, line);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NpcCombatTrace] " + e.Message);
        }
    }

    public static string Describe(Entity entity)
    {
        if (entity == null)
            return "null";
        int id = entity.Identity != null ? entity.Identity.Id : 0;
        string name = entity.Identity != null && !string.IsNullOrEmpty(entity.Identity.Name)
            ? entity.Identity.Name
            : entity.name;
        return id + ":" + name;
    }

    public static string Place(Entity entity)
    {
        if (entity == null || entity.transform == null)
            return "pos=?";
        Vector3 p = entity.transform.position;
        return "pos=" + p.x.ToString("F1", CultureInfo.InvariantCulture) +
            "," + p.y.ToString("F1", CultureInfo.InvariantCulture) +
            "," + p.z.ToString("F1", CultureInfo.InvariantCulture) +
            " action=" + entity.ActionSlot.Action +
            " dead=" + entity.IsDead();
    }
}
