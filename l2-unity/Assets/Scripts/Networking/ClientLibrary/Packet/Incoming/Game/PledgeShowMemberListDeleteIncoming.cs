using UnityEngine;

[IncomingGamePacket(GameServerPacketType.PledgeShowMemberListDelete)]
public sealed class PledgeShowMemberListDeleteIncoming : IncomingWirePacket<PledgeShowMemberListDeleteDto>
{
    public override void Apply(PledgeShowMemberListDeleteDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Clan.DeleteMemberData(packet));
    }
}
