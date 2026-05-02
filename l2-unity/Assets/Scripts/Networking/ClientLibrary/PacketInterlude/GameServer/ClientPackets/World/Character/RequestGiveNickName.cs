using UnityEngine;

public class RequestGiveNickName : ClientPacket
{
    public RequestGiveNickName(string memberName , string title) : base((byte)GameInterludeClientPacketType.RequestGiveNickName)
    {
        WriteOtherS(memberName);
        WriteOtherS(title);
        BuildPacket();
    }
}
