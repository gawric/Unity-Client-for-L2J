using UnityEngine;

[IncomingGamePacket(GameServerPacketType.JoinParty)]
public sealed class JoinPartyIncoming : IncomingWirePacket<JoinPartyDto>
{
    public override void Apply(JoinPartyDto dto)
    {
        if (dto == null)
            return;

        IncomingPacketActions.Queue(() =>
        {
            // Just confirms to the inviter whether their invite was accepted/declined - the actual
            // roster update for either side arrives via PartySmallWindowAll/Add.
            Debug.Log(dto.Accepted ? "Party invitation accepted." : "Party invitation declined.");
        });
    }
}
