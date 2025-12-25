

using UnityEngine;
using UnityEngine.UIElements;

public class TransparentTooltipManipulator : PointerManipulator
{

    private string _text;
    private bool _pointerOver;

    public TransparentTooltipManipulator(VisualElement target, string text)
    {
        this.target = target;
        _text = text;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerEnterEvent>(PointerInHandler);
        target.RegisterCallback<MouseOverEvent>(PointerOverHandler);
        target.RegisterCallback<PointerOutEvent>(PointerOutHandler);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerEnterEvent>(PointerInHandler);
        target.UnregisterCallback<MouseOverEvent>(PointerOverHandler);
        target.UnregisterCallback<PointerOutEvent>(PointerOutHandler);
    }

    private void PointerInHandler(PointerEnterEvent evt)
    {
        if (target != null & L2TransparentTooltip.Instance != null) {
            if (_text.Length > 0)
                L2TransparentTooltip.Instance.UpdateTooltip(_text, target);
        }
        else
        {
            Debug.Log("TransparentTooltipManipulator: PointerInHandler �� ����������� ������!");
        }

    }

    private void PointerOverHandler(MouseOverEvent evt)
    {
        _pointerOver = true;
    }

    private void PointerOutHandler(PointerOutEvent evt)
    {
        if(target != null & L2TransparentTooltip.Instance != null)
        {
            _pointerOver = false;
            if (_text.Length > 0)
                L2TransparentTooltip.Instance.HideWindow(target);
        }
        else
        {
            Debug.Log("TransparentTooltipManipulator: PointerOutHandler �� ����������� ������!");
        }
     
    }

    public void SetText(string text)
    {
        _text = text;
    }

    public void Clear()
    {
        if (_pointerOver)
        {
            if(L2TransparentTooltip.Instance != null)
            {
                L2TransparentTooltip.Instance.HideWindow(target);
            }
            else
            {
                Debug.Log("TransparentTooltipManipulator: Clear �� ����������� ������!");
            }
            
        }
    }
}
