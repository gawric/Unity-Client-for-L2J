using UnityEngine;

[IncomingGamePacket(GameServerPacketType.CharMoveToLocation)]
public sealed class CharMoveToLocationIncoming : IncomingWirePacket<CharMoveToLocationDto>
{
    public override void Apply(CharMoveToLocationDto packet)
    {
        MoveCostLog.NoteMovePacket(packet != null ? packet.ObjId : 0);
        MovementPacketLatest.PutMove(packet);
    }
}
