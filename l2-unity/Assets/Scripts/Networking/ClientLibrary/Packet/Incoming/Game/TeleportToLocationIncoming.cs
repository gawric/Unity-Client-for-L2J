using UnityEngine;

[IncomingGamePacket(GameServerPacketType.TeleportToLocation)]
public sealed class TeleportToLocationIncoming : IncomingWirePacket<TeleportToLocationDto>
{
    public override void Apply(TeleportToLocationDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            UnityChainLog.OnTeleport(packet.TarObjId, packet.X, packet.Y, packet.Z);
            IncomingPacketActions.GameWorld.TeleportTo(packet.TarObjId, packet.TeleportPos);
        });
    }
}
