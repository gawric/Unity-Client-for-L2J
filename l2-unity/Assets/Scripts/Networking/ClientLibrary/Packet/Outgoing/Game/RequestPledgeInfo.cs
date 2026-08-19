[OutgoingCommandPacket(typeof(RequestPledgeInfoCommand))]
public sealed class RequestPledgeInfo : OutgoingWirePacket<RequestPledgeInfoDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestPledgeInfo;

    public RequestPledgeInfo(RequestPledgeInfoCommand command) : this(command.ClanId) { }

    public RequestPledgeInfo(int clanId)
    {
        Dto.ClanId = clanId;
    }
}
