using L2_login;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Diagnostics;

public class LoginClientInterludePacketHandler : ClientPacketHandler
{
 
    public override void SendPacket(ClientPacket packet)
    {
        if (LoginClient.Instance.LogSentPackets)
        {
            LoginClientPacketType packetType = (LoginClientPacketType)packet.GetPacketType();
            if (packetType != LoginClientPacketType.Ping)
            {
                Debug.Log("[" + Thread.CurrentThread.ManagedThreadId + "] [LoginServer] Sending packet:" + packetType);
               
            }
        }
        _client.SendPacket(packet);

    }
}
