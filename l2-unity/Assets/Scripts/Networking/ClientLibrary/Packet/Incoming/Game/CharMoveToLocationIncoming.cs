using UnityEngine;

[IncomingGamePacket(GameServerPacketType.CharMoveToLocation)]
public sealed class CharMoveToLocationIncoming : IncomingWirePacket<CharMoveToLocationDto>
{
    public override void Apply(CharMoveToLocationDto packet)
    {
        IncomingPacketActions.QueueWorld(apply => apply.MoveTo(packet.ObjId, packet.NewPosition, packet.OldPosition, packet));
    }
}
