using System;
using UnityEngine;

public class SimpleToolTipData : AbstractSimpleToolTip , IDataTips
{
    private bool _isDetectedShortCut;
    public SimpleToolTipData(object data)
    {
        InitializeToolTipData(data);
    }

    private void InitializeToolTipData(object data)
    {
        switch (data)
        {
            case Product product:
                _product = product;
                _name = _product.GetName();
                _description = _product.GetDescription();
                _item_description = _product.GetItemDescription();
                _enchant = 0;
                break;

            case ItemInstance itemInstance:
                _itemInstance = itemInstance;
                _name = _itemInstance.GetName();
                _description = _itemInstance.GetDescription();
                _item_description = _itemInstance.GetItemDescription();
                _enchant = _itemInstance.EnchantLevel;
                break;

            case SkillInstance skillInstance:
                _skillInstance = skillInstance;
                _name = skillInstance.GetName();
                _description = skillInstance.GetDescription();
                _item_description = skillInstance.GetDescription();
                _enchant = skillInstance.Level;
                break;
            case ActionData action:
                _actionInstance = action;
                _name = _actionInstance.GetName();
                _description = _actionInstance.GetDescription();
                _item_description = _actionInstance.GetDescription();
                _enchant = _actionInstance.Level;
                break;

            case Shortcut shortcut:
                if (!_isDetectedShortCut)
                {
                    _isDetectedShortCut = true;
                    object insideData = ShortcutUtils.DetectedType(shortcut);
                    if (insideData != null)
                    {
                        InitializeToolTipData(insideData);
                    }
                }
                break;

            default:
                Debug.LogWarning($"Unsupported data type: {data?.GetType().Name}");
                break;
        }
    }



}
