using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuthLogin : ClientPacket
{
    public AuthLogin(string account, int playKey1, int playKey2, int loginKey1, int loginKey2) : base((byte)GameInterludeClientPacketType.AuthLogin)
    {
        WriteSOther(account);
        WriteChar((char)0);
        WriteI(playKey2);
        WriteI(playKey1);
        WriteI(loginKey1);
        WriteI(loginKey2);

        //not insert packet type
        //when inserting a String, a regular Build adds extra bytes
        // BuildPacketOther();
        BuildPacketManualType((byte)GameInterludeClientPacketType.AuthLogin);
    }

}
