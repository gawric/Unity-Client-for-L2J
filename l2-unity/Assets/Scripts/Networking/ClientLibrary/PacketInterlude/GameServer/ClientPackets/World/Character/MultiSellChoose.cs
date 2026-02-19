using UnityEngine;

public class MultiSellChoose : ClientPacket
{
    public MultiSellChoose(int listId , int entryId , int amount) : base((byte)GameInterludeClientPacketType.MultiSellChoose)
    {
        WriteI(listId);
        WriteI(entryId);
        WriteI(amount);
        BuildPacket();
    }
}
