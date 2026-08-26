[IncomingGamePacket(GameServerPacketType.GetItem)]
public sealed class GetItemIncoming : IncomingWirePacket<GetItemDto>
{
    public override void Apply(GetItemDto dto)
    {
        if (dto == null || IncomingPacketActions.GameWorld == null)
            return;
        IncomingPacketActions.GameWorld.PickupGroundItem(dto.ObjectId, dto.PlayerId, dto.UnityPos);
    }
}
