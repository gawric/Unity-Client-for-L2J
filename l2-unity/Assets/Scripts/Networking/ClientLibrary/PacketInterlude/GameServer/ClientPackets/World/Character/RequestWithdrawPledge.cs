using UnityEngine;

public class RequestWithdrawPledge : ClientPacket
{
    public RequestWithdrawPledge() : base((byte)GameInterludeClientPacketType.RequestWithdrawalPledge)
    {

       // WriteI(0);
        BuildPacket();
    }
}
