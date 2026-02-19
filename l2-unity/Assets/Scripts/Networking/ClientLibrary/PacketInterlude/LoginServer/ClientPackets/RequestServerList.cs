using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestServerList : ClientPacket
{
    public RequestServerList(int _sessionKey1 , int _sessionKey2) : base((byte)LoginInterludeClientPacketTYpe.RequestServerList)
    {

        WriteI(_sessionKey1);
        WriteI(_sessionKey2);
        WriteI(0);
        WriteI(0);
        //BuildPacket();
        BuildPacketExperimental(0);
    }
}
