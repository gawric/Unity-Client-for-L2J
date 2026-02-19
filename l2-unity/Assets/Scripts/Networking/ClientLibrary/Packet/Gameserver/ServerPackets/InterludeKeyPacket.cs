using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterludeKeyPacket : ServerPacket {
    public byte[] BlowFishKey { get; private set; }
    public bool AuthAllowed { get; private set; }

    public int ServerId { get; private set; }
    public bool UseBlowfish { get; private set; }

    public InterludeKeyPacket(byte[] d) : base(d) {
        Parse();
    }

    public override void Parse() {
        int auth  = ReadB();
        ParceAllow(auth);// 0 - wrong protocol, 1 - protocol ok
        byte blowFishKeyLength = 8;//Setting server Interlude
        BlowFishKey = ReadB(blowFishKeyLength);
        ParceBlowFish(ReadI());
        ServerId = ReadI();
        //Debug.Log("InterludeKeyPacket success  !!!!!!!!!!!!!! " + AuthAllowed);
        //Debug.Log("InterludeKeyPacket success  !!!!!!!!!!!!!! auth " + auth);
        //Debug.Log("InterludeKeyPacket success  !!!!!!!!!!!!!! BlowFishKey " + BlowFishKey.ToString());
        //Debug.Log("InterludeKeyPacket success  !!!!!!!!!!!!!! ServerId "  + ServerId);
        //Debug.Log($"BlowFishKey Print  : {StringUtils.ByteArrayToString(BlowFishKey)}");
    }

    private void ParceBlowFish(int result)
    {
        if (result == 0)
        {
            UseBlowfish = false;
        }
        else
        {
            UseBlowfish = true;
        }
    }

    private void ParceAllow(int result)
    {
        if(result == 0)
        {
            AuthAllowed = false;
        }
        else
        {
            AuthAllowed = true;
        }
    }
}
