
public class SendTradeRequestDto : IWireDto
{

    private int _senderId;

    public int SenderId { get { return _senderId; } }

    

    public void ReadFrom(PacketReader reader)
    {
        _senderId = reader.ReadI();
    }
}
