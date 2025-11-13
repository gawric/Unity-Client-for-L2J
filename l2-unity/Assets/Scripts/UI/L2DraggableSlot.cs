using System;
using UnityEngine.UIElements;

public class L2DraggableSlot : L2ClickableSlot
{
    protected SlotDragManipulator _slotDragManipulator;

    public L2DraggableSlot(int position, VisualElement slotElement, SlotType slotType, bool leftMouseUp, bool rightMouseup)
    : base(slotElement, position, slotType, leftMouseUp, rightMouseup)
    {
        if (slotElement == null)
        {
            return;
        }

        CreateDragManipulator(ref _slotDragManipulator, ref slotElement);
    }

    public L2DraggableSlot(int position, VisualElement slotElement, SlotType slotType , bool isDragged)
         : base(slotElement, position, slotType, true, false)
    {
        if (slotElement == null)
        {
            return;
        }

        CreateDragManipulator(ref _slotDragManipulator, ref slotElement , isDragged);
    }


    private void CreateDragManipulator(ref SlotDragManipulator slotDragManipulator , ref VisualElement slotElement)
    {
        if (slotDragManipulator == null)
        {
             slotDragManipulator = new SlotDragManipulator(slotElement, this);
             slotElement.AddManipulator(slotDragManipulator);
        }
    }

    private void CreateDragManipulator(ref SlotDragManipulator slotDragManipulator, ref VisualElement slotElement , bool isDragged)
    {
        if (slotDragManipulator == null)
        {
            slotDragManipulator = new SlotDragManipulator(slotElement, this , isDragged);
            slotElement.AddManipulator(slotDragManipulator);
        }
    }

    public override void ClearManipulators()
    {
        base.ClearManipulators();

        if (_slotDragManipulator != null)
        {
            _slotElement.RemoveManipulator(_slotDragManipulator);
            _slotDragManipulator = null;
        }
    }
}