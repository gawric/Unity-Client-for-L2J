using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.UIElements;
using static UnityEngine.Analytics.IAnalytic;


public class SkillListWindow : L2PopupWindow
{


    //new fields
    private BuilderTabs _builderTabs;
    private VisualTreeAsset _tabTemplate;
    private VisualTreeAsset _tabHeaderTemplate;
    private VisualTreeAsset _templateActiveSkill;
    private VisualTreeAsset _templatePassiveSkill;
    private VisualTreeAsset _templateLearnSkill;

    //templte items
    private VisualTreeAsset _templateBoxPanel;
    private VisualTreeAsset _templateSlotSkill;
    private VisualTreeAsset _templateSkillsRow8x1;

    private const string _activeName = "Active";
    private const string _passiveName = "Passive";
    private const string _learnName = "Learn Skill";
    private Dictionary<int, SkillInstance> _allSkills;


    //end fields
    private ActiveSkillsHide _supportActiveSkills;
    private PassiveSkillsHide _supportPassiveSkills;
    private static SkillListWindow _instance;

    public static SkillListWindow Instance
    {
        get { return _instance; }
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _builderTabs = new BuilderTabs();
            _supportActiveSkills = new ActiveSkillsHide();
            _supportPassiveSkills = new PassiveSkillsHide();
            _allSkills = new Dictionary<int, SkillInstance>();

        }
        else
        {
            Destroy(this);
        }
    }

   
    public bool IsWindowContain(Vector2 vector2)
    {
        return  _windowEle.worldBound.Contains(vector2);
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/SkillLearn/SkillListWindow");
        _tabTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTab");
        _tabHeaderTemplate = LoadAsset("Data/UI/_Elements/Game/SkillLearn/SkillTabHeader");

        _templateActiveSkill = LoadAsset("Data/UI/_Elements/Game/SkillLearn/TabActive");
        _templatePassiveSkill = LoadAsset("Data/UI/_Elements/Game/SkillLearn/TabPassive");
        _templateLearnSkill = LoadAsset("Data/UI/_Elements/Game/SkillLearn/TabLearn");

        //template panels 8x1 skills
        _templateBoxPanel = LoadAsset("Data/UI/_Elements/Template/Skills/SkillBoxRow");
        _templateSkillsRow8x1 = LoadAsset("Data/UI/_Elements/Template/Skills/SkillPanels/SkillsRow8x1");
        _templateSlotSkill = LoadAsset("Data/UI/_Elements/Template/Skills/SkillPanels/SlotSkill");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);


        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        var content = (VisualElement)GetElementById("content");

        _builderTabs.InitContentTabs(new string[3] { _activeName, _passiveName , _learnName });
        _builderTabs.CreateTabs(content, _tabTemplate, _tabHeaderTemplate);

        _supportActiveSkills.SetActiveSkillTemplate(_templateActiveSkill , _templateBoxPanel , _templateSkillsRow8x1 , _templateSlotSkill);
        _supportPassiveSkills.SetPassiveSkillTemplate(_templatePassiveSkill, _templateBoxPanel, _templateSkillsRow8x1, _templateSlotSkill);

        _builderTabs.EventSwitchOut += OnSwitchEventOut;

        VisualElement element = _builderTabs.GetActiveContent();
        _supportActiveSkills.GetOrCreateTab(element);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterCloseWindowEventByName("CloseButton");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

    }

    public void SetSkillList(List<SkillInstance> list)
    {
        if (list == null) return;
        CopyInDictionry(list);

        var activeSkills = list.Where(s => !s.IsPassive).ToList();
        var passiveSkills = list.Where(s => s.IsPassive).ToList();
        _supportActiveSkills.CreateSlots(activeSkills , 7);
        _supportPassiveSkills.CreateSlots(passiveSkills, 7);
    }

    public void UpdateSkillList(List<SkillInstance> list)
    {
        if (list == null) return;
        CopyInDictionry(list);

        var activeSkills = list.Where(s => !s.IsPassive).ToList();
        var passiveSkills = list.Where(s => s.IsPassive).ToList();
        _supportActiveSkills.UpdateSlots(activeSkills);
        _supportPassiveSkills.CreateSlots(passiveSkills, 7);
    }

    public SkillInstance GetSkillInstanceBySkillId(int skillId)
    {
        if (_allSkills == null)
            return null;

        _allSkills.TryGetValue(skillId, out var skill);
        return skill; 
    }

    private void CopyInDictionry(List<SkillInstance> list)
    {
        if (_allSkills.Count == 0) _allSkills.Clear();

        _allSkills = list.ToDictionary(si => si.SkillID);
    }


    // }

    // _activeName = "Active";
    //_passiveName = "Passive";
    // _learnName = "Learn Skill";
    private void OnSwitchEventOut(ITab tab, bool isTrade)
    {
        var tabName = tab.GetTabName();
        var element = _builderTabs.GetActiveContent();
        switch (tabName)
        {
            case _activeName:
                _supportActiveSkills.GetOrCreateTab(element);
                break;

            case _passiveName:
                _supportPassiveSkills.GetOrCreateTab(element);
                break;

            case _learnName:

                break;
        }


    }



 


}
