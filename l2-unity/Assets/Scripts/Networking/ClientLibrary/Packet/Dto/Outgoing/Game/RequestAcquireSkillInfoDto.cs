public sealed class RequestAcquireSkillInfoDto : IOutgoingDto
{
    public int SkillId;
    public int SkillLevel;
    public int SkillType;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(SkillId);
        writer.WriteI(SkillLevel);
        writer.WriteI(SkillType);
    }
}
