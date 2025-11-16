using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

public class ShortCutInit : ServerPacket
{
    private List<Shortcut> _shortcut;
    public ShortCutInit(byte[] d) : base(d)
    {
        _shortcut = new List<Shortcut>();
        Parse();
    }

    public List<Shortcut> ShortCuts { get => _shortcut; }

    //L2j Enum type
    //NONE,
    //ITEM,
    //SKILL,
    //ACTION,
    //MACRO,
    //RECIPE,
    public override void Parse()
    {
        int size = ReadI();
        Shortcut shortcut;

        for (int i = 0; i < size; i++)
        {
            int type = ReadI();
            int world_slot = ReadI();

            int slot = world_slot % 12;
            int page = world_slot / 12;

            if (type == Shortcut.TYPE_ITEM)
            {
                int itemCutId = ReadI();
                ReadI();
                ReadI();
                ReadI();
                ReadI();
                ReadSh();
                ReadSh();
                shortcut = new Shortcut(slot, page, Shortcut.TYPE_ITEM, itemCutId, 0);
                _shortcut.Add(shortcut);

            }
            else if (type == Shortcut.TYPE_SKILL)
            {
                int itemCutId = ReadI();
                int level = ReadI();
                //shortcut = new Shortcut(shortCutId, level);
                ReadB();// C5
                ReadI();// C6
                shortcut = new Shortcut(slot, page, Shortcut.TYPE_SKILL, itemCutId, level);
                _shortcut.Add(shortcut);
            }
            else if (type == Shortcut.TYPE_ACTION)
            {
                int actionId = ReadI();
                _shortcut.Add(new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0));
                ReadI();// C6
            }
            else if (type == Shortcut.TYPE_MACRO)
            {
                //int macroCutId = ReadI();
                //shortcut = new Shortcut(slot, page, Shortcut., macroCutId, 0);
                ReadI();// C6
            }
            else if (type == Shortcut.TYPE_RECIPE)
            {
                int shortCutId = ReadI();
                ReadI();// C6
            }

        }
    }

    private int ParcePages(int slot)
    {
        if(slot >= 11)
        {
            return 1;
        }else if(slot > 12 & slot <= 22)
        {
            return 2;
        }
        else if(slot > 22 & slot <= 33)
        {
            return 3;
        }
        return 1;
    }

    private int ConvertWorldSlot(int world_slot , int page)
    {
        if (page == 1)
        {
            return world_slot;
        }
        else
        {
            int all_slot = page * 11;
            return all_slot - world_slot;
        }
    }

}
