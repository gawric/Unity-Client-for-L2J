[IncomingGamePacket(GameServerPacketType.PartySmallWindowAdd)]
public sealed class PartySmallWindowAddIncoming : IncomingWirePacket<PartySmallWindowAddDto>
{
    public override void Apply(PartySmallWindowAddDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.Queue(() => PartyManager.Instance.ApplyAdd(dto));
    }
}
