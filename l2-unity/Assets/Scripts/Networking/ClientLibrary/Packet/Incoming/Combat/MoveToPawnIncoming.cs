using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MoveToPawn)]
public sealed class MoveToPawnIncoming : IncomingWirePacket<MoveToPawnDto>
{
    public override void Apply(MoveToPawnDto packet)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.MoveToPawn(packet));
    }
}
