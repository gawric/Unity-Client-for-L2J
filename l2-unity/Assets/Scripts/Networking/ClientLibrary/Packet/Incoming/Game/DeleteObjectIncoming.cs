using UnityEngine;

[IncomingGamePacket(GameServerPacketType.DeleteObject)]
public sealed class DeleteObjectIncoming : IncomingPacket<DeleteObjectDto>
{
    public override DeleteObjectDto Read(PacketReader reader)
    {
        DeleteObjectDto dto = new DeleteObjectDto();
        dto.ObjectId = reader.ReadI();
        reader.ReadI();
        return dto;
    }

    public override void Apply(DeleteObjectDto dto)
    {
        SpawnFrameBudget.Cancel(dto.ObjectId);
        if (IncomingPacketActions.GameWorld != null)
            IncomingPacketActions.GameWorld.DeleteObject(dto.ObjectId);
    }
}
