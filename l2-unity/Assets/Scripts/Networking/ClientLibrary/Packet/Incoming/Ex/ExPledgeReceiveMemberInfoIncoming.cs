using UnityEngine;

[IncomingExPacket(GameExServerPacketType.ExPledgeReceiveMemberInfo)]
public sealed class ExPledgeReceiveMemberInfoIncoming : IncomingWirePacket<PledgeReceiveMemberInfoDto>
{
    public override void Apply(PledgeReceiveMemberInfoDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Clan.UpdateDetailedInfo(packet));
    }
}
