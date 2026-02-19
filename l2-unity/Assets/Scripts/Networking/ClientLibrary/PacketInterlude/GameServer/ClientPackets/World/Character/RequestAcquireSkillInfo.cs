using UnityEngine;

public class RequestAcquireSkillInfo : ClientPacket
{
    public RequestAcquireSkillInfo(int skillId, int skillLevel, int skillType) : base((byte)GameInterludeClientPacketType.RequestAcquireSkillInfo)
    {

        WriteI(skillId);
        WriteI(skillLevel);
        WriteI(skillType);

        BuildPacket();
    }
}
