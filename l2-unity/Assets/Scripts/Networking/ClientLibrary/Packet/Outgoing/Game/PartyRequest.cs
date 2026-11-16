[OutgoingCommandPacket(typeof(RequestJoinPartyCommand))]
public sealed class RequestJoinParty : OutgoingWirePacket<RequestJoinPartyDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestJoinParty;

    public RequestJoinParty(RequestJoinPartyCommand command)
    {
        Dto.Name = command.Name;
        Dto.DistributionType = command.DistributionType;
    }
}

[OutgoingCommandPacket(typeof(RequestWithDrawalPartyCommand))]
public sealed class RequestWithDrawalParty : OutgoingWirePacket<RequestWithDrawalPartyDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestWithDrawalParty;

    public RequestWithDrawalParty(RequestWithDrawalPartyCommand command) { }
}
