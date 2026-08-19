[IncomingGamePacket(GameServerPacketType.DropItem)]
public sealed class DropItemIncoming : IncomingWirePacket<DropItemDto>
{
    public override void Apply(DropItemDto dto)
    {
    }
}
