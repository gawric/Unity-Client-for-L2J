using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatorPackets
{
    public static AuthGameGuard CreateGGPacket(InitPacket packet)
    {
        return new AuthGameGuard(packet.SessionId, packet.GG);
    }

    public static RequestAuthLogin CreateAuthPacket(string account, string password , int responce)
    {
        return new RequestAuthLogin(account , password , responce);
    }

    public static RequestServerList CreateServerListPacket(int  sessionKey1, int sessionKey2)
    {
        return new RequestServerList(sessionKey1, sessionKey2);
    }

    public static RequestServerLogin CreateServerLoginPacket(int serverId, int sessionKey1, int sessionKey2)
    {
        return new RequestServerLogin(serverId, sessionKey1, sessionKey2);
    }
}
