[OutgoingCommandPacket(typeof(RequestTargetCanceldCommand))]
public sealed class RequestTargetCanceld : OutgoingWirePacket<RequestTargetCanceldDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestTargetCanceld;

    public RequestTargetCanceld()
    {

    }
}
