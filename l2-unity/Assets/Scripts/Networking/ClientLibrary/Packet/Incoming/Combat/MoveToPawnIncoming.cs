using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MoveToPawn)]
public sealed class MoveToPawnIncoming : IncomingWirePacket<MoveToPawnDto>
{
    public override void Apply(MoveToPawnDto packet)
    {
        MoveCostLog.NotePawnPacket(packet != null ? packet.ObjId : 0);
        MovementPacketLatest.PutPawn(packet);
    }
}
