using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class SettingCheckBox 
{
    private bool _checked = false;
    private bool _disabled = false;
    private string _name;
    private VisualElement _element;
    private VisualElement _elementUnchecked;
    private VisualElement _elementChecked;
    private EventCallback<ClickEvent> _callback;
    private int _data;
    private SettingCheckBox _headerSetting;
    public SettingCheckBox(string name, bool isChecked = false, bool isDisabled = false)
    {
        _name = name;
        _checked = isChecked;
        _disabled = isDisabled;
    }

    public SettingCheckBox(string name, bool isChecked = false, bool isDisabled = false , int data  = 0 , SettingCheckBox headerSetting = null)
    {
        _name = name;
        _checked = isChecked;
        _disabled = isDisabled;
        _data = data;
        _headerSetting = headerSetting;
    }

    public bool IsHeader()
    {
        return _headerSetting == null;
    }
    public void SetElement(VisualElement element)
    {
        _element = element;
    }

    public VisualElement GetElement()
    {
        return _element;
    }
    public void SetElementChecked(VisualElement element)
    {
        _elementChecked = element;
    }

    public VisualElement GetElementChecked()
    {
        return _elementChecked;
    }

    public void SetElementUnChecked(VisualElement element)
    {
        _elementUnchecked = element;
    }

    public VisualElement GetElementUnChecked()
    {
        return _elementUnchecked;
    }

    public SettingCheckBox GetMyHeaderSetting()
    {
        return _headerSetting;
    }

    public void SetChecked(bool click_checked)
    {
        _checked = click_checked;
    }
    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
    }
    // Getters
    public bool IsChecked()
    {
        return _checked;
    }

    public bool IsDisabled()
    {
        return _disabled;
    }

    public string GetName()
    {
        return _name;
    }

    public void SetCallback(EventCallback<ClickEvent> callback)
    {
        _callback = callback;
    }

    public EventCallback<ClickEvent> GetCallBack()
    {
        return _callback;
    }

    public void SetData(int data)
    {
        _data = data;
    }

    public int GetData()
    {
        return _data;
    }
}
