[IncomingGamePacket(GameServerPacketType.SpawnItem)]
public sealed class SpawnItemIncoming : IncomingWirePacket<SpawnItemDto>
{
    public override void Apply(SpawnItemDto dto)
    {
        if (dto == null || IncomingPacketActions.GameWorld == null)
            return;

        IncomingPacketActions.GameWorld.SpawnOrUpdateGroundItem(
            dto.ObjectId,
            dto.ItemId,
            dto.Count,
            dto.Stackable,
            dto.UnityPos,
            0);
    }
}
