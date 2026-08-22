[OutgoingCommandPacket(typeof(RequestJoinPledgeCommand))]
public sealed class RequestJoinPledge : OutgoingWirePacket<RequestJoinPledgeDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestJoinPledge;

    public RequestJoinPledge(RequestJoinPledgeCommand command) : this(command.ObjectId) { }

    public RequestJoinPledge(int objectId)
    {
        Dto.ObjectId = objectId;
    }
}
