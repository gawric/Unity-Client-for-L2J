[OutgoingCommandPacket(typeof(RequestWithdrawPledgeCommand))]
public sealed class RequestWithdrawPledge : OutgoingWirePacket<RequestWithdrawPledgeDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestWithdrawalPledge;
}
