using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UIElements;

public class ClanInfoContent : AbstractClanContent
{
    private ICreatorSkillsPanel _skillsPanel;
    private List<SkillInstance> _listDemoSkills = new List<SkillInstance>() ;
    private const string _boxGroupSkillsContainer = "GroupClanSkillsPanel";
    public ClanInfoContent(DataProviderClanInfo dataProvider , ICreatorSkillsPanel skillsPanel)
    {
        _dataProvider = dataProvider;
        _skillsPanel = skillsPanel;
        _listDemoSkills = CreateTestDataSkills();
    }

    private List<SkillInstance> CreateTestDataSkills()
    {
        for(int i = 0; i < 36; i++)
        {
            _listDemoSkills.Add(new SkillInstance(-1, 1, false,false));
        }
        return _listDemoSkills;
    }

    public void PreShow(PledgeClanInfo clanInfo, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        content = LoadContent(content, detailedInfoElement);
        ClearContent(content);

        Show(clanInfo, detailedInfoElement, packetAll);
    }
    public void Show(PledgeClanInfo clanInfo, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        VisualElement page = ToolTipsUtils.CloneOne(template);
        //_dataProvider.SetMemberInfo(page, memberInfo, packetAll);

        Button cancelButton = page.Q<Button>("CancelButtonBox2");
        SubscribeCloseButton(cancelButton, detailedInfoElement);

        VisualElement skillsPanel = page.Q<VisualElement>(_boxGroupSkillsContainer);

        if (skillsPanel != null)
        {
            _skillsPanel.RefreshData(skillsPanel);
            _skillsPanel.CreateSlots(_listDemoSkills , 6);
        }

        _dataProvider.SetSubjectClanInfo(page, packetAll);

        if (content != null & page != null)
        {
            content.Add(page);
        }
    }


}
