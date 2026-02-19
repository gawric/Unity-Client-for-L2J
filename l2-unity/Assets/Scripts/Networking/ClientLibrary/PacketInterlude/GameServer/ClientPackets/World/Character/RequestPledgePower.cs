using System.Collections.Generic;
using UnityEngine;

public class RequestPledgePower : ClientPacket
{
    public RequestPledgePower(int rank , int action , int privs) : base((byte)GameInterludeClientPacketType.RequestPledgePower)
    {
        WriteI(rank);
        WriteI(action);
        WriteI(privs);

        BuildPacket();
    }
}
