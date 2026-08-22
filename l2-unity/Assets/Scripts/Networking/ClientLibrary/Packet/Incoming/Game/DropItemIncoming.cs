[IncomingGamePacket(GameServerPacketType.DropItem)]
public sealed class DropItemIncoming : IncomingWirePacket<DropItemDto>
{
    public override void Apply(DropItemDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.QueueWorld(apply => apply.DropItem(dto));
    }
}
