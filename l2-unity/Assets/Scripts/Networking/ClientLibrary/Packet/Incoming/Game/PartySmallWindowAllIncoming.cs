[IncomingGamePacket(GameServerPacketType.PartySmallWindowAll)]
public sealed class PartySmallWindowAllIncoming : IncomingWirePacket<PartySmallWindowAllDto>
{
    public override void Apply(PartySmallWindowAllDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.Queue(() => PartyManager.Instance.ApplyAll(dto));
    }
}
