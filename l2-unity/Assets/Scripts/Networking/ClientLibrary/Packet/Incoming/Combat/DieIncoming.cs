[IncomingGamePacket(GameServerPacketType.Die)]
public sealed class DieIncoming : IncomingWirePacket<DieDto>
{
    public override void Apply(DieDto packet)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.Die(packet));
    }
}
