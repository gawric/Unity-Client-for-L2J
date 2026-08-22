using UnityEngine;

[IncomingGamePacket(GameServerPacketType.PledgeStatusChanged)]
public sealed class PledgeStatusChangedIncoming : IncomingWirePacket<PledgeStatusChangedDto>
{
    public override void Apply(PledgeStatusChangedDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Clan.UpdateClanIdInfo(packet));
    }
}
