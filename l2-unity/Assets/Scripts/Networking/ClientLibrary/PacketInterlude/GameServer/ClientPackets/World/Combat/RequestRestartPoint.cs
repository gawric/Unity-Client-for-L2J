public class RequestRestartPoint : ClientPacket
{
    //_requestedPointType = 0 client
    //_requestedPointType = 1 // to clanhall
    //_requestedPointType = 2 // to castle
    //_requestedPointType = 3 // to siege HQ
    //_requestedPointType = 4 // Fixed or Player is a festival participant
    //_requestedPointType = 5 // TODO: agathion ress
    //_requestedPointType = 5 // // to jail

    public RequestRestartPoint() : base((byte)GameInterludeClientPacketType.RequestRestartPoint)
    {

        WriteI(0);

        BuildPacket();
    }

}
