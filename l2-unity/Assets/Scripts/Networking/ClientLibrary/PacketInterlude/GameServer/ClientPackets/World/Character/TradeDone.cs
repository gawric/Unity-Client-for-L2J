
public class TradeDone : ClientPacket
{
    private int _response;

    public TradeDone(int response) : base((byte)GameInterludeClientPacketType.TradeDone)
    {
        _response = response;
        WriteI(_response);
        BuildPacket();
    }
}
