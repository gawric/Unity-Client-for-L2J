using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtocolVersion : ClientPacket
{
    public ProtocolVersion(int _protocol) : base((byte)GameInterludeClientPacketType.ProtocolVersion)
    {
        WriteI(_protocol);
        BuildPacket();
    }
}
