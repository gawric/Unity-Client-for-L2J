using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LoginInterludeClientPacketTYpe : byte
{
    AuthGameGuard = 0x07,
    RequestAuthLogin = 0x00,
    RequestServerList = 0x05,
    ProtocolVersion = 0x00,
    RequestServerLogin = 0x02,
}
