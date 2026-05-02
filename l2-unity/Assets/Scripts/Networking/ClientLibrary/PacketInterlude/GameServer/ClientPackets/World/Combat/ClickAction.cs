using UnityEngine;

public class ClickAction : ClientPacket
{
    public ClickAction(int objectId, int originX, int originY, int originZ , int actionId) : base((byte)GameInterludeClientPacketType.Action)
    {

        WriteI(objectId);// Target object Identifier
        WriteI(originX);
        WriteI(originY);
        WriteI(originZ);
        WriteB((byte)actionId);// Action identifier : 0-Simple click, 1-Shift click

        BuildPacket();
    }
}
