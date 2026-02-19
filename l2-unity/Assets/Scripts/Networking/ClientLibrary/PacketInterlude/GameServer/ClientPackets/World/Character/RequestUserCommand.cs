using UnityEngine;

public class RequestUserCommand : ClientPacket
{
    public RequestUserCommand(int idCommand) : base((byte)GameInterludeClientPacketType.BypassUserCmd)
    {
        WriteI(idCommand);
        BuildPacket();
    }
}
