
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

using UnityEngine.UIElements;



public class ActiveSkillsHide : AbstractSkills
{
    private string[] fillBackgroundDf = { "Data/UI/Window/Skills/QuestWndPlusBtn_v2", "Data/UI/Window/Skills/Button_DF_Skills_Down_v3" };
    private VisualElement _activeSkillPanel;
    private VisualTreeAsset _templateBoxPanel;
    private VisualTreeAsset _templatePanel8x1;
    private List<VisualElement> _boxPanelsPhysical;
    private List<VisualElement> _boxPanelsMagic;

    private const string _rowNamePhysical = "RowPhysical";
    private const string _rowNameMagical = "RowMagical";

    private const string _allContentPhysical = "PhysicalContent";
    private const string _allContentMagical = "MagicContent";
    private List<SkillInstance> _tempList;
    private VisualTreeAsset _templateSlotSkill;
    private int[] _arrDfActiveSelect;

    private VisualElement _rowPhysical;
    private VisualElement _rowMagical;
    public ActiveSkillsHide()
    {

        _arrDfActiveSelect = new int[5] { 0, 0, 0, 0, 0 };
    }

    public void SetActiveSkillTemplate(VisualTreeAsset templateActiveSkill , VisualTreeAsset templateBoxPanel ,  VisualTreeAsset templatePanel8x1 , VisualTreeAsset templateSlotSkill)
    {
        _activeSkillPanel = ToolTipsUtils.CloneOne(templateActiveSkill);
        _templateBoxPanel = templateBoxPanel;
        _templatePanel8x1 = templatePanel8x1;
        _templateSlotSkill = templateSlotSkill;
        _boxPanelsPhysical = new List<VisualElement>();
        _boxPanelsMagic = new List<VisualElement>();
        RegisterClickButtonPhysical(_activeSkillPanel);
        RegisterClickButtonMagic(_activeSkillPanel);
    }


    public VisualElement GetOrCreateTab(VisualElement content)
    {
        if(_activeSkillPanel != null)
        {
            content.Clear();
            content.Add(_activeSkillPanel);
        }

        return _activeSkillPanel;
    }




    public void CreateSlots(List<SkillInstance> list , int sizeCell)
    {
        int panelCount = CalculatePanelCount(list);

        skillsPerPanel = sizeCell;
    
        var activeSkills = list.Where(s => !s.IsMagic()).ToList();
        var magicSkills = list.Where(s => s.IsMagic()).ToList();

        SetAllSlots(new Dictionary<int, SkillSlot>());

        _rowPhysical = _activeSkillPanel.Q(_rowNamePhysical);
        _rowMagical = _activeSkillPanel.Q(_rowNameMagical);

        var allContentPhysical = _activeSkillPanel.Q(_allContentPhysical);
        var allContentMagical = _activeSkillPanel.Q(_allContentMagical);

        ShowPanelIfCount1(activeSkills, _rowPhysical, allContentPhysical);
        ShowPanelIfCount1(magicSkills, _rowMagical, allContentMagical);

         if(_allSlots != null) _allSlots.Clear();
        CreatePhysicalSlots(activeSkills, _rowPhysical);
        CreateMagicalSlots(magicSkills, _rowMagical);

        HidePanelIfCount0(activeSkills, _rowPhysical, allContentPhysical);
        HidePanelIfCount0(magicSkills, _rowMagical, allContentMagical);

        _tempList = list;

    }

    public void UpdateSlots(List<SkillInstance> list)
    {

        var allContentPhysical = _activeSkillPanel.Q(_allContentPhysical);
        var allContentMagical = _activeSkillPanel.Q(_allContentMagical);

        if (list == null | list.Count == 0)
        {
            //var rowPhysical_hide = _activeSkillPanel.Q(_rowNamePhysical);
            //var rowMagical_hide = _activeSkillPanel.Q(_rowNameMagical);

            HidePanels(list, allContentPhysical, allContentMagical);
            return;
        }


     
        List<SkillInstance> added;
        List<SkillInstance> removed;

        CompareSkillLists(list, _tempList, out added, out removed);

        var rowPhysical = _activeSkillPanel.Q(_rowNamePhysical);
        var rowMagical = _activeSkillPanel.Q(_rowNameMagical);



        var addActiveSkills = added.Where(s => !s.IsMagic()).ToList();
        var addMagicSkills = added.Where(s => s.IsMagic()).ToList();


        var rowsVirtualPhysical = rowPhysical.Query<VisualElement>(name: "RowsVirtual").ToList();
        var rowsVirtualMagic = rowMagical.Query<VisualElement>(name: "RowsVirtual").ToList();


        ShowActiveSkillsPanels(list, allContentPhysical);
        ShowMagicSkillsPanels(list, allContentMagical);


        UpdateSkills(addActiveSkills, removed, list, rowsVirtualPhysical, _templateSlotSkill , _templatePanel8x1 , _boxPanelsPhysical[0]);
        UpdateSkills(addMagicSkills, removed, list, rowsVirtualMagic, _templateSlotSkill, _templatePanel8x1, _boxPanelsMagic[0]);
        _tempList = list;

        HidePanelActiveSkillsPanel(list, allContentPhysical);
        HidePanelMagicSkillsPanel(list, allContentMagical);
    }

    

    private void ShowActiveSkillsPanels(List<SkillInstance> list, VisualElement allContentMagical)
    {
        var activeSkills = list.Where(s => !s.IsMagic()).ToList();


        if (activeSkills.Count > 0)
        {
            allContentMagical.style.display = DisplayStyle.Flex;
        }
    }

    private void ShowMagicSkillsPanels(List<SkillInstance> list, VisualElement allContentMagical)
    {

        var magicSkills = list.Where(s => s.IsMagic()).ToList();

        if (magicSkills.Count > 0)
        {
            allContentMagical.style.display = DisplayStyle.Flex;
        }
    }

    private void HidePanels(List<SkillInstance> list, VisualElement allContentMagical, VisualElement allContentPhysical)
    {

        if (list == null | list.Count == 0)
        {
            allContentPhysical.style.display = DisplayStyle.None;
            allContentMagical.style.display = DisplayStyle.None;
        }
    }

    private void HidePanelActiveSkillsPanel(List<SkillInstance> list,  VisualElement allContent)
    {
        var activeSkills = list.Where(s => !s.IsMagic()).ToList();

        if (activeSkills.Count == 0)
        {
            allContent.style.display = DisplayStyle.None;
        }
    }

    private void HidePanelMagicSkillsPanel(List<SkillInstance> list, VisualElement allContent)
    {
        var magicSkills = list.Where(s => s.IsMagic()).ToList();

        if (magicSkills.Count == 0)
        {
            allContent.style.display = DisplayStyle.None;
        }
    }

    public void CreatePhysicalSlots(List<SkillInstance> list , VisualElement rowPhysical)
    {
        if (rowPhysical != null)
        {
            var boxPanel = ToolTipsUtils.CloneOne(_templateBoxPanel);
            SetWidthBoxPanel(boxPanel, skillsPerPanel);
            var panels = base.CreateSlots(list, _templatePanel8x1, _templateSlotSkill, boxPanel);

            _boxPanelsPhysical.Add(panels);
            rowPhysical.Add(panels);
        }
        else
        {
            Debug.LogWarning("ActiveSkillsHide > not found root panels ");
        }
    }

    public void CreateMagicalSlots(List<SkillInstance> list, VisualElement rowMagical)
    {
        if (rowMagical != null)
        {
            var boxPanel = ToolTipsUtils.CloneOne(_templateBoxPanel);
            SetWidthBoxPanel(boxPanel, skillsPerPanel);
            var panels = base.CreateSlots(list, _templatePanel8x1, _templateSlotSkill, boxPanel);
            _boxPanelsMagic.Add(panels);
            rowMagical.Add(panels);
        }
        else
        {
            Debug.LogWarning("ActiveSkillsHide > not found root panels ");
        }
    }




    private void RegisterClickButtonPhysical(VisualElement rootElement)
    {
        rootElement.Q<Button>("DF_Button")?.RegisterCallback<ClickEvent>(evt => ClickDfPhysical((Button)evt.target, _arrDfActiveSelect));
    }

    private void RegisterClickButtonMagic(VisualElement rootElement)
    {
        rootElement.Q<Button>("DF_Button_Magic")?.RegisterCallback<ClickEvent>(evt => ClickDfMagic((Button)evt.target, _arrDfActiveSelect));
    }

    public void ClickDfPhysical(Button btn, int[] arrDfSelect)
    {
        bool show = arrDfSelect[0] == 0;
        ChangeDfBox(btn, fillBackgroundDf[show ? 0 : 1]);
        arrDfSelect[0] = show ? 1 : 0;
        _rowPhysical ??= _activeSkillPanel.Q(_rowNamePhysical);
        ToogleHideElement(_rowPhysical, show);
    }


    public void ClickDfMagic(Button btn, int[] arrDfSelect)
    {
        bool show = arrDfSelect[1] == 0;
        ChangeDfBox(btn, fillBackgroundDf[show ? 0 : 1]);
        arrDfSelect[1] = show ? 1 : 0;
        _rowMagical ??= _activeSkillPanel.Q(_rowNameMagical);
        ToogleHideElement(_rowMagical, show);
    }

}


