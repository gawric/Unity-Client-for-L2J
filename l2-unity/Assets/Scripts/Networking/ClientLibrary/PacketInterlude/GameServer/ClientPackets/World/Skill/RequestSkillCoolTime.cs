using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestSkillCoolTime : ClientPacket
{
    public RequestSkillCoolTime() : base((byte)GameInterludeClientPacketType.RequestSkillCoolTime)
    {
        BuildPacket();
    }
}
