using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICreatorPanelCheckBox
{
    void LoadAsset(Func<string, VisualTreeAsset> loaderFunc);
    public void CreateTwoPanels(CheckBoxRootElements elements);
    public void DestroyTempElements();

    public List<SettingCheckBox> GetSelectAllCheckBoxs();
}
