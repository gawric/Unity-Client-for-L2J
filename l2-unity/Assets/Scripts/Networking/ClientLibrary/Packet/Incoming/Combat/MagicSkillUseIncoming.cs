using UnityEngine;

[IncomingGamePacket(GameServerPacketType.MagicSkillUse)]
public sealed class MagicSkillUseIncoming : IncomingWirePacket<MagicSkillUseDto>
{
    public override void Apply(MagicSkillUseDto packet)
    {
        // Temporary: skill cast/VFX hides CharInfo spawn cost in packetApply.txt.
        // Packet is still read, so the stream stays aligned. Restore the queue below to turn it back on.
         if (packet == null)
             return;
         if (UnityAutoLogin.IsActive)
             Debug.Log("[LocalDemoSkill] caster=" + packet.AttackerObjId + " target=" + packet.TargetId + " skill=" + packet.SkillId + " level=" + packet.SkillLvl);
         IncomingPacketActions.QueueWorld(apply => apply.MagicSkillUse(packet));
    }
}
