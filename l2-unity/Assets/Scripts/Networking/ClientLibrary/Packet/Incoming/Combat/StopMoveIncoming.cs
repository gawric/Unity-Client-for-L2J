using UnityEngine;

[IncomingGamePacket(GameServerPacketType.StopMove)]
public sealed class StopMoveIncoming : IncomingWirePacket<StopMoveDto>
{
    public override void Apply(StopMoveDto packet)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.StopMove(packet));
    }
}
