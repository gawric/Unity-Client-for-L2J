using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class SkillbarSlot : L2ClickableSlot
{
    private ButtonClickSoundManipulator _buttonClickSoundManipulator;
    private L2Slot _innerSlot;
    private Shortcut _shortcut;
    private int _skillbarId;
    private int _slot;
    private VisualElement _keyElement;
    private VisualElement _reuseElement;
    private VisualElement _rechargeMaskBg;

    public SkillbarSlot(VisualElement slotElement, int position, int skillbarId, int slot) : base(slotElement, position, SlotType.SkillBar, true, false)
    {
        _slotElement = slotElement;
        _position = position;
        _skillbarId = skillbarId;
        _slot = slot;
        _keyElement = _slotElement.Q<VisualElement>("Key");
        _reuseElement = _slotElement.Q<VisualElement>("ReuseBg");
        _rechargeMaskBg = _slotElement.Q<VisualElement>("RechargeMaskBg");
    }


    public VisualElement GetReuseElement()
    {
        return _reuseElement;
    }


    public VisualElement GetRechargeMaskElement()
    {
        return _rechargeMaskBg;
    }

    public Shortcut Shortcut
    {
        get { return _shortcut; }
    }

    public void AssignShortcut(Shortcut shortcut)
    {
        ClearManipulators();

        _shortcut = shortcut;
        _buttonClickSoundManipulator = new ButtonClickSoundManipulator(_slotElement);

        switch (shortcut.Type)
        {
            case Shortcut.TYPE_ACTION:
                AssignAction(shortcut.Id);
                break;
            case Shortcut.TYPE_ITEM:
                AssignItem(shortcut.Id);
                break;
            case Shortcut.TYPE_MACRO:
                break;
            case Shortcut.TYPE_RECIPE:
                break;
            case Shortcut.TYPE_SKILL:
                AssignSkill(shortcut.Id , shortcut.Level);
                break;
        }
    }

    public void AssignItem(int objectId)
    {
        ItemInstance item = PlayerInventory.Instance.FindByAllInventory(objectId);
        _innerSlot = new InventorySlot(_position, _slotElement, SlotType.SkillBar);
        ((InventorySlot)_innerSlot).AssignItem(item);
        ((L2ClickableSlot)_innerSlot).UnregisterClickableCallback();

        UpdateInputInfo();
    }

    public void AssignSkill(int objectId , int level)
    {
       
        _innerSlot = new SkillSlot(_slotElement, _position, SlotType.SkillBar);
        ((SkillSlot)_innerSlot).AssignSkill(objectId , level , false);
        ((L2ClickableSlot)_innerSlot).UnregisterClickableCallback();

        UpdateInputInfo();
    }

    public void AssignAction(int objectId)
    {
        _innerSlot = new ActionSlot(_slotElement, _position, SlotType.SkillBar);
        ((ActionSlot)_innerSlot).AssignAction((ActionType)objectId);
        ((L2ClickableSlot)_innerSlot).UnregisterClickableCallback();

        UpdateInputInfo();
    }

    private void UpdateInputInfo()
    {
        _slotElement.RemoveFromClassList("empty");

        string key = PlayerShortcuts.Instance.GetKeybindForShortcut(_skillbarId, _slot);

        //Debug.Log("UpdateInputInfo поиску по KeyImageTable");
        Texture2D inputTexture = KeyImageTable.Instance.LoadTextureByKey(key);

        if (inputTexture != null)
        {
            _keyElement.style.backgroundImage = inputTexture;
            _keyElement.style.width = inputTexture.width;
        }
    }

   





    public override void ClearManipulators()
    {
        base.ClearManipulators();

        if (_buttonClickSoundManipulator != null)
        {
            _slotElement.RemoveManipulator(_buttonClickSoundManipulator);
            _buttonClickSoundManipulator = null;
        }
    }

    protected override void HandleLeftClick()
    {
        if (_shortcut != null)
        {
            Debug.LogWarning($"Use bar slot {_position}.");
            PlayerShortcuts.Instance.UseShortcut(_shortcut);
        }
    }

    protected override void HandleRightClick()
    {
    }

    protected override void HandleMiddleClick()
    {
    }
}