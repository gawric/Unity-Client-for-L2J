using UnityEngine;

[IncomingGamePacket(GameServerPacketType.TeleportToLocation)]
public sealed class TeleportToLocationIncoming : IncomingWirePacket<TeleportToLocationDto>
{
    public override void Apply(TeleportToLocationDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.GameWorld.TeleportTo(packet.TarObjId, packet.TeleportPos));
    }
}
