using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class ShortcutUtils 
{
    public static object DetectedType(Shortcut data)
    {

        if (data != null)
        {
            switch (data.Type)
            {
                case Shortcut.TYPE_SKILL:
                    return new SkillInstance(data.Id, data.Level, false, false);
                case Shortcut.TYPE_ACTION:
                    return ActionNameTable.Instance.GetAction((ActionType)data.Id);
                case Shortcut.TYPE_ITEM:
                    return PlayerInventory.Instance.FindByAllInventory(data.Id);
                default:
                    Debug.LogWarning($"SimpleToolTipData:> DetectedType:  Unsupported shortcut type: {data.Type}");
                    return null;
            }
        }

        return null;
    }
}
