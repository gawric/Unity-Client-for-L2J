
using System.Collections.Generic;
using UnityEngine.UIElements;

public class CheckBoxRootElements
{
    private VisualElement[] _rootPanels;
    private List<List<SettingCheckBox>> _leftAll;
    private List<List<SettingCheckBox>> _rightAll;



    public CheckBoxRootElements(VisualElement[] rootPanels, List<List<SettingCheckBox>> leftAll, List<List<SettingCheckBox>> rightAll)
    {
        _rootPanels = rootPanels;
        _leftAll = leftAll;
        _rightAll = rightAll;
    }

    public void RefreshLeftAndRight(List<List<SettingCheckBox>> leftAll, List<List<SettingCheckBox>> rightAll)
    {
        _leftAll = leftAll;
        _rightAll = rightAll;
    }

    public List<List<SettingCheckBox>> GetLeftAll()
    {
        return _leftAll;
    }

    public List<List<SettingCheckBox>> GetRightAll()
    {
        return _rightAll;
    }

    public void RefreshElements(VisualElement[] rootPanels)
    {
        _rootPanels = rootPanels;
    }
    public VisualElement[] GetRootPanels()
    {
        return _rootPanels;
    }

    public List<SettingCheckBox> GetLeft(int index)
    {
        return _leftAll[index];
    }

    public List<SettingCheckBox> GetRight(int index)
    {
        return _rightAll[index];
    }

    public void Clear()
    {
        _rootPanels = null;
        _leftAll = null;
        _rightAll = null;
    }
}