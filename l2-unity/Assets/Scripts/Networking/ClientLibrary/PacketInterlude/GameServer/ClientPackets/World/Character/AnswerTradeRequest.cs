public class AnswerTradeRequest : ClientPacket
{
    private int _response;

    public AnswerTradeRequest(int response) 
        : base((byte)GameInterludeClientPacketType.AnswerTradeRequest)
    {
        _response = response;
        WriteI(_response);
        BuildPacket();
    }
}

