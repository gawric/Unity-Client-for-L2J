using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LoginServerPacketType : byte
{
    Init = 0,
    GGAuth = 11,
    LoginFail = 1,
    LoginOk = 3,
    ServerList = 4,
    PlayOk = 7,
}
