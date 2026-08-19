using System;
using UnityEngine;

/// <summary>
/// Filter Console by <c>[LOBBY]</c> to see which packet stalls server-select → char-select.
/// </summary>
public static class LobbyFlowLog
{
    public const string Tag = "[LOBBY]";

    public static bool Active
    {
        get
        {
            GameManager manager = IncomingPacketActions.Manager;
            return manager == null || manager.GameState < GameState.IN_GAME;
        }
    }

    public static void Info(string message)
    {
        if (!Active)
            return;
        Debug.Log(Tag + " " + message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning(Tag + " " + message);
    }

    public static void Error(string message)
    {
        Debug.LogError(Tag + " " + message);
    }

    public static void Exception(string where, Exception ex)
    {
        Debug.LogError(Tag + " EXCEPTION at " + where + "\n" + ex);
    }

    public static string OpcodeName(byte opcode)
    {
        if (Enum.IsDefined(typeof(GameServerPacketType), opcode))
            return ((GameServerPacketType)opcode).ToString();
        return "UNKNOWN";
    }
}
