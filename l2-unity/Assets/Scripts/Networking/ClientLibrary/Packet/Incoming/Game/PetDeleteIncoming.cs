[IncomingGamePacket(GameServerPacketType.PetDelete)]
public sealed class PetDeleteIncoming : IncomingPacket<DeleteObjectDto>
{
    public override DeleteObjectDto Read(PacketReader reader)
    {
        reader.ReadI();
        DeleteObjectDto dto = new DeleteObjectDto();
        dto.ObjectId = reader.ReadI();
        return dto;
    }

    public override void Apply(DeleteObjectDto dto)
    {
        SpawnFrameBudget.Cancel(dto.ObjectId);
        if (IncomingPacketActions.GameWorld != null)
            IncomingPacketActions.GameWorld.DeleteObject(dto.ObjectId);
    }
}
