using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCreate : ClientPacket
{
    public CharacterCreate(string name , int female , int hairStyle , int hairColor , int face , PlayerTemplates template) : base((byte)GameInterludeClientPacketType.CharacterCreate)
    {
        WriteSOther(name);
        WriteChar((char)0);
        WriteI(template.Race);
        WriteI(female);
        WriteI(template._classId);
        WriteI(template.Base_int);
        WriteI(template.Base_str);
        WriteI(template.Base_con);
        WriteI(template.Base_men);
        WriteI(template.Base_dex);
        WriteI(template.Base_wit);
        WriteI(hairStyle);
        WriteI(hairColor);
        WriteI(face);
        BuildPacketManualType((byte)GameInterludeClientPacketType.CharacterCreate);
    }
}
