using UnityEngine;

public class RequestSay2 : ClientPacket
{
    public RequestSay2(ChatTypeData data, string message, string name) : base((byte)GameInterludeClientPacketType.Say2)
    {
        WriteOtherS(message);
        WriteI(data.Type);//chat type - general-party etc
        WriteOtherS(name);
        BuildPacket();
    }
}
