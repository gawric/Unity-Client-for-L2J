using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


public class CreatePanelCheckBoxWindows : ICreatorPanelCheckBox
{

    private const string _checkBoxTemplateName = "Data/UI/_Elements/Template/Elements/Checkbox/DefaultCheckBox";
    private const string _twoPanelsTemplateName = "Data/UI/_Elements/Template/Clan/TwoPanelsCheckBox";
    private List<VisualTreeAsset> _listTemplate  = new List<VisualTreeAsset>();
    private  string DEFAULT_DISABLED_COLOR = "#7f7f7f";
    private Color _colorTextDisabled;
    private CheckBoxRootElements _tempElements;
    private List<SettingCheckBox> _registeredCallbacks = new List<SettingCheckBox>();
    public CreatePanelCheckBoxWindows()
    {
        ColorUtility.TryParseHtmlString(DEFAULT_DISABLED_COLOR, out _colorTextDisabled);
    }
    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        foreach (string templateName in GetTemplateElementName())
        {
            _listTemplate.Add(loaderFunc(templateName));
        }
    }

    private List<string> GetTemplateElementName()
    {
        return new List<string> { _twoPanelsTemplateName, _checkBoxTemplateName };
    }

    public void CreateTwoPanels(CheckBoxRootElements elements)
    {
        for (int i =0; i <  elements.GetRootPanels().Length; i++)
        {
            VisualElement rootPanel = elements.GetRootPanels()[i];
            VisualElement twoPanel = AddTwoPanels(rootPanel);
            VisualTreeAsset checkBoxTemplate = _listTemplate[1];

            if (twoPanel == null)
            {
                Debug.LogError("CreatePanelCheckBoxWindows-> Not Found Template");
                return;
            }

            _tempElements = elements;

            List<SettingCheckBox> left = elements.GetLeft(i);
            List<SettingCheckBox> right = elements.GetRight(i);

            SetCheckBoxHeaderPanel(twoPanel, _listTemplate[1], left , right);
            //left.RemoveAt(0);
            SetCheckBoxLeftPanel(twoPanel, checkBoxTemplate, left);
            SetCheckBoxRightPanel(twoPanel, checkBoxTemplate, right);
        }
    
    }

    public void DestroyTempElements()
    {
        if(_tempElements != null)
        {
            UnRegisterAllClick();
            _tempElements = null;
            _registeredCallbacks.Clear();
        }
    }

    private void UnRegisterAllClick()
    {
        if (_registeredCallbacks != null)
        {
            foreach (var callbackInfo in _registeredCallbacks)
            {
                callbackInfo.GetElement().UnregisterCallback<ClickEvent>(callbackInfo.GetCallBack());
            }
            _registeredCallbacks.Clear();
        }
    }


    private VisualElement AddTwoPanels(VisualElement panel)
    {


        if(panel != null)
        {
            VisualElement twoPanels = CreateTwoPanels(_listTemplate[0]);
            panel.Add(twoPanels);
            return twoPanels;
        }
        else
        {
            Debug.LogError("CreatePanelCheckBoxWindows-> Not Found Panel");
            return null;
        }

    }

    private void SetCheckBoxHeaderPanel(VisualElement panel, VisualTreeAsset checkboxTemplate , List<SettingCheckBox> left , List<SettingCheckBox> right)
    {
        SettingCheckBox headerSetting = left[0];
        VisualElement checkbox = CreateCheckBox(checkboxTemplate);
        headerSetting.SetElement(checkbox);
        VisualElement header = panel.Q<VisualElement>("GroupHeaderTwoPanels");
        SetSetting(checkbox, headerSetting);
        AddClickHeaderEvent(checkbox, headerSetting ,  left,  right);
        header.Add(checkbox);
    }

    private void SetCheckBoxLeftPanel(VisualElement panel, VisualTreeAsset checkboxTemplate, List<SettingCheckBox> listSetting)
    {
        VisualElement subLeftPanel = panel.Q<VisualElement>("GroupLeftListCheckBox");
        AutoCreateCheckBox(subLeftPanel, listSetting, checkboxTemplate);
    }

    public void SetCheckBoxRightPanel(VisualElement panel, VisualTreeAsset checkboxTemplate, List<SettingCheckBox> listSetting)
    {
        VisualElement subRightPanel = panel.Q<VisualElement>("GroupRightListCheckBox");
        AutoCreateCheckBox(subRightPanel, listSetting, checkboxTemplate);
    }


    public VisualElement CreateTwoPanels(VisualTreeAsset template)
    {
        return ToolTipsUtils.CloneOne(template);
    }

    public VisualElement CreateCheckBox(VisualTreeAsset template)
    {
        return ToolTipsUtils.CloneOne(template);
    }

    private void AutoCreateCheckBox(VisualElement panel, List<SettingCheckBox> listSettings, VisualTreeAsset checkboxTemplate)
    {
        for (int i = 0; i < listSettings.Count; i++)
        {

            SettingCheckBox setting = listSettings[i];

            if (setting.IsHeader())
            {
                Debug.Log("Detected Header " + setting.GetName());
                continue;
            }

            VisualElement checkbox = CreateCheckBox(checkboxTemplate);
            setting.SetElement(checkbox);
            SetSetting(checkbox , setting);
            AddClickEvent(checkbox, setting);
            panel.Add(checkbox);
        }
    }



    private VisualElement SetSetting(VisualElement checkbox , SettingCheckBox setting)
    {
        if (checkbox == null) return null;

        VisualElement uncheked = checkbox.Q<VisualElement>("imageUnchecked");
        VisualElement cheked = checkbox.Q<VisualElement>("imageChecked");
        VisualElement checkedDisabled = checkbox.Q<VisualElement>("imageCheckedDisabled");
        VisualElement uncheckdDisabled = checkbox.Q<VisualElement>("imageUnCheckedDisabled");
        Label label = checkbox.Q<Label>("labelCheckBox");

        setting.SetElementChecked(cheked);
        setting.SetElementUnChecked(uncheked);

        SetChecked(setting.IsChecked() , uncheked, cheked);
        SetDisabled(checkbox, setting.IsDisabled(), setting.IsChecked(), uncheked, cheked, checkedDisabled , uncheckdDisabled);
        SetText(label, setting.GetName());
        SetColorText(label, setting.IsDisabled());

        return checkbox;

    }

    private void AddClickEvent(VisualElement checkbox , SettingCheckBox setting)
    {
        if (!setting.IsDisabled())
        {
            VisualElement uncheked = checkbox.Q<VisualElement>("imageUnchecked");
            VisualElement cheked = checkbox.Q<VisualElement>("imageChecked");

            EventCallback<ClickEvent> callback = evt => OnClick(evt, uncheked, cheked, setting);
            checkbox.RegisterCallback(callback);
            setting.SetCallback(callback);
            _registeredCallbacks.Add(setting);
        }
    }

    private void AddClickHeaderEvent(VisualElement headerCheckBox, SettingCheckBox setting , List<SettingCheckBox> left, List<SettingCheckBox> right)
    {
        if (!setting.IsDisabled())
        {
            EventCallback<ClickEvent> callback = evt => OnClickHeader(evt, left, right, setting);
            headerCheckBox.RegisterCallback(callback);
            setting.SetCallback(callback);
            _registeredCallbacks.Add(setting);
        }
    }

    private void OnClick(ClickEvent evt, VisualElement uncheked , VisualElement cheked , SettingCheckBox setting)
    {
        if (setting.IsChecked())
        {
            SetChecked(false, uncheked, cheked);
            setting.SetChecked(false);
        }
        else
        {
            SetChecked(true, uncheked, cheked);
            setting.SetChecked(true);
        }
    }

    private void OnClickHeader(ClickEvent evt, List<SettingCheckBox> left , List<SettingCheckBox> right , SettingCheckBox headerSetting)
    {
        if (headerSetting.IsChecked())
        {
            ForEachList(left, false);
            ForEachList(right, false);
            SetChecked(false, headerSetting.GetElementUnChecked(), headerSetting.GetElementChecked());
            headerSetting.SetChecked(false);
        }
        else
        {
            ForEachList(left, true);
            ForEachList(right, true);
            SetChecked(true, headerSetting.GetElementUnChecked(), headerSetting.GetElementChecked());
            headerSetting.SetChecked(true);
        }

    }

    private void ForEachList(List<SettingCheckBox> list , bool isCheckedHeader)
    {
        foreach (SettingCheckBox setting in list)
        {
            SetChecked(isCheckedHeader, setting.GetElementUnChecked(), setting.GetElementChecked());
            setting.SetChecked(isCheckedHeader);
        }
    }

    private void SetChecked(bool isChecked , VisualElement uncheked , VisualElement cheked)
    {

        if (isChecked)
        {
            uncheked.style.display = DisplayStyle.None;
            cheked.style.display = DisplayStyle.Flex;
        }
        else
        {
            cheked.style.display = DisplayStyle.None;
            uncheked.style.display = DisplayStyle.Flex;
        }
    }

    private void SetText(Label label, string name )
    {
        label.text = name;
    }

    private void SetColorText(Label label , bool isDisabled)
    {
        if (isDisabled)
        {
            label.style.color = _colorTextDisabled;
        }
        else
        {
            label.style.color = Color.white;
        }
    }

    private void SetDisabled(VisualElement checkbox, bool isDisabled, bool isChecked , VisualElement uncheked, VisualElement cheked , VisualElement  checkedDisabled, VisualElement unCheckedDisabled)
    {


        if (isDisabled & isChecked)
        {
            uncheked.style.display = DisplayStyle.None;
            cheked.style.display = DisplayStyle.None;
            checkedDisabled.style.display = DisplayStyle.Flex;
            unCheckedDisabled.style.display = DisplayStyle.None;
        }
        else if(isDisabled & !isChecked)
        {
            uncheked.style.display = DisplayStyle.None;
            cheked.style.display = DisplayStyle.None;
            checkedDisabled.style.display = DisplayStyle.None;
            unCheckedDisabled.style.display = DisplayStyle.Flex;
        }
    }

    public List<SettingCheckBox> GetSelectAllCheckBoxs() =>
      _registeredCallbacks.Any() ? _registeredCallbacks.Where(cb => cb.IsChecked()).ToList() : new List<SettingCheckBox>();
}
