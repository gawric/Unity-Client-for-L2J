using UnityEngine;

public class RequestRecipeBookOpen : ClientPacket
{
    public RequestRecipeBookOpen(int isDwarven) : base((byte)GameInterludeClientPacketType.RequestRecipeBookOpen)
    {
        WriteI(isDwarven);
        BuildPacket();
    }
}
