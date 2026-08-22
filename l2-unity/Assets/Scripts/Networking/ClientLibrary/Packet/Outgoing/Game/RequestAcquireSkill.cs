[OutgoingCommandPacket(typeof(RequestAcquireSkillCommand))]
public sealed class RequestAcquireSkill : OutgoingWirePacket<RequestAcquireSkillDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestAcquireSkill;

    public RequestAcquireSkill(RequestAcquireSkillCommand command) : this(command.SkillId, command.SkillLevel, command.SkillType) { }

    public RequestAcquireSkill(int skillId, int skillLevel, int skillType)
    {
        Dto.SkillId = skillId;
        Dto.SkillLevel = skillLevel;
        Dto.SkillType = skillType;
    }
}
