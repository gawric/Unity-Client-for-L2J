using UnityEngine;

public class RequestOustPledgeMember : ClientPacket
{
    public RequestOustPledgeMember(string memberName) : base((byte)GameInterludeClientPacketType.RequestOustPledgeMember)
    {
        WriteOtherS(memberName);
        BuildPacket();
    }
}
