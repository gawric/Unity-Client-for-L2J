using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MagicSkillUse)]
public sealed class MagicSkillUseIncoming : IncomingWirePacket<MagicSkillUseDto>
{
    public override void Apply(MagicSkillUseDto packet)
    {
        if (packet == null)
            return;

        IncomingPacketActions.QueueWorld(apply => apply.MagicSkillUse(packet));
    }
}
