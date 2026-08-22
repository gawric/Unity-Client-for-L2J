[IncomingGamePacket(GameServerPacketType.Revive)]
public sealed class ReviveIncoming : IncomingPacket<ReviveDto>
{
    public override ReviveDto Read(PacketReader reader)
    {
        ReviveDto dto = new ReviveDto();
        dto.ObjectId = reader.ReadI();
        return dto;
    }

    public override void Apply(ReviveDto dto)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.Revive(dto));
    }
}
