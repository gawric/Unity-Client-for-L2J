using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillCoolTimeDto : IWireDto
{
  
    

    public void ReadFrom(PacketReader reader)
    {
        int size = reader.ReadI();
        for(int i = 0; i < size; i++)
        {
            int skillId = reader.ReadI();
            int skillLevel = reader.ReadI();
            int reuse = reader.ReadI();
            int remaining = reader.ReadI();
        }
    }
}
