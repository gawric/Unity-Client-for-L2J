using System;
using UnityEngine;

[IncomingGamePacket(GameServerPacketType.SystemMessage)]
public sealed class SystemMessageIncoming : IncomingWirePacket<SystemMessageDto>
{
    public override void Apply(SystemMessageDto packet)
    {
        try
        {
            IncomingPacketActions.ApplyMessage(apply => apply.SystemMessage(packet));
        }
        catch (Exception ex)
        {
            Debug.Log("GameServerPacketHandler.OnMessage: " + ex);
        }
    }
}
