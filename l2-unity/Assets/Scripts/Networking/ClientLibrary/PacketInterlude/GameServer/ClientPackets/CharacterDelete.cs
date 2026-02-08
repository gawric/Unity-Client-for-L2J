using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CharacterDelete : ClientPacket
{
    public CharacterDelete(int slot) : base((byte)GameInterludeClientPacketType.CharacterDelete)
    {
        WriteI(slot);
        BuildPacket();
    }
}