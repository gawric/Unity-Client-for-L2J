using System;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class L2Slot
{
    public enum SlotType
    {
        Other,
        Inventory,
        Enchant,
        Multisell,
        Recipe,
        RecipeCraftItem,
        InventoryBis, //other tab slots
        Gear,
        Skill,
        SkillBar,
        BuffPanel,
        SkillWindow,
        Action,
        Trash,
        PriceBuy,
        PriceSell,
        Trade
    }

    [SerializeField] protected int _id;
    protected int _level;
    [SerializeField] protected int _position;
    [SerializeField] protected SlotType _slotType;
    protected string _name;
    protected string _description;
    protected string _icon;
    protected int _enchantLevel;
    protected VisualElement _slotElement;
    protected VisualElement _slotBg;
    protected VisualElement _slotDisabled;
    protected TooltipManipulator _tooltipManipulator;
    protected SlotHoverDetectManipulator _hoverManipulator;

    public int EnchantLevel { get { return _enchantLevel; } set { _enchantLevel = value; } }
    public int Id { get { return _id; } set { _id = value; } }

    public int Level { get { return _level; } set { _level = value; } }
    public int Position { get { return _position; } set { _position = value; } }
    public SlotType Type { get { return _slotType; } set { _slotType = value; } }
    public string Name { get { return _name; } set { _name = value; } }
    public string Description { get { return _description; } set { _description = value; } }
    public string Icon { get { return _icon; } set { _icon = value; } }
    public VisualElement SlotBg { get { return _slotBg; } }
    public VisualElement SlotElement { get { return _slotElement; } }

    public L2Slot(VisualElement slotElement)
    {
        _slotElement = slotElement;
        _slotElement.AddToClassList("dragged");
        _slotBg = _slotElement.Q<VisualElement>(null, "slot-bg");
        _slotDisabled = _slotElement.Q<VisualElement>(null, "slot-disabled");

        _position = -1;
        _id = -1;
        _name = "";
        _description = "";
        _icon = "";
        _slotType = SlotType.Other;
    }

    public L2Slot(VisualElement slotElement, int position, SlotType type)
    {
        _slotElement = slotElement;
        _position = position;
        _slotType = type;

        if (slotElement == null)
        {
            return;
        }

        _slotBg = _slotElement.Q<VisualElement>(null, "slot-bg");
        _slotDisabled = _slotElement.Q<VisualElement>(null, "slot-disabled");

        if (slotElement != null)
        {
            if(type != SlotType.Trash)
            {
                ToolTipManager.GetInstance().RegisterSimple(type, _slotElement, position);
            }
           
        }

        if (_hoverManipulator == null)
        {
            _hoverManipulator = new SlotHoverDetectManipulator(_slotElement, this);
            _slotElement.AddManipulator(_hoverManipulator);
        }

    }

    public virtual void ClearManipulators()
    {
        if (_slotElement == null)
        {
            return;
        }

        if (_tooltipManipulator != null)
        {
            _tooltipManipulator.Clear();
            _slotElement.RemoveManipulator(_tooltipManipulator);
            _tooltipManipulator = null;
        }

        if (_hoverManipulator == null)
        {
            _slotElement.RemoveManipulator(_hoverManipulator);
        }
    }
}
