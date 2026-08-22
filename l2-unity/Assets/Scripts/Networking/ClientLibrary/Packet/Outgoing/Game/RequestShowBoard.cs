[OutgoingCommandPacket(typeof(RequestShowBoardCommand))]
public sealed class RequestShowBoard : OutgoingWirePacket<RequestShowBoardDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestShowBoard;
}
