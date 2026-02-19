using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class PassiveSkillsHide : AbstractSkills
{
    private SkillListWindow _skillLearn;
    private string[] fillBackgroundDf = { "Data/UI/Window/Skills/QuestWndPlusBtn_v2", "Data/UI/Window/Skills/Button_DF_Skills_Down_v3" };
    private VisualElement _passiveSkillPanel;
    private VisualTreeAsset _templateBoxPanel;
    private VisualTreeAsset _templatePanel8x1;
    private List<VisualElement> _boxPanelsAbility;
    private List<VisualElement> _boxPanelsSubject;

    private const string _rowNameAbility = "RowAbility";
    private const string _rowNameSubject = "RowSubject";

    private const string _allContentPhysical = "AbilityContent";
    private const string _allContentMagical = "SubjectContent";
    private List<SkillInstance> _tempList;
    private VisualTreeAsset _templateSlotSkill;
    private VisualElement _rowAbility;
    private VisualElement _rowSubject;
    private int[] _arrDfPassiveSelect;


    private const string _abilityButtonName = "DF_Button_Ability";
    private const string _subjectButtonName = "DF_Button_Subject";
    public PassiveSkillsHide()
    {

        _arrDfPassiveSelect = new int[2] { 0, 0 };
    }

    public void SetPassiveSkillTemplate(VisualTreeAsset templatePassiveSkill, VisualTreeAsset templateBoxPanel, VisualTreeAsset templatePanel8x1, VisualTreeAsset templateSlotSkill)
    {
        _passiveSkillPanel = ToolTipsUtils.CloneOne(templatePassiveSkill);
        _templateBoxPanel = templateBoxPanel;
        _templatePanel8x1 = templatePanel8x1;
        _templateSlotSkill = templateSlotSkill;
        _boxPanelsAbility = new List<VisualElement>();
        _boxPanelsSubject = new List<VisualElement>();
        RegisterClickButtonAbility(_passiveSkillPanel);
        RegisterClickButtonSubject(_passiveSkillPanel);

    }


    public VisualElement GetOrCreateTab(VisualElement content)
    {
        if (_passiveSkillPanel != null)
        {
            content.Clear();
            content.Add(_passiveSkillPanel);
        }

        return _passiveSkillPanel;
    }

    public void CreateSlots(List<SkillInstance> list , int sizeCell)
    {
        skillsPerPanel = sizeCell;

        int panelCount = CalculatePanelCount(list);

        var abilitySkills = list.Where(s => s.IsPassive).ToList();
        var subjectSkills = new List<SkillInstance>();

        SetAllSlots(new Dictionary<int, SkillSlot>());

        _rowAbility = _passiveSkillPanel.Q(_rowNameAbility);
        _rowSubject = _passiveSkillPanel.Q(_rowNameSubject);

        var allContentPhysical = _passiveSkillPanel.Q(_allContentPhysical);
        var allContentMagical = _passiveSkillPanel.Q(_allContentMagical);

        ShowPanelIfCount1(abilitySkills, _rowAbility, allContentPhysical);
        ShowPanelIfCount1(subjectSkills, _rowSubject, allContentMagical);

        CreateAbilitySlots(abilitySkills, _rowAbility);
        CreateSubjectSlots(subjectSkills, _rowSubject);

        HidePanelIfCount0(abilitySkills, _rowAbility, allContentPhysical);
        HidePanelIfCount0(subjectSkills, _rowSubject, allContentMagical);
        _tempList = list;
    }





    public void CreateAbilitySlots(List<SkillInstance> list, VisualElement rowPhysical)
    {
        if (rowPhysical != null)
        {
            var boxPanel = ToolTipsUtils.CloneOne(_templateBoxPanel);
            var panels = base.CreateSlots(list, _templatePanel8x1, _templateSlotSkill, boxPanel);
            _boxPanelsAbility.Add(panels);
            rowPhysical.Add(panels);
        }
        else
        {
            Debug.LogWarning("ActiveSkillsHide > not found root panels ");
        }
    }

    public void CreateSubjectSlots(List<SkillInstance> list, VisualElement rowMagical)
    {
        if (rowMagical != null)
        {
            var _boxPanel = ToolTipsUtils.CloneOne(_templateBoxPanel);
            var panels = base.CreateSlots(list, _templatePanel8x1, _templateSlotSkill, _boxPanel);
            _boxPanelsSubject.Add(panels);
            rowMagical.Add(panels);
        }
        else
        {
            Debug.LogWarning("ActiveSkillsHide > not found root panels ");
        }
    }




    private void RegisterClickButtonAbility(VisualElement rootElement)
    {
        rootElement.Q<Button>(_abilityButtonName)?.RegisterCallback<ClickEvent>(evt => ClickDfAbility((Button)evt.target, _arrDfPassiveSelect));
    }

    private void RegisterClickButtonSubject(VisualElement rootElement)
    {
        rootElement.Q<Button>(_subjectButtonName)?.RegisterCallback<ClickEvent>(evt => ClickDfSubject((Button)evt.target, _arrDfPassiveSelect));
    }


    public void ClickDfAbility(Button btn,  int[] arrDfPassiveSelect)
    {
        bool show = arrDfPassiveSelect[0] == 0;
        ChangeDfBox(btn, fillBackgroundDf[show ? 0 : 1]);
        arrDfPassiveSelect[0] = show ? 1 : 0;
        ToogleHideElement(_rowAbility, show);
    }

    public void ClickDfSubject(Button btn,  int[] arrDfPassiveSelect)
    {
        bool show = arrDfPassiveSelect[1] == 0;
        ChangeDfBox(btn, fillBackgroundDf[show ? 0 : 1]);
        arrDfPassiveSelect[1] = show ? 1 : 0;
        ToogleHideElement(_rowSubject, show);
    }


}
