[OutgoingCommandPacket(typeof(RequestShortCutRegCommand))]
public sealed class RequestShortCutReg : OutgoingWirePacket<RequestShortCutRegDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestShortCutReg;

    public RequestShortCutReg(RequestShortCutRegCommand command) : this(command.TypeId, command.WorldSlot, command.Id, command.Level) { }

    public RequestShortCutReg(int typeId, int worldSlot, int id, int level)
    {
        Dto.TypeId = typeId;
        Dto.WorldSlot = worldSlot;
        Dto.Id = id;
        Dto.Level = level;
    }
}
