using UnityEngine;

[IncomingGamePacket(GameServerPacketType.PledgeShowMemberListDeleteAll)]
public sealed class PledgeShowMemberListDeleteAllIncoming : IncomingWirePacket<PledgeShowMemberListDeleteAllDto>
{
    public override void Apply(PledgeShowMemberListDeleteAllDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Clan.DeleteMemberData());
    }
}
