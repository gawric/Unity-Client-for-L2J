using UnityEngine;

public class RequestQuestAbort : ClientPacket
{
    public RequestQuestAbort(int questId) : base((byte)GameInterludeClientPacketType.RequestQuestAbort)
    {
        WriteI(questId);
        BuildPacket();
    }
}
