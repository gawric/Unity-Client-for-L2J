[IncomingGamePacket(GameServerPacketType.GetItem)]
public sealed class GetItemIncoming : IncomingWirePacket<GetItemDto>
{
    public override void Apply(GetItemDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.QueueWorld(apply => apply.GetItem(dto));
    }
}
