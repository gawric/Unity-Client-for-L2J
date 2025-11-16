public class RequestAnswerJoinParty : ClientPacket
{
    private int _response;
    public RequestAnswerJoinParty(int response) : base((byte)GameInterludeClientPacketType.RequestAnswerJoinParty)
    {
        _response = response;
        WriteI(_response);
        BuildPacket();
    }
}

