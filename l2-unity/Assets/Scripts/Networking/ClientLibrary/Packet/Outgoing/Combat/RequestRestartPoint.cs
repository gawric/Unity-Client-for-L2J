[OutgoingCommandPacket(typeof(RequestRestartPointCommand))]
public sealed class RequestRestartPoint : OutgoingWirePacket<RequestRestartPointDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestRestartPoint;

    public RequestRestartPoint()
    {
        Dto.PointType = 0;
    }
}
