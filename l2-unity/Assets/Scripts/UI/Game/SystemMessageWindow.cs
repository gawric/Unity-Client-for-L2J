using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

using UnityEngine.UIElements;


public class SystemMessageWindow : L2PopupWindow
{
    private static SystemMessageWindow _instance;
    protected VisualTreeAsset _windowTemplateDropdown;
    private const string _dropDownTemplateName = "SystemMessageComboBox";
    private const string _defaultNameCancel = "CancelButton";
    private const string _defaultNameOk = "OkButton";
    public static SystemMessageWindow Instance { get { return _instance; } }
    private UnityEngine.UIElements.Label _textLabel;
    private Button _ñancelButton;
    private Button _okButton;

    public event Action OnButtonOk;
    public event Action OnButtonClosed;



    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/SystemMessageWindow");
        _windowTemplateDropdown = LoadAsset("Data/UI/_Elements/Game/SystemMessageDropBoxWidow");
    }


    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        _ñancelButton = _windowEle.Q<Button>(_defaultNameCancel);
        _okButton = _windowEle.Q<Button>(_defaultNameOk);
        _textLabel = _windowEle.Q<UnityEngine.UIElements.Label>("labelText");
        //RegisterCloseWindowEventByName("OkButton");
        _ñancelButton.RegisterCallback<ClickEvent>(ClickEventClosed);
        _okButton.RegisterCallback<ClickEvent>(ClickEventOk);

        RegisterClickWindowEvent(_windowEle, null);
        OnCenterScreen(_root);
    }


    private void ClickEventOk(ClickEvent evt)
    {
        OnButtonOk?.Invoke(); 
        base.HideWindow();
    }

    private void ClickEventClosed(ClickEvent evt)
    {
        OnButtonClosed?.Invoke(); 
        base.HideWindow();
    }


    public void ShowWindow(string messageText)
    {
        if (_windowEle.name == _dropDownTemplateName)
        {
            ReplaceWindow(_windowTemplate);
            ReRegisterCallBack();
        }

        OnCenterScreen(_root);
        _ñancelButton.style.display = DisplayStyle.None;
        _okButton.style.display = DisplayStyle.Flex;
        _textLabel.text = messageText;
        base.ShowWindow();
    }



    public void ShowWindowDialogYesOrNot(string messageText)
    {
        if (_windowEle.name == _dropDownTemplateName)
        {
            ReplaceWindow(_windowTemplate);
            ReRegisterCallBack();
        }

        OnCenterScreen(_root);
        _ñancelButton.style.display = DisplayStyle.Flex;
        _okButton.style.display = DisplayStyle.Flex;
        _textLabel.text = messageText;
        base.ShowWindow();
    }

    public void ShowWindowDialogDropdownYesOrNot(string headerText , List<string> list)
    {
        ReplaceWindow(_windowTemplateDropdown);

        UnityEngine.UIElements.Label headerName = _windowEle.Q<UnityEngine.UIElements.Label>("labelText");
        DropdownField dropdown = _windowEle.Q<DropdownField>("comboBox");
        headerName.text = headerText;

        SetDropdownList(dropdown, list);
        dropdown.RegisterCallback<PointerDownEvent>(OnDropdownPointer, TrickleDown.TrickleDown);

        ReRegisterCallBack();

        OnCenterScreen(_root);
        base.ShowWindow();


    }

    private void ReRegisterCallBack()
    {
        _ñancelButton = _windowEle.Q<Button>(_defaultNameCancel);
        _okButton = _windowEle.Q<Button>(_defaultNameOk);
        _ñancelButton.RegisterCallback<ClickEvent>(ClickEventClosed);
        _okButton.RegisterCallback<ClickEvent>(ClickEventOk);
    }

    public void SetDropdownList(DropdownField dropdown, List<string> list)
    {
        if (list == null && list.Count ==0)
        {
            dropdown.value = null;
            dropdown.choices = null;
            _listDropDown = list;
        }
        else
        {
            dropdown.value = list[0];
            dropdown.choices = list;
            _listDropDown = list;
        }

    }


    
    private void OnDestroy()
    {
        _instance = null;
    }

}
