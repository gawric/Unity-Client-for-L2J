public sealed class RequestMagicSkillUseDto : IOutgoingDto
{
    public int SkillId;
    public int CtrlPressed;
    public byte ShiftPressed;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(SkillId);
        writer.WriteI(CtrlPressed);
        writer.WriteB(ShiftPressed);
    }
}
