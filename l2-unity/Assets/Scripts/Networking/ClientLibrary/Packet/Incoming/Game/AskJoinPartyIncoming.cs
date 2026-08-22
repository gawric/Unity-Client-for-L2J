using UnityEngine;

[IncomingGamePacket(GameServerPacketType.AskJoinParty)]
public sealed class AskJoinPartyIncoming : IncomingWirePacket<AskJoinPartyDto>
{
    public override void Apply(AskJoinPartyDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.PartyInvite.AddData(packet);
            IncomingPacketActions.PartyInvite.ShowWindow();
        });
    }
}
