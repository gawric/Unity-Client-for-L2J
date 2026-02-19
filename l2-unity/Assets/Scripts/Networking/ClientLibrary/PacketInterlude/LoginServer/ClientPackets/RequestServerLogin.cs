using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestServerLogin : ClientPacket
{
    public RequestServerLogin(int serverId, int sessionKey1, int sessionKey2) : base((byte)LoginInterludeClientPacketTYpe.RequestServerLogin)
    {
        //Debug.Log("RequestServerLogin: serverId " + serverId);
        WriteI(sessionKey1);
        WriteI(sessionKey2);
        WriteI(serverId);

        BuildPacket();
    }
}