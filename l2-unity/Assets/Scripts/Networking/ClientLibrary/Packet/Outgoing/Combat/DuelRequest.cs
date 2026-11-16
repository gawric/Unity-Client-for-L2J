[OutgoingCommandPacket(typeof(RequestDuelStartCommand))]
public sealed class RequestDuelStart : OutgoingWirePacket<RequestDuelStartDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.Extender;
    public RequestDuelStart(RequestDuelStartCommand command)
    {
        Dto.Name = command.Name;
        Dto.Party = command.Party;
    }
}

[OutgoingCommandPacket(typeof(RequestDuelAnswerStartCommand))]
public sealed class RequestDuelAnswerStart : OutgoingWirePacket<RequestDuelAnswerStartDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.Extender;
    public RequestDuelAnswerStart(RequestDuelAnswerStartCommand command)
    {
        Dto.Party = command.Party;
        Dto.Accepted = command.Accepted;
    }
}

[OutgoingCommandPacket(typeof(RequestDuelSurrenderCommand))]
public sealed class RequestDuelSurrender : OutgoingWirePacket<RequestDuelSurrenderDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.Extender;
    public RequestDuelSurrender(RequestDuelSurrenderCommand command) { }
}
