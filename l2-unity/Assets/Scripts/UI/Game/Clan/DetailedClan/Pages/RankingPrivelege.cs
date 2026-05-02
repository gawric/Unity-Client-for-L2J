using FMOD.Studio;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RankingPrivelege : AbstractClanContent
{
    private ICreatorTables _creatorTableWindows;
    private Func<string, VisualTreeAsset> _loaderFunc;
    private const string master_table_content = "Content";
    private const string button_edit_auth = "EditAuthButton";
    private PledgePowerGradeList _rankingList;
    private GradeList _selectRank;
    private ClanDetailedInfo _detailedClan;
    public Action<int> OnSwitchRank;
    private const int EDIT_MODE_ALL_NOT_CHECKED = -1;
    public RankingPrivelege(DataProviderClanInfo dataProvider)
    {
        _dataProvider = dataProvider;
        _creatorTableWindows = new CreatorTableWindows();
        _creatorTableWindows.OnRowClicked += SelectRank;
    }

    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        _loaderFunc = loaderFunc;
    }
    public void PreShow(PledgePowerGradeList rankingList, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        _rankingList = rankingList;
        content = LoadContent(content, detailedInfoElement);
        ClearClick(content);
        ClearContent(content);
        if (_creatorTableWindows != null) _creatorTableWindows.DestroyTable();

        Show(rankingList, detailedInfoElement, packetAll);
    }
    public void Show(PledgePowerGradeList rankingList, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        VisualElement page = ToolTipsUtils.CloneOne(template);
        VisualElement master_table_content_result = page.Q<VisualElement>(master_table_content);
        Button editButton = page.Q<Button>(button_edit_auth);
        editButton?.RegisterCallback<ClickEvent>(evt => OnClickEdit(evt));

        CreateTable(rankingList , master_table_content_result, _loaderFunc);

        if (content != null & page != null)
        {
            content.Add(page);
        }
    }

    private void CreateTable(PledgePowerGradeList rankingList , VisualElement master_table_content , Func<string, VisualTreeAsset> loaderFunc)
    {
        if(master_table_content != null)
        {
            _creatorTableWindows.InitTable(master_table_content);
            _creatorTableWindows.LoadAsset(loaderFunc);
            RankedClanTableColumn.ForEachClan(rankingList.GradeList, _creatorTableWindows);
        }

    }

    public void SelectRank(int select , string name)
    {
        if (ArrayUtils.IsValidIndexList(_rankingList.GradeList, select))
        {
            GradeList gradeItem = _rankingList.GradeList[select];
            _selectRank = gradeItem;
        }
    }

    private void OnClickEdit(ClickEvent evt)
    {
        if (_selectRank != null)
        {

            SendGameDataQueue.Instance().AddItem(
                CreatorPacketsUser.CreateRequestPledgePower(_selectRank.GetRank(), 1, _selectRank.GetPower()),
                GameClient.Instance.IsCryptEnabled(),
                GameClient.Instance.IsCryptEnabled());

            //OnSwitchRank?.Invoke(EDIT_MODE_ALL_NOT_CHECKED);

        }

    }

    private void ClearClick(VisualElement content)
    {
        Button editButton = content.Q<Button>(button_edit_auth);
        editButton?.UnregisterCallback<ClickEvent>(evt => OnClickEdit(evt));
    }
}
