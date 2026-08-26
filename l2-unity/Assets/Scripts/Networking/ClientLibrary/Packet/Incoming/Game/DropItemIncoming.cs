[IncomingGamePacket(GameServerPacketType.DropItem)]
public sealed class DropItemIncoming : IncomingWirePacket<DropItemDto>
{
    public override void Apply(DropItemDto dto)
    {
        if (dto == null || IncomingPacketActions.GameWorld == null)
            return;

        IncomingPacketActions.GameWorld.SpawnOrUpdateGroundItem(
            dto.ObjectId,
            dto.ItemId,
            dto.Count,
            dto.Stackable,
            dto.UnityPos,
            dto.CharObjId);
    }
}
