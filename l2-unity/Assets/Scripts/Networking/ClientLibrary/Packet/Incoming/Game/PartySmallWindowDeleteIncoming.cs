[IncomingGamePacket(GameServerPacketType.PartySmallWindowDelete)]
public sealed class PartySmallWindowDeleteIncoming : IncomingWirePacket<PartySmallWindowDeleteDto>
{
    public override void Apply(PartySmallWindowDeleteDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.Queue(() => PartyManager.Instance.ApplyDelete(dto));
    }
}
