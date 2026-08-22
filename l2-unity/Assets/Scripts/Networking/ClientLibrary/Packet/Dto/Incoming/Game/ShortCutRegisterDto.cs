using System;
using System.Collections.Generic;
using UnityEngine;

public class ShortCutRegisterDto : IWireDto
{
    private Shortcut shortcut;
    
    public Shortcut Shortcut { get => shortcut; }
    public void ReadFrom(PacketReader reader)
    {
        int type = reader.ReadI();
        int world_slot = reader.ReadI();

        int slot = world_slot % 12;
        int page = world_slot / 12;
        //if (page >= 2) page = page - 1;
        if (type == Shortcut.TYPE_ITEM)
        {
            int itemCutId = reader.ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ITEM, itemCutId, 0);
        }
        else if (type == Shortcut.TYPE_SKILL)
        {
            int skillId = reader.ReadI();
            int skilLevel = reader.ReadI();
            int unk1 = reader.ReadB();
            int characterType = reader.ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_SKILL, skillId, skilLevel);
            Debug.Log("ShortCutRegister : не реализовано принятия shortcutskill");
        }
        else if (type == Shortcut.TYPE_ACTION)
        {
            int actionId = reader.ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0);
        }
        else if (type == Shortcut.TYPE_RECIPE)
        {
            int actionId = reader.ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0);
        }
        else if (type == Shortcut.TYPE_MACRO)
        {
            int actionId = reader.ReadI();
            shortcut = new Shortcut(slot, page, Shortcut.TYPE_ACTION, actionId, 0);
        }
    }
}
