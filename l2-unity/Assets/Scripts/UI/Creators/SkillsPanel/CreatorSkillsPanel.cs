using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatorSkillsPanel : ICreatorSkillsPanel
{
    private const string _skillRow8x1Name = "Data/UI/_Elements/Template/Skills/SkillPanels/SkillsRow6x1";
    private const string _slotSkillName = "Data/UI/_Elements/Template/Skills/SkillPanels/SlotSkill";
    private const string _activeSkillName = "Data/UI/_Elements/Game/SkillLearn/OnlySkillPanel";
    private const string _boxPanelName = "Data/UI/_Elements/Template/Skills/SkillBoxRow";

    private VisualTreeAsset _templateSkillsRow8x1;
    private VisualTreeAsset _templateSlotSkill;
    private VisualTreeAsset _templateActiveSkill;
    private VisualTreeAsset _templateBoxPanel;

    private ActiveSkillsHide _skillPanel;

    public CreatorSkillsPanel()
    {
        _skillPanel = new ActiveSkillsHide();
    }
    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        _templateSkillsRow8x1 = loaderFunc(_skillRow8x1Name);
        _templateSlotSkill = loaderFunc(_slotSkillName);
        _templateActiveSkill = loaderFunc(_activeSkillName);
        _templateBoxPanel = loaderFunc(_boxPanelName);

        _skillPanel.SetActiveSkillTemplate(_templateActiveSkill, _templateBoxPanel, _templateSkillsRow8x1, _templateSlotSkill);
    }

    public void RefreshData(VisualElement container)
    {
        _skillPanel.GetOrCreateTab(container);
    }

    public void CreateSlots(List<SkillInstance> list , int sizeCell)
    {
        _skillPanel.CreateSlots(list , sizeCell);
    }
}
