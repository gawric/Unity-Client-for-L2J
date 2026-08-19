[IncomingGamePacket(GameServerPacketType.TargetUnselected)]
public sealed class TargetUnselectedIncoming : IncomingWirePacket<TargetUnselectedDto>
{
    public override void Apply(TargetUnselectedDto dto)
    {
    }
}
