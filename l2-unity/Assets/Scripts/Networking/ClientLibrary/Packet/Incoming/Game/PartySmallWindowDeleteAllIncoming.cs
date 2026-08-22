[IncomingGamePacket(GameServerPacketType.PartySmallWindowDeleteAll)]
public sealed class PartySmallWindowDeleteAllIncoming : IncomingWirePacket<PartySmallWindowDeleteAllDto>
{
    public override void Apply(PartySmallWindowDeleteAllDto dto)
    {
        IncomingPacketActions.Queue(() => PartyManager.Instance.ApplyDeleteAll());
    }
}
