using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewCharacter : ClientPacket
{
    public NewCharacter() : base((byte)GameInterludeClientPacketType.NewCharacter)
    {
        //WriteSOther(account);
       // WriteChar((char)0);
        //WriteI(playKey2);
        //WriteI(playKey1);
        //WriteI(loginKey1);
       // WriteI(loginKey2);

        //not insert packet type
        //when inserting a String, a regular Build adds extra bytes
        BuildPacket();
    }
}
