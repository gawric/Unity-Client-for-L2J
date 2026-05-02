using System;
using System.Collections.Generic;
using UnityEngine;

public class ShortCutRegister : ServerPacket
{
    private Shortcut shortcut;
    public ShortCutRegister(byte[] d) : base(d)
    {
        Parse();
    }
    public Shortcut Shortcut { get => shortcut; }
    public override void Parse()
    {
        int type = ReadI();
        int world_slot = ReadI();

        int slot = world_slot % 12;
        int page = world_slot / 12;
        //if (page >= 2) page = page - 1;
        if (type == Shortcut.TYPE_ITEM)
        {
            int itemCutId = ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ITEM, itemCutId, 0);
        }
        else if (type == Shortcut.TYPE_SKILL)
        {
            int skillId = ReadI();
            int skilLevel = ReadI();
            int unk1 = ReadB();
            int characterType = ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_SKILL, skillId, skilLevel);
            Debug.Log("ShortCutRegister : не реализовано принятия shortcutskill");
        }
        else if (type == Shortcut.TYPE_ACTION)
        {
            int actionId = ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0);
        }
        else if (type == Shortcut.TYPE_RECIPE)
        {
            int actionId = ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0);
        }
        else if (type == Shortcut.TYPE_MACRO)
        {
            int actionId = ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0);
        }
    }
}
