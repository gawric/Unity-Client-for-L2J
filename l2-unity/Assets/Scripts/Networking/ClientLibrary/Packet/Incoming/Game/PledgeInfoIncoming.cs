using UnityEngine;

[IncomingGamePacket(GameServerPacketType.PledgeInfo)]
public sealed class PledgeInfoIncoming : IncomingWirePacket<PledgeInfoDto>
{
    public override void Apply(PledgeInfoDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Clan.UpdatePledge(packet));
    }
}
