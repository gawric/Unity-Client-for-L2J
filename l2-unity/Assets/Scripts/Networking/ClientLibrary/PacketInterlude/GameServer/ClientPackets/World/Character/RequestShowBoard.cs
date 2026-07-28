public class RequestShowBoard : ClientPacket
{
    public RequestShowBoard() : base((byte)GameInterludeClientPacketType.RequestShowBoard)
    {
        BuildPacket();
    }
}
