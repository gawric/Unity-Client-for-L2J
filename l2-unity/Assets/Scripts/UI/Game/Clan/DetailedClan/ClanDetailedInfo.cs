using FMOD.Studio;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ClanDetailedInfo
{
    private const string _templateNameClanInfo = "Data/UI/_Elements/Game/Clan/DetailedContent/ClanInfoContent";
    private const string _templateNamePrivilegesInfo = "Data/UI/_Elements/Game/Clan/DetailedContent/PrivilegesInfoContent";
    private const string _templateNameRankingPrivelege = "Data/UI/_Elements/Game/Clan/DetailedContent/RankingPrivilege";
    private const string _templateNameMemberInfo = "Data/UI/_Elements/Game/Clan/DetailedContent/MemberInfoContent";

    private DataProviderClanInfo _dataProvider;
    private MemberInfoContent _memberInfoContent;

    private PrivilegesInfoContent _privilegesInfoContent;
    private RankingPrivelege _rankingPrivilege;
    private ClanInfoContent _clanInfoContent;
    private ICreatorPanelCheckBox _createPanelCheckBox;
    private ICreatorSkillsPanel _creatorSkillsPanel;
    private VisualElement _detailedInfoElement;

    private int _showPanel = -1;

    public void  SetDetailedInfoElement(VisualElement detailedInfoElement)
    {
        _detailedInfoElement = detailedInfoElement;
    }
    public ClanDetailedInfo(DataProviderClanInfo dataProvider)
    {
        _dataProvider = dataProvider;

        _createPanelCheckBox = new CreatePanelCheckBoxWindows();
        _creatorSkillsPanel = new CreatorSkillsPanel();

        _memberInfoContent = new MemberInfoContent(_dataProvider);
        _memberInfoContent.OnClickHide += OnClickHide;
        _memberInfoContent.OnOutsideClickRank += OnClickOkRank;
        _memberInfoContent.OnOutsideClickTitle += OnClickOkTitle;
        _memberInfoContent.OnOutsideClickDeleteTitle += OnDeleteTitle;
        _memberInfoContent.OnOutsideClickDismiss += OnOkDismiss;


        _privilegesInfoContent = new PrivilegesInfoContent(_dataProvider , _createPanelCheckBox);
        _privilegesInfoContent.OnClickHide += OnClickHide;
        _privilegesInfoContent.OnClickApply += OnClickApplyPrivilege;

        _clanInfoContent = new ClanInfoContent(_dataProvider , _creatorSkillsPanel);
        _clanInfoContent.OnClickHide += OnClickHide;

        _rankingPrivilege = new RankingPrivelege(_dataProvider);
        _rankingPrivilege.OnClickHide += OnClickHide;
        _rankingPrivilege.OnSwitchRank += OnSwitchSubWindow;
    }

   public void LoadAssets(Func<string, VisualTreeAsset> loaderFunc)
   {
        _memberInfoContent.template = loaderFunc(_templateNameMemberInfo);
        _privilegesInfoContent.template = loaderFunc(_templateNamePrivilegesInfo);
        _clanInfoContent.template = loaderFunc(_templateNameClanInfo);
        _rankingPrivilege.template = loaderFunc(_templateNameRankingPrivelege);

        _memberInfoContent.LoadAsset(loaderFunc);
        _createPanelCheckBox.LoadAsset(loaderFunc);
        _creatorSkillsPanel.LoadAsset(loaderFunc);
        _rankingPrivilege.LoadAsset(loaderFunc);

    }

    public void UpdateDetailedInfo(ServerPacket packet, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        switch (packet)
        {
            case PledgeReceiveMemberInfo memberInfo:
                _memberInfoContent.PreShow(memberInfo, detailedInfoElement, packetAll);
                _showPanel = 0;
                break;

            case PledgeReceivePowerInfo powerInfo:
                //if leader max access
                if (powerInfo.Name == packetAll.SubPledgeLeaderName) powerInfo.PowerGradeByRank = ClanPrivileges.CP_ALL;

                _privilegesInfoContent.PreShow(powerInfo, detailedInfoElement);
                _showPanel = 1;
                break;
            case PledgeClanInfo clanInfo:
                _clanInfoContent.PreShow(clanInfo, detailedInfoElement, packetAll);
                _showPanel = 2;
                break;
            case PledgePowerGradeList authInfo:
                _rankingPrivilege.PreShow(authInfo, detailedInfoElement, packetAll);
                _showPanel = 3;
                break;
            case ManagePledgePower pladgePower:
                _privilegesInfoContent.PreShow(pladgePower, detailedInfoElement);
                _showPanel = 1;
                break;
            default:
                // Handle unknown packet types if necessary
                break;
        }
    }
    public int GetShowPanel()
    {
        return _showPanel;
    }

    private void OnClickOkRank(string memberName, int rank)
    {
        //SendGameDataQueue.Instance().AddItem(
        //    CreatorPacketsUser.CreateRequestPledgeSetMemberPowerGrade(memberName , rank),
        //    GameClient.Instance.IsCryptEnabled(),
        //    GameClient.Instance.IsCryptEnabled());

        ////Debug.Log("Click OnClickOkRank memberName " + memberName + " Rank " + rank);
    }

    private void OnClickOkTitle(string memberName, string title)
    {
        SendGameDataQueue.Instance().AddItem(
            CreatorPacketsUser.CreateRequestGiveNickName(memberName, title),
            GameClient.Instance.IsCryptEnabled(),
            GameClient.Instance.IsCryptEnabled());

        //Debug.Log("Click Ok OnClickOkTitle memberName " + memberName + " Rank " + title);
    }

    private void OnDeleteTitle(string memberName)
    {
        SendGameDataQueue.Instance().AddItem(
            CreatorPacketsUser.CreateRequestGiveNickName(memberName, ""),
            GameClient.Instance.IsCryptEnabled(),
            GameClient.Instance.IsCryptEnabled());
    }
    private string _selectDismiss = "";
    private void OnOkDismiss(string memberName)
    {
        _selectDismiss = memberName;
        SystemMessageWindow.Instance.OnButtonOk += SystemMessageClickOkDismiss;
        SystemMessageWindow.Instance.OnButtonClosed += OnРЎancel;
        SystemMessageWindow.Instance.ShowWindowDialogYesOrNot("Do you want to be kicked from the clan?");
    }

    private void SystemMessageClickOkDismiss()
    {
        SendGameDataQueue.Instance().AddItem(
            CreatorPacketsUser.CreateRequestOustPledgeMember(_selectDismiss),
            GameClient.Instance.IsCryptEnabled(),
            GameClient.Instance.IsCryptEnabled());
        CancelEvent();
    }

    private void OnРЎancel()
    {
        CancelEvent();
    }

    private void CancelEvent()
    {
        SystemMessageWindow.Instance.OnButtonOk -= SystemMessageClickOkDismiss;
        SystemMessageWindow.Instance.OnButtonClosed -= OnРЎancel;
    }

    private void OnClickHide(int id)
    {
        _showPanel = -1;
    }

    private void OnClickApplyPrivilege(int useRank)
    {
        List<SettingCheckBox> listSelect = _createPanelCheckBox.GetSelectAllCheckBoxs();
        int newPrivileges = GetCalcNewRank(listSelect);

        if(newPrivileges != -1)
        {
            SendGameDataQueue.Instance().AddItem(
                CreatorPacketsUser.CreateRequestPledgePower(useRank, 2, newPrivileges),
                GameClient.Instance.IsCryptEnabled(),
                GameClient.Instance.IsCryptEnabled());

        }


    }

    private void OnSwitchSubWindow(int id)
    {
        PledgeReceivePowerInfo powerInfo = new PledgeReceivePowerInfo(new byte[1]);
        powerInfo.PowerGrade = id;
        _privilegesInfoContent.PreShow(powerInfo, _detailedInfoElement);
    }

    private int GetCalcNewRank(List<SettingCheckBox> list)
    {
        if(list.Count == 1)
        {
            return list[0].GetData();
        }

        int[] dataArray = list.Where(cb => cb.GetData() != 0)
                               .Select(cb => cb.GetData())
                               .ToArray();

        return ByteUtils.CombineFlags(dataArray);
    }
}
