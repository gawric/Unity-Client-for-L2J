using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class L2TwoPanels : L2PopupWindow
{
    protected VisualElement _detailedInfoElement;
    protected void HideDetailedInfo()
    {
        _detailedInfoElement.style.display = DisplayStyle.None;
        _detailedInfoElement.pickingMode = PickingMode.Ignore;
    }

    protected void ShowDetailedInfo()
    {
        _detailedInfoElement.style.display = DisplayStyle.Flex;
    }

    public void SetMouseOverDetectionSubElement(VisualElement subElement)
    {
        if (_mouseOverDetection != null && subElement != null)
        {
            _mouseOverDetection.SetSubElement(subElement);
        }
    }

    public void SetMouseOverDetectionRefreshTargetElement(VisualElement root)
    {
        if (_mouseOverDetection != null && root != null)
        {
            _mouseOverDetection.RefreshTargetElement(root);
        }
    }
}
