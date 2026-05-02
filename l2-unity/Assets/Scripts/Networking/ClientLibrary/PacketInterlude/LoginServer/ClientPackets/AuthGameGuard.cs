using L2_login;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuthGameGuard : ClientPacket
{
    public AuthGameGuard(int _sessionId, int[] gg) : base((byte)LoginInterludeClientPacketTYpe.AuthGameGuard)
    {

        WriteI(_sessionId);
        WriteI(gg[0]);
        WriteI(gg[1]);
        WriteI(gg[2]);
        WriteI(gg[3]);

        WriteB(0);
        WriteB(0);
        WriteB(0);

        BuildPacketExperimental(1);
    }
}
