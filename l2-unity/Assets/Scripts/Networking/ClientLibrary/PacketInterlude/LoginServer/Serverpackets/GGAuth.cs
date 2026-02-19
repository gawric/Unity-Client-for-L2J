using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;


//		buffer.writeByte(0x0b);
//buffer.writeInt(_response);
//buffer.writeInt(0);
//buffer.writeInt(0);
//buffer.writeInt(0);
//buffer.writeInt(0);
public class GGAuth : ServerPacket
{
    private int responce;
    public int Response { get { return responce; } }
    public GGAuth(byte[] d) : base(d)
    {
        Parse();
    }


    public override void Parse()
    {
        responce  = ReadI();
        var sessionId3 = ReadI();
        var sessionId4 = ReadI();
        var sessionId5 = ReadI();
        var sessionId6 = ReadI();
        Debug.Log("GGAuth response " + responce);
    }
}
