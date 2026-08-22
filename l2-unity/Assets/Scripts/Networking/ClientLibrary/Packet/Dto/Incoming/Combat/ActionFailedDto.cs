using System;
using UnityEngine;

public class ActionFailedDto : IWireDto
{
    public PlayerAction PlayerAction { get; private set; }

    private byte packet;
    

    public void ReadFrom(PacketReader reader)
    {
        try
        {
            //packet = reader.ReadB();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}

