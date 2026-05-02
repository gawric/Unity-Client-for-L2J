using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ModelPowerCheckBox
{
    private ICreatorPanelCheckBox _createPanelCheckBox;
    private List<List<SettingCheckBox>> _leftCheckBoxes;
    private List<List<SettingCheckBox>> _rightCheckBoxes;
    private VisualElement[] _element;

    // Constructor
    public ModelPowerCheckBox(ICreatorPanelCheckBox createPanelCheckBox,
                            List<List<SettingCheckBox>> leftCheckBoxes,
                            List<List<SettingCheckBox>> rightCheckBoxes,
                            VisualElement[] element)
    {
        _createPanelCheckBox = createPanelCheckBox;
        _leftCheckBoxes = leftCheckBoxes;
        _rightCheckBoxes = rightCheckBoxes;
        _element = element;
    }

    // Getters and Setters
    public ICreatorPanelCheckBox CreatePanelCheckBox
    {
        get => _createPanelCheckBox;
        set => _createPanelCheckBox = value;
    }

    public List<List<SettingCheckBox>> LeftCheckBoxes
    {
        get => _leftCheckBoxes;
        set => _leftCheckBoxes = value;
    }

    public List<List<SettingCheckBox>> RightCheckBoxes
    {
        get => _rightCheckBoxes;
        set => _rightCheckBoxes = value;
    }

    public VisualElement[] Element
    {
        get => _element;
        set => _element = value;
    }
}
