using UnityEngine;

[IncomingGamePacket(GameServerPacketType.SkillCoolTime)]
public sealed class SkillCoolTimeIncoming : IncomingWirePacket<SkillCoolTimeDto>
{
    public override void Apply(SkillCoolTimeDto packet)
    {
    }
}
