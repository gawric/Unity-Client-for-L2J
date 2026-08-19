[OutgoingCommandPacket(typeof(RequestAcquireSkillInfoCommand))]
public sealed class RequestAcquireSkillInfo : OutgoingWirePacket<RequestAcquireSkillInfoDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestAcquireSkillInfo;

    public RequestAcquireSkillInfo(RequestAcquireSkillInfoCommand command) : this(command.SkillId, command.SkillLevel, command.SkillType) { }

    public RequestAcquireSkillInfo(int skillId, int skillLevel, int skillType)
    {
        Dto.SkillId = skillId;
        Dto.SkillLevel = skillLevel;
        Dto.SkillType = skillType;
    }
}
