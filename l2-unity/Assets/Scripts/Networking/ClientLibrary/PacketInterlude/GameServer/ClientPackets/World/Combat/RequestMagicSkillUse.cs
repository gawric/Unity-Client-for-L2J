using UnityEngine;

public class RequestMagicSkillUse : ClientPacket
{
    public RequestMagicSkillUse(int skillId , int ctrlPressed , byte shiftPressed) : base((byte)GameInterludeClientPacketType.RequestMagicSkillUse)
    {

        WriteI(skillId);// Identifier of the used skill
        WriteI(ctrlPressed);// True if it's a ForceAttack : Ctrl pressed
        WriteB(shiftPressed);// True if Shift pressed

        BuildPacket();
    }
}
