[IncomingGamePacket(GameServerPacketType.SpawnItem)]
public sealed class SpawnItemIncoming : IncomingWirePacket<SpawnItemDto>
{
    public override void Apply(SpawnItemDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.QueueWorld(apply => apply.SpawnItem(dto));
    }
}
