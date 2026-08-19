using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HennaInfoDto : IWireDto
{
    

    public void ReadFrom(PacketReader reader)
    {
        byte henInt = reader.ReadB(); // equip INT
        byte henStr = reader.ReadB(); // equip Str
        byte henCon = reader.ReadB(); // equip Con
        byte henMen = reader.ReadB(); // equip Men
        byte henDex = reader.ReadB(); // equip Dex
        byte henWit = reader.ReadB(); // equip Wit
        int slots = reader.ReadI();
        int size = reader.ReadI();
        for(int i = 0; i < size; i++)
        {
            int dyeId = reader.ReadI();
            int count = reader.ReadI();
        }
    }
}
