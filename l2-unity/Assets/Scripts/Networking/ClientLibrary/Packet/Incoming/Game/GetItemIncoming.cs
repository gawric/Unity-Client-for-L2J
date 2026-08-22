[IncomingGamePacket(GameServerPacketType.GetItem)]
public sealed class GetItemIncoming : IncomingWirePacket<GetItemDto>
{
    public override void Apply(GetItemDto dto)
    {
    }
}
