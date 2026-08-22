[OutgoingCommandPacket(typeof(RequestPledgePowerCommand))]
public sealed class RequestPledgePower : OutgoingWirePacket<RequestPledgePowerDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestPledgePower;

    public RequestPledgePower(RequestPledgePowerCommand command) : this(command.Rank, command.Action, command.Privs) { }

    public RequestPledgePower(int rank, int action, int privs)
    {
        Dto.Rank = rank;
        Dto.Action = action;
        Dto.Privs = privs;
    }
}
