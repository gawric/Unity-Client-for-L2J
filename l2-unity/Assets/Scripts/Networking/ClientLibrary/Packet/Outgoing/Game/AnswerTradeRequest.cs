[OutgoingCommandPacket(typeof(AnswerTradeRequestCommand))]
public sealed class AnswerTradeRequest : OutgoingWirePacket<AnswerTradeRequestDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.AnswerTradeRequest;

    public AnswerTradeRequest(AnswerTradeRequestCommand command) : this(command.Response) { }

    public AnswerTradeRequest(int response)
    {
        Dto.Response = response;
    }
}
