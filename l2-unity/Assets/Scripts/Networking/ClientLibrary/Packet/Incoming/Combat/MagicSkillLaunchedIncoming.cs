using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MagicSkillLaunched)]
public sealed class MagicSkillLaunchedIncoming : IncomingWirePacket<MagicSkillLaunchedDto>
{
    public override void Apply(MagicSkillLaunchedDto packet)
    {
    }
}
