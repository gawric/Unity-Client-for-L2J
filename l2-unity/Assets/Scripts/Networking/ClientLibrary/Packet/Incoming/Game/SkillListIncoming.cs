using UnityEngine;

[IncomingGamePacket(GameServerPacketType.SkillList)]
public sealed class SkillListIncoming : IncomingWirePacket<SkillListDto>
{
    public override void Apply(SkillListDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.SkillList.UpdateSkillList(packet.Skills));
    }
}
