using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HennaInfo : ServerPacket
{
    public HennaInfo(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        byte henInt = ReadB(); // equip INT
        byte henStr = ReadB(); // equip Str
        byte henCon = ReadB(); // equip Con
        byte henMen = ReadB(); // equip Men
        byte henDex = ReadB(); // equip Dex
        byte henWit = ReadB(); // equip Wit
        int slots = ReadI();
        int size = ReadI();
        for(int i = 0; i < size; i++)
        {
            int dyeId = ReadI();
            int count = ReadI();
        }
    }
}
