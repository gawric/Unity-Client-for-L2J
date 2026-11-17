using System;
using UnityEngine;

public class ActionFailedDto : IWireDto
{
    public PlayerAction PlayerAction { get; private set; }
    public int Reason { get; private set; }

    private byte packet;

    public static ActionFailedDto From(int reason)
    {
        return new ActionFailedDto { Reason = reason };
    }

    public void ReadFrom(PacketReader reader)
    {
        try
        {
            //packet = reader.ReadB();
        }
        catch (Exception e)
        {
            // Debug.LogError(e);
        }
    }
}

