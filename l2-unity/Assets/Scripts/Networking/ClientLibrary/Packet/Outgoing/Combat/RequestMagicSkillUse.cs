[OutgoingCommandPacket(typeof(RequestMagicSkillUseCommand))]
public sealed class RequestMagicSkillUse : OutgoingWirePacket<RequestMagicSkillUseDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestMagicSkillUse;

    public RequestMagicSkillUse(RequestMagicSkillUseCommand command) : this(command.SkillId, command.CtrlPressed, command.ShiftPressed) { }

    public RequestMagicSkillUse(int skillId, int ctrlPressed, byte shiftPressed)
    {
        Dto.SkillId = skillId;
        Dto.CtrlPressed = ctrlPressed;
        Dto.ShiftPressed = shiftPressed;
    }
}
