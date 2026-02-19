using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CharacterSelect : ClientPacket
{
    public CharacterSelect(int slot) : base((byte)GameInterludeClientPacketType.CharacterSelect)
    {
        WriteI(slot);
        WriteShort(0);
        WriteI(slot);
        WriteI(slot);
        WriteI(slot);

        BuildPacket();
    }
}