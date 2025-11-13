using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public abstract class L2Window : MonoBehaviour
{
    protected VisualElement _root;
    protected VisualTreeAsset _windowTemplate;

    protected VisualElement _windowEle;
    protected bool _isWindowHidden = false;
    protected MouseOverDetectionManipulator _mouseOverDetection;
    protected List<string> _listDropDown;

    void Start()
    {
        _isWindowHidden = false;
        LoadAssets();
    }

    protected abstract void LoadAssets();

    protected VisualTreeAsset LoadAsset(string assetPath)
    {
        VisualTreeAsset asset = Resources.Load<VisualTreeAsset>(assetPath);
        if (asset == null)
        {
            Debug.LogError($"Could not load {assetPath} template.");
        }

        return asset;
    }

    protected bool _isCenter = false;
    protected bool _isReg = false;
    public void OnCenterScreen(VisualElement root)
    {
        _isCenter = false;
        UpdateCenter(root);
    }


    protected float defaultWidth = 0;
    protected float defaultHeight = 0;
    private void UpdateCenter(VisualElement root)
    {
        if (_isReg == false)
        {
            _isReg = true;
            _windowEle.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (_isCenter) return;

                _isCenter = true;

                float width = _windowEle.resolvedStyle.width;
                float height = _windowEle.resolvedStyle.height;

                if(width == 0 & height == 0)
                {
                    width = defaultWidth;
                    height = defaultHeight;
                }


                float parentWidth = root.resolvedStyle.width;
                float parentHeight = root.resolvedStyle.height;

                _windowEle.style.left = (parentWidth - width) / 2;
                _windowEle.style.top = (parentHeight - height) / 2;

            });
        }
    
    }




    public void AddWindow(VisualElement root)
    {
        if (_windowTemplate == null)
        {
            return;
        }
        StartCoroutine(BuildWindow(root));
    }

    protected virtual void InitWindow(VisualElement root)
    {
        try
        {
            _root = root;
            _windowEle = _windowTemplate.Instantiate()[0];
            _mouseOverDetection = new MouseOverDetectionManipulator(_windowEle);
            _windowEle.AddManipulator(_mouseOverDetection);
            DisableEventOnOver(_windowEle);
            if (_isWindowHidden)
            {
                _mouseOverDetection.Disable();
            }

            root.Add(_windowEle);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
 
    }



    protected void OnDropdownPointer(PointerDownEvent evt)
    {
        if (_listDropDown == null || _listDropDown.Count == 0)
        {
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }
    }

    private DropdownFieldUtils dropDownUtils = new DropdownFieldUtils();
    private System.Action<bool> onOpen;
    private System.Action<bool> onClose;
    private bool _IsCloseCreate = false;

    //Algorutm Dropdown disabled click MoveTo
    //_IsCloseCreate > The flag that indicates that we are entering the mode of selecting the desired element from the dropdown list.
    //OnClose(bool check) > OnClose is triggered if the list is closed; it will be triggered even if there was no value in the list.
    //Event Running: 1. OnMoveEvent 2.OnLeaveEvent 3.OnMouseOutFocus 4. OnClose 
    protected void DisableEventOnOver(VisualElement _windowEle)
    {
        var dropdowns = _windowEle.Query<DropdownField>().ToList();

        if(dropdowns != null)
        {
            foreach (var dropdown in dropdowns)
            {
                DisablePointerEvents(dropdown);

                dropdown.RegisterCallback<PointerMoveEvent>(OnMoveEvent);
                dropdown.RegisterCallback<FocusOutEvent>(evt => OnMouseOutFocus(evt ,  dropdown));
                dropdown.RegisterCallback<PointerLeaveEvent>(OnLeaveEvent);
                dropdown.RegisterCallback<PointerDownEvent>(evt => ClickDropdownElement(evt, dropdown));

            }
        }
    
    }

    private void ClickDropdownElement(PointerDownEvent evt, DropdownField dropdown)
    {
        _IsCloseCreate = true;
    }


    private void OnClose(bool check)
    {
        onClose -= OnClose;
        SetUnBlockStatusMouseOver();
        _IsCloseCreate = false;
    }



    //special for dropfield
    private void OnMoveEvent(PointerMoveEvent evt)
    {
       SetBlockStatusMouseOver();
    }

    private void OnMouseOutFocus(FocusOutEvent evt, DropdownField dropdown)
    {
        onClose += OnClose;
        _IsCloseCreate = false;
        dropDownUtils.SubscribeToDropdownOpenClose(dropdown.panel.visualTree, onOpen, onClose , ref _IsCloseCreate);
    }

    private void OnLeaveEvent(PointerLeaveEvent evt)
    {

        if (_IsCloseCreate == false)
        {
            SetUnBlockStatusMouseOver();
        }

    }

    private void DisablePointerEvents(VisualElement element)
    {
        element.RegisterCallback<PointerOverEvent>(evt => evt.StopPropagation());
        element.RegisterCallback<PointerOutEvent>(evt => evt.StopPropagation());
    }



    protected abstract IEnumerator BuildWindow(VisualElement root);

    public virtual void HideWindow()
    {
        try
        {
            _isWindowHidden = true;
            _windowEle.style.display = DisplayStyle.None;
            _mouseOverDetection.Disable();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }

    public virtual void OnlyHideWindow()
    {
        _windowEle.style.display = DisplayStyle.None;
    }

    public virtual void ShowWindow()
    {
        try
        {
            _isWindowHidden = false;
            _windowEle.style.display = DisplayStyle.Flex;
            _mouseOverDetection.Enable();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }

    public virtual void ToggleHideWindow()
    {
        if (_isWindowHidden)
        {
            ShowWindow();
        }
        else
        {
            HideWindow();
        }
    }

    protected Label GetLabelById(string id)
    {
        VisualElement e = GetElementById(id);
        if (e == null)
        {
            return null;
        }

        return (Label)e;
    }

    protected VisualElement GetElementById(string id)
    {
        var btn = _windowEle.Q<VisualElement>(id);
        if (btn == null)
        {
            Debug.LogError(id + " can't be found.");
            return null;
        }

        return btn;
    }

    public VisualElement GetElementByClass(string className)
    {
        var btn = _windowEle.Q<VisualElement>(null, className);
        if (btn == null)
        {
            Debug.LogError(className + " can't be found.");
            return null;
        }

        return btn;
    }


    public virtual void BringToFront()
    {
    }

    public virtual void SendToBack()
    {
    }

    public bool MouseOverThisWindow()
    {
        return _mouseOverDetection.MouseOver;
    }

    protected void SetEnableMouseOver(bool isBlock)
    {
        _mouseOverDetection.SetBlock(isBlock);
    }

    protected void SetBlockStatusMouseOver()
    {
        _mouseOverDetection.SetBlockDropfieldStatus();
    }

    protected void SetUnBlockStatusMouseOver()
    {
        _mouseOverDetection.SetUnBlockDropfieldStatus();
    }
}
