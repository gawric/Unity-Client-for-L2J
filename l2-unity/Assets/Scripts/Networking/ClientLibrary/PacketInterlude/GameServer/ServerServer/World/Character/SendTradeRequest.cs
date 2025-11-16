
public class SendTradeRequest : ServerPacket
{

    private int _senderId;

    public int SenderId { get { return _senderId; } }

    public SendTradeRequest(byte[] d) 
        : base(d)
    {}

    public override void Parse()
    {
        _senderId = ReadI();
    }
}
