[OutgoingCommandPacket(typeof(RequestAnswerJoinPartyCommand))]
public sealed class RequestAnswerJoinParty : OutgoingWirePacket<RequestAnswerJoinPartyDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestAnswerJoinParty;

    public RequestAnswerJoinParty(RequestAnswerJoinPartyCommand command) : this(command.Response) { }

    public RequestAnswerJoinParty(int response)
    {
        Dto.Response = response;
    }
}
