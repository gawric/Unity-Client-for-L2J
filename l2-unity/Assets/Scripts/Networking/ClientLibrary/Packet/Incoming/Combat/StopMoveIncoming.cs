using UnityEngine;

[IncomingGamePacket(GameServerPacketType.StopMove)]
public sealed class StopMoveIncoming : IncomingWirePacket<StopMoveDto>
{
    public override void Apply(StopMoveDto packet)
    {
        MoveCostLog.NoteStopPacket(packet != null ? packet.ObjId : 0);
        MovementPacketLatest.PutStop(packet);
    }
}
