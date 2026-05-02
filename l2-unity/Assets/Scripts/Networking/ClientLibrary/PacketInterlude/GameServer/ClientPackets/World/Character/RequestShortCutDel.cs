using UnityEngine;

public class RequestShortCutDel : ClientPacket
{
    public RequestShortCutDel(int world_slot) : base((byte)GameInterludeClientPacketType.RequestShortCutDel)
    {
        //int world_slot = slot + (page * 12);
        WriteI(world_slot);

        BuildPacket();
    }
}
