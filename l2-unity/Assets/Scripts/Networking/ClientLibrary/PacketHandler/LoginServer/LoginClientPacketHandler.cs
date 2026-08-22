using System.Threading;
using UnityEngine;

public class LoginClientPacketHandler : ClientPacketHandler
{
    public override void SendPacket(IOutgoingPacket packet)
    {
        if (IncomingPacketActions.Login.LogSentPackets)
            Debug.Log("[" + Thread.CurrentThread.ManagedThreadId + "] [LoginServer] Sending packet:" + (LoginClientPacketType)packet.GetPacketType());

        _client.SendPacket(packet);
    }
}
