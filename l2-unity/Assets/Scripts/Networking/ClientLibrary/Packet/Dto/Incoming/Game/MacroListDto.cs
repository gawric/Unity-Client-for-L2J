using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MacroListDto : IWireDto
{
    

    public void ReadFrom(PacketReader reader)
    {
        int rev = reader.ReadI();// macro change revision (changes after each macro edition)
        byte unknow = reader.ReadB();
        int size = reader.ReadB();// count of Macros
        byte unknow2 = reader.ReadB();
        if(size > 0)
        {
            int macroId = reader.ReadI(); // Macro ID
            string macroName = reader.ReadOtherS();
            string desc = reader.ReadOtherS();
            string acronym = reader.ReadOtherS();
            byte icon = reader.ReadB();
            int countSub = reader.ReadB();

            for(int i = 0; i < countSub; i++)
            {
                byte commandCount = reader.ReadB();
                byte type = reader.ReadB(); //// type 1 = skill, 3 = action, 4 = shortcut
                int skillId = reader.ReadI();
                byte shortCutId = reader.ReadB();
                string commandName = reader.ReadOtherS();
            }
        }
    }
}
