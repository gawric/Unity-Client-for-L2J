using UnityEngine;

public class RequestAcquireSkill : ClientPacket
{
    public RequestAcquireSkill(int skillId, int skillLevel, int skillType) : base((byte)GameInterludeClientPacketType.RequestAcquireSkill)
    {

        WriteI(skillId);
        WriteI(skillLevel);
        WriteI(skillType);

        BuildPacket();
    }
}
