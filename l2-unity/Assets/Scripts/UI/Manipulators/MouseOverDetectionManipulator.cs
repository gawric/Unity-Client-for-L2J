using UnityEngine;
using UnityEngine.UIElements;

public class MouseOverDetectionManipulator : PointerManipulator
{
    private bool _enabled = true;
    private bool _overThisManipulator = false;
    private L2UI _ui;
    private VisualElement _subElement;
    private bool _isSelectDropfield = false;
    public bool MouseOver => _overThisManipulator;

    public MouseOverDetectionManipulator(VisualElement target)
    {
        this.target = target;
        _ui = L2GameUI.Instance != null ? L2GameUI.Instance : L2LoginUI.Instance;
    }

    public void RefreshTargetElement(VisualElement newTarget)
    {
        if (target != null) UnregisterCallbacksFromTarget();
        target = newTarget;
        if (target != null) RegisterCallbacksOnTarget();
    }

    public void SetSubElement(VisualElement subElement) => _subElement = subElement;
    public void Enable() => _enabled = true;
    public void Disable()
    {
        _enabled = false;
        if (_overThisManipulator)
        {
           // Debug.Log("PointerLeaveHandler> leave1 unblock 2");
            UnBlock();
        }

    }
    public void SetBlock(bool isBlock) { if (isBlock) Block(); else UnBlock(); }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerEnterEvent>(PointerEnterHandler);
        target.RegisterCallback<PointerOverEvent>(PointerOverHandler);
        target.RegisterCallback<PointerLeaveEvent>(PointerLeaveHandler);
        target.RegisterCallback<PointerDownEvent>(PointerDownHandler);

        if (_subElement != null)
        {
            _subElement.RegisterCallback<PointerDownEvent>(PointerDownHandler);
            _subElement.RegisterCallback<PointerLeaveEvent>(PointerLeaveHandler);
            _subElement.RegisterCallback<PointerOverEvent>(PointerOverHandler);
        }
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerEnterEvent>(PointerEnterHandler);
        target.UnregisterCallback<PointerOverEvent>(PointerOverHandler);
        target.UnregisterCallback<PointerLeaveEvent>(PointerLeaveHandler);
        target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
        if (_subElement != null)
        {
            _subElement.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
            _subElement.UnregisterCallback<PointerLeaveEvent>(PointerLeaveHandler);
            _subElement.UnregisterCallback<PointerOverEvent>(PointerOverHandler);
        }
    }
    //Deprecated not working Flexbox VisualElement
    //private bool IsVisibleAndContains(Vector2 pos, VisualElement el) =>
    //    el != null && el.resolvedStyle.display != DisplayStyle.None && el.worldBound.Contains(pos);



    private bool IsVisibleAndContains(Vector2 posScreen, VisualElement el)
    {

        if (el == null) return false;
        var panel = el.panel;
        if (panel == null) return false;

 
        Vector2 panelPos = new Vector2(posScreen.x, Screen.height - posScreen.y);


        if (el.worldBound.Contains(posScreen))
            return true;

        var picked = panel.Pick(panelPos); 
        if (picked == null) return false;

        return picked == el || el.Contains(picked);
    }

    private void PointerEnterHandler(PointerEnterEvent evt)
    {
        //Debug.Log("PointerLeaveHandler> enter enter");
        if (_enabled && (_subElement == null || !IsVisibleAndContains(evt.position, _subElement)))
        {
            //Debug.Log("PointerLeaveHandler> enter enter block");
            Block();
        }

    }

    private void PointerOverHandler(PointerOverEvent evt)
    {
        //Debug.Log("PointerLeaveHandler> over enter");
        if (_enabled && (_subElement == null || !IsVisibleAndContains(evt.position, _subElement)))
        {
            //Debug.Log("PointerLeaveHandler> over enter block");
            Block();
        }

        else if (_enabled && _subElement != null && IsVisibleAndContains(evt.position, _subElement))
        {
           // Debug.Log("PointerLeaveHandler> over enter block");
            Block();
        }

    }

    private void PointerLeaveHandler(PointerLeaveEvent evt)
    {
       
        //Debug.Log("PointerLeaveHandler> leave enter");
        //if(_subElement != null)
        //{
          //  Debug.Log("name level 1 subelement " + _subElement.name + "Reuslt " + IsVisibleAndContains(evt.position, _subElement) + " evt " + evt.target.ToString());
         //   Debug.Log("name level 2 target element " + target.name + " Reuslt " + IsVisibleAndContains(evt.position, target) + " evt " + evt.target.ToString());
        //}

        if (_isSelectDropfield == true) return;
        if (!_enabled) return;

        if (!IsVisibleAndContains(Input.mousePosition, target) && !IsVisibleAndContains(Input.mousePosition, _subElement))
        {
            UnBlock();
            //Debug.Log("PointerLeaveHandler> leave unblock");
        }

    }

    private void PointerDownHandler(PointerDownEvent evt)
    {
        //Debug.Log("name 1 subelement " + _subElement.name + "Reuslt " + IsVisibleAndContains(evt.position, _subElement));
        //Debug.Log("name 2 target element " + target.name + " Reuslt " + IsVisibleAndContains(evt.position, target)); 

        //Debug.Log("name 1 subelement new " + _subElement.name + "Reuslt " + IsVisibleAndContains(Input.mousePosition, _subElement));
        //Debug.Log("name 1 subelement new " + _subElement.name + "Reuslt " + IsVisibleAndContains(Input.mousePosition, target));

        if (_enabled && (IsVisibleAndContains(Input.mousePosition, _subElement) || IsVisibleAndContains(Input.mousePosition, target)))
            Block();
        else
        {
            //Debug.Log("PointerDownHandler> leave1 unblock");
            UnBlock();
        }

    }

    public void SetBlockDropfieldStatus()
    {
        _isSelectDropfield = true;
        Block();
    }

    public void SetUnBlockDropfieldStatus()
    {
        _isSelectDropfield = false;
        UnBlock();
    }

    private void Block()
    {
        _ui.MouseOverUI = true;
        _overThisManipulator = true;
    }

    private void UnBlock()
    {
        _ui.MouseOverUI = false;
        _overThisManipulator = false;
        //Debug.Log("PointerLeaveHandler> leave1 unblock root");
    }
}