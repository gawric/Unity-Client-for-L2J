using UnityEngine;

[IncomingGamePacket(GameServerPacketType.AcquireSkillInfo)]
public sealed class AcquireSkillInfoIncoming : IncomingWirePacket<AcquireSkillInfoDto>
{
    public override void Apply(AcquireSkillInfoDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SkillLearn.HideWindow();
            IncomingPacketActions.SkillDesc.AddData(packet);
            IncomingPacketActions.SkillDesc.ShowWindow();
        });
    }
}
