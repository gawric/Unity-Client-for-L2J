[IncomingGamePacket(GameServerPacketType.PartySpelled)]
public sealed class PartySpelledIncoming : IncomingWirePacket<PartySpelledDto>
{
    public override void Apply(PartySpelledDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.Queue(() => PartyManager.Instance.ApplySpelled(dto));
    }
}
