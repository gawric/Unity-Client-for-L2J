[OutgoingCommandPacket(typeof(RequestShortCutDelCommand))]
public sealed class RequestShortCutDel : OutgoingWirePacket<RequestShortCutDelDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestShortCutDel;

    public RequestShortCutDel(RequestShortCutDelCommand command) : this(command.WorldSlot) { }

    public RequestShortCutDel(int worldSlot)
    {
        Dto.WorldSlot = worldSlot;
    }
}
