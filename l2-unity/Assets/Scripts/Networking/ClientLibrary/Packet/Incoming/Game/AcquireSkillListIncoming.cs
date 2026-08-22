using UnityEngine;

[IncomingGamePacket(GameServerPacketType.AcquireSkillList)]
public sealed class AcquireSkillListIncoming : IncomingWirePacket<AcquireSkillListDto>
{
    public override void Apply(AcquireSkillListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SkillLearn.AddData(packet.AcquireList);
            IncomingPacketActions.SkillLearn.ShowWindow();
        });
    }
}
