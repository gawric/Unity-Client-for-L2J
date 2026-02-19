using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MacroList :ServerPacket
{
    public MacroList(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        int rev = ReadI();// macro change revision (changes after each macro edition)
        byte unknow = ReadB();
        int size = ReadB();// count of Macros
        byte unknow2 = ReadB();
        if(size > 0)
        {
            int macroId = ReadI(); // Macro ID
            string macroName = ReadOtherS();
            string desc = ReadOtherS();
            string acronym = ReadOtherS();
            byte icon = ReadB();
            int countSub = ReadB();

            for(int i = 0; i < countSub; i++)
            {
                byte commandCount = ReadB();
                byte type = ReadB(); //// type 1 = skill, 3 = action, 4 = shortcut
                int skillId = ReadI();
                byte shortCutId = ReadB();
                string commandName = ReadOtherS();
            }
        }
    }
}
