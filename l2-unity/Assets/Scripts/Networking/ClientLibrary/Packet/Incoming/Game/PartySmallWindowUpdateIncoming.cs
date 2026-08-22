[IncomingGamePacket(GameServerPacketType.PartySmallWindowUpdate)]
public sealed class PartySmallWindowUpdateIncoming : IncomingWirePacket<PartySmallWindowUpdateDto>
{
    public override void Apply(PartySmallWindowUpdateDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.Queue(() => PartyManager.Instance.ApplyUpdate(dto));
    }
}
