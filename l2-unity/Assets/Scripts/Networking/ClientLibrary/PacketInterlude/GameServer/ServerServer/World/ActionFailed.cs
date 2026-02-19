using System;
using UnityEngine;

public class ActionFailed : ServerPacket
{
    public PlayerAction PlayerAction { get; private set; }

    private byte packet;
    public ActionFailed(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        try
        {
            //packet = ReadB();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}

