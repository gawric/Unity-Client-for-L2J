

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class L2ClickableSlot : L2Slot
{
    private bool _leftMouseUp;
    private bool _rightMouseup;
    private SlotType _slotType;
    private const float doubleClickThreshold = 0.3f; //Time Double click event
    private float clickTime;
    public L2ClickableSlot(VisualElement slotElement, int position, SlotType type, bool leftMouseUp, bool rightMouseup) : base(slotElement, position, type)
    {
        _leftMouseUp = leftMouseUp;
        _rightMouseup = rightMouseup;
        _slotType = type;
        RegisterClickableCallback();
    }

   /// public L2ClickableSlot(VisualElement slotElement, int position, SlotType type) : base(slotElement, position,  type)
   //{
        
   // }

    protected void RegisterClickableCallback()
    {
        if (_slotElement == null)
        {
            return;
        }

        _slotElement.RegisterCallback<MouseDownEvent>(HandleSlotClickDown, TrickleDown.TrickleDown);
        _slotElement.RegisterCallback<MouseUpEvent>(HandleSlotClickUp, TrickleDown.TrickleDown);
    }

    public void UnregisterClickableCallback()
    {
        if (_slotElement == null)
        {
            return;
        }

        _slotElement.UnregisterCallback<MouseDownEvent>(HandleSlotClickDown, TrickleDown.TrickleDown);
        _slotElement.UnregisterCallback<MouseUpEvent>(HandleSlotClickUp, TrickleDown.TrickleDown);
    }
    private VisualElement slotFrame;
    public void SetSelected()
    {
        Debug.Log($"Slot {_position} {Id}selected.");

        if (slotFrame == null) slotFrame = _slotElement.Q(className: "slot-frame");

        if (slotFrame != null)
        {
            slotFrame.style.display = DisplayStyle.Flex;
        }

        ToolTipManager.GetInstance().EventLeftClickSlot(_slotElement);
        EventLeftDoubleClick(_slotElement);

    }

    private void EventLeftDoubleClick(VisualElement slotElement)
    {
        if (Time.time - clickTime < doubleClickThreshold)
        {
            switch (_slotType)
            {
                case SlotType.Recipe:
                    RecipeBookWindow.Instance.EventDoubleClick(slotElement);
                    break;
                case SlotType.PriceBuy:
                case SlotType.PriceSell:
                    DealerWindow.Instance.EventDoubleClick(slotElement);
                    break;
            }
        }
        clickTime = Time.time;
    }


    public void UnSelect()
    {
        Debug.Log($"Slot {_position} unselected.");
        if(slotFrame == null ) slotFrame = _slotElement.Q(className: "slot-frame");

        if (slotFrame != null)
        {
            slotFrame.style.display = DisplayStyle.None;
        }
        
    }

    private void HandleSlotClickDown(MouseDownEvent evt)
    {
        if (evt.button == 0)
        {
            SetActive();

            if (!_leftMouseUp)
            {
                HandleLeftClick();
            }
        }
        else if (evt.button == 1)
        {
            if (!_rightMouseup)
            {
                HandleRightClick();
            }
        }
        else
        {
            HandleMiddleClick();
        }
    }

    private void HandleSlotClickUp(MouseUpEvent evt)
    {
        UnsetActive();

        if (!_hoverManipulator.Hovering)
        {
            return;
        }

        if (evt.button == 0)
        {
            if (_leftMouseUp)
            {
                HandleLeftClick();
            }
        }
        else if (evt.button == 1)
        {
            if (_rightMouseup)
            {
                HandleRightClick();
            }
        }
        else
        {
            HandleMiddleClick();
        }
    }

    protected virtual void SetActive()
    {
        _slotElement.AddToClassList("active");
    }

    protected virtual void UnsetActive()
    {
        _slotElement.RemoveFromClassList("active");
    }

    protected virtual void HandleLeftClick() { }
    protected virtual void HandleRightClick() { }
    protected virtual void HandleMiddleClick() { }
}