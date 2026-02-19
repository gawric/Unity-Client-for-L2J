using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillCoolTime : ServerPacket
{
  
    public SkillCoolTime(byte[] d) : base(d)
    {
 
        Parse();
    }

    public override void Parse()
    {
        int size = ReadI();
        for(int i = 0; i < size; i++)
        {
            int skillId = ReadI();
            int skillLevel = ReadI();
            int reuse = ReadI();
            int remaining = ReadI();
        }
    }
}
