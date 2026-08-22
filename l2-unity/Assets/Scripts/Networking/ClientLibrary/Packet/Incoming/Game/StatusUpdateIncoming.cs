using System.Collections.Generic;

[IncomingGamePacket(GameServerPacketType.StatusUpdate)]
public sealed class StatusUpdateIncoming : IncomingPacket<StatusUpdateDto>
{
    public override StatusUpdateDto Read(PacketReader reader)
    {
        StatusUpdateDto dto = new StatusUpdateDto();
        dto.ObjectId = reader.ReadI();
        int count = reader.ReadI();
        dto.Attributes = new List<StatusUpdate.Attribute>(count);
        for (int i = 0; i < count; i++)
            dto.Attributes.Add(new StatusUpdate.Attribute(reader.ReadI(), reader.ReadI()));
        return dto;
    }

    public override void Apply(StatusUpdateDto dto)
    {
        if (IncomingPacketActions.GameWorld != null)
            IncomingPacketActions.GameWorld.StatusUpdate(dto.ObjectId, dto.Attributes);
    }
}
