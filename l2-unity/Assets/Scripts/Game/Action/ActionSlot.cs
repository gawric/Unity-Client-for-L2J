using System;
using UnityEngine.UIElements;

public class ActionSlot : L2DraggableSlot
{
    public int ActionId { get; private set; }
    public ActionData Action { get; private set; }

    public ActionSlot(VisualElement slotElement, int position, SlotType slotType) : base(position, slotElement, slotType, true, false)
    {
        _slotElement = slotElement;
        _position = position;
    }

    protected override void HandleLeftClick()
    {
        PlayerActions.Instance.UseAction((ActionType)ActionId);
    }

    protected override void HandleRightClick()
    {
    }

    protected override void HandleMiddleClick()
    {
    }

    public void AssignAction(ActionType actionType)
    {
        ButtonClickSoundManipulator _buttonClickSoundManipulator = new ButtonClickSoundManipulator(_slotElement);
        _slotDragManipulator.enabled = true;
        _slotElement.RemoveFromClassList("empty");

        ActionId = (int)actionType;
        Action = ActionNameTable.Instance.GetAction(actionType);
        var icon = IconManager.Instance.LoadTextureByName(ConvertIcon(actionType));
        _slotBg.style.backgroundImage = icon;

        if (_tooltipManipulator != null)
        {
            _tooltipManipulator.SetText(Action.Name);
        }
    }

    private string ConvertIcon(ActionType actionType)
    {
        int id = (int)actionType;

        if (id == 0)
        {
            return "action001";
        }else if (id == 1)
        {
            return "action002";
        }
        else if (id == 2)
        {
            return "action003";
        }
        else if (id == 4)
        {
            return "action005";
        }
        else if (id == 5)
        {
            return "action007";
        }
        else if (id == 6)
        {
            return "action008";
        }

        return ActionNameTable.Instance.GetAction(actionType)._icon;
    }
}