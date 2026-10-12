using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes CallTrace_slice.json in the same shape as AutoLoginInterlude ChainLogs.
/// </summary>
public static class UnityChainJson
{
    public static readonly string[] MoveToPawnSlices =
    {
        "User.name","User.pawn","User.title","APawn.CreatureID","APawn.L2ServerObjectID","APawn.L2ServerObjectRealID",
        "APawn.NpcClassID","APawn.CharClassID","APawn.bNpc","APawn.Physics","APawn.Location","APawn.Rotation",
        "APawn.DesiredRotation","APawn.Velocity","APawn.ColLocation","APawn.CollisionRadius","APawn.CollisionHeight",
        "APawn.Controller","APawn.Environment","APawn.GroundSpeed","APawn.WaterSpeed","APawn.AirSpeed",
        "APawn.GroundMaxSpeed","APawn.GroundMinSpeed","APawn.NonAttackSpeedRate","APawn.AccelRate","APawn.DesiredSpeed",
        "APawn.BaseMovementRate","APawn.Health","APawn.ValidateLocationMoveTime","APawn.ValidateLocationSpeed",
        "APawn.MoveTimer","APawn.MoveToPawnAdjust","APawn.MoveEmergencyLevel","APawn.WalkAnimFrameInMove",
        "APawn.DestinationOffset","APawn.WantedYaw","APawn.bWantChangeYaw","APawn.bTurning","APawn.TurningDir",
        "APawn.bHasActorTarget","APawn.CurWeaponType","APawn.WalkAnimName","APawn.RunAnimName","APawn.WalkAnimRate",
        "APawn.RunAnimRate","APawn.AutoTargetPawn","APawn.fAutoTargetPendingTime","APawn.MovementAnims"
    };

    public static readonly string[] AttackSlices =
    {
        "User.name","User.pawn","User.title","APawn.CreatureID","APawn.L2ServerObjectID","APawn.L2ServerObjectRealID",
        "APawn.NpcClassID","APawn.CharClassID","APawn.bNpc","APawn.Location","APawn.Rotation","APawn.DesiredRotation",
        "APawn.Velocity","APawn.ColLocation","APawn.CollisionRadius","APawn.CollisionHeight","APawn.Controller",
        "APawn.Health","APawn.AttackSpeedRate","APawn.NonAttackSpeedRate","APawn.NAttackStatus","APawn.bPlayingSpecialAnim",
        "APawn.bHasActorTarget","APawn.bTargetSpineRotation","APawn.CurWeaponType","APawn.AttackItemClassID",
        "APawn.LastAttackItemClassID","APawn.AttackItemEnchantedValue","APawn.AtkWaitAnimName","APawn.Atk01AnimName",
        "APawn.Atk02AnimName","APawn.Atk03AnimName","APawn.Atk01AnimRate","APawn.Atk02AnimRate","APawn.Atk03AnimRate",
        "APawn.HitLocation","APawn.TakeHitLocation","APawn.EffTargetLocation","APawn.AutoTargetPawn",
        "APawn.fAutoTargetPendingTime","APawn.NActions"
    };

    public static string F32(float value)
    {
        uint bits = unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        return "{\"v\":" + Num(value) + ",\"bits\":\"" + bits.ToString("X8") + "\"}";
    }

    public static string Vec3(float x, float y, float z)
    {
        return "{\"v\":[" + Num(x) + "," + Num(y) + "," + Num(z) + "],\"bits\":[\"" +
               Bits(x) + "\",\"" + Bits(y) + "\",\"" + Bits(z) + "\"]}";
    }

    public static string Num(float value)
    {
        return value.ToString("G9", CultureInfo.InvariantCulture);
    }

    public static string Bits(float value)
    {
        uint bits = unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        return bits.ToString("X8");
    }

    public static string Quote(string value)
    {
        if (value == null)
            return "\"\"";
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static void WriteSlice(string folder, string chain, int switchPauseMs, string[] slices, List<string> records)
    {
        Directory.CreateDirectory(folder);
        StringBuilder sb = new StringBuilder(Math.Max(4096, records.Count * 128));
        sb.Append("{\n  \"chain\": ").Append(Quote(chain));
        sb.Append(",\n  \"switchPauseMs\": ").Append(switchPauseMs);
        sb.Append(",\n  \"slices\": [");
        for (int i = 0; i < slices.Length; i++)
        {
            if (i > 0)
                sb.Append(",");
            sb.Append(Quote(slices[i]));
        }
        sb.Append("],\n  \"records\": [\n");
        for (int i = 0; i < records.Count; i++)
        {
            if (i > 0)
                sb.Append(",\n");
            sb.Append("    ").Append(records[i]);
        }
        sb.Append("\n  ]\n}\n");

        string path = Path.Combine(folder, "CallTrace_slice.json");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Debug.Log("[UnityChain] wrote " + path + " n=" + records.Count);
    }
}
