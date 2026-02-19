using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.UIElements;

public class ClanWindow : L2TwoPanels
{
   
    private static ClanWindow _instance;
    private MasterClan _masterClan;
    private ClanDetailedInfo _detailedClan;
    private ICreatorTables _creatorTableWindows;

    private PledgeClanInfo _pladgeClanInfo = new PledgeClanInfo(new byte[1]);

    private DropdownField _dropdown;
    private string _selectDropDown = "";

    private DataProviderClanInfo _dataProviderClanInfo;

    private Label _labelEditAuth;
    private Label _labelMemberInfo;
    private Label _labelPrivileges;
    private Label _labelClanInfo;
    private Label _labelPenalty;
    private Label _labelLeave;
    private Label _labelClanEntry;
    private Label _labelInvite;

    //data
    private PledgeShowMemberListAll _packet;

    public static ClanWindow Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _creatorTableWindows = new CreatorTableWindows();
            _masterClan = new MasterClan();

            _dataProviderClanInfo = new DataProviderClanInfo();
            _detailedClan = new ClanDetailedInfo(_dataProviderClanInfo);
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Clan/ClanWindow");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {


        InitWindow(root);


        yield return new WaitForEndOfFrame();

        defaultHeight = 447;
        defaultWidth = 538;

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);


        _dropdown = _windowEle.Q<DropdownField>("clanComboBox");
        _dropdown.pickingMode = PickingMode.Position;
        DisableEventOnOver(_dropdown);
        _dropdown.RegisterCallback<PointerDownEvent>(OnDropdownPointer, TrickleDown.TrickleDown);


        var master_table_content = GetElementByClass("master-table-list");

        _creatorTableWindows.InitTable(master_table_content);
        _creatorTableWindows.LoadAsset(LoadAsset);
        _masterClan.ForEachClan(_creatorTableWindows);
        _creatorTableWindows.OnRowClicked += _masterClan.SelectMember;
        _creatorTableWindows.OnRowClicked += SelectMember;

        _detailedClan.LoadAssets(LoadAsset);



        var memberButton = (UnityEngine.UIElements.Button)GetElementById("MemberButton");
        memberButton?.RegisterCallback<ClickEvent>(evt => OnClickShowMember(evt));

        var privilegesButton = (UnityEngine.UIElements.Button)GetElementById("PrivilegesButton");
        privilegesButton?.RegisterCallback<ClickEvent>(evt => OnClickPrivileges(evt));
   
        var editAuthButton = (UnityEngine.UIElements.Button)GetElementById("EditAuthButton");
        editAuthButton?.RegisterCallback<ClickEvent>(evt => OnClickEditAuthButton(evt));

        var clanInfo = (UnityEngine.UIElements.Button)GetElementById("ClanInoButton");
        clanInfo?.RegisterCallback<ClickEvent>(evt => OnClickClanInfo(evt));



        var penaltyButton = (UnityEngine.UIElements.Button)GetElementById("PenaltyButton");
        penaltyButton?.RegisterCallback<ClickEvent>(evt => OnClickPenalty(evt));

        var leaveButton = (UnityEngine.UIElements.Button)GetElementById("LeaveButton");
        leaveButton?.RegisterCallback<ClickEvent>(evt => OnClickLeave(evt));

        var inviteButton = (UnityEngine.UIElements.Button)GetElementById("InviteButton");
        inviteButton?.RegisterCallback<ClickEvent>(evt => OnClickInvite(evt));

        _detailedInfoElement = (VisualElement)GetElementById("detailedInfo");
        var masterClan = (VisualElement)GetElementById("masterClan");

        _detailedClan.SetDetailedInfoElement(_detailedInfoElement);

        _labelEditAuth = (UnityEngine.UIElements.Label)GetElementById("EditAuthLabel");
        _labelMemberInfo = (UnityEngine.UIElements.Label)GetElementById("MemberInfoLabel");
        _labelPrivileges = (UnityEngine.UIElements.Label)GetElementById("PrivilegesLabel");
        _labelClanInfo = (UnityEngine.UIElements.Label)GetElementById("ClanInfoLabel");
        _labelPenalty = (UnityEngine.UIElements.Label)GetElementById("PenaltyLabel");
        _labelLeave = (UnityEngine.UIElements.Label)GetElementById("LeaveLabel");
        _labelClanEntry = (UnityEngine.UIElements.Label)GetElementById("ClanEntryLabel");
        _labelInvite = (UnityEngine.UIElements.Label)GetElementById("InviteLabel");


        SetMouseOverDetectionSubElement(_detailedInfoElement);
        SetMouseOverDetectionRefreshTargetElement(masterClan);


        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        HideDetailedInfo();

        OnCenterScreen(_root);
        EnabledUnkButtons();
    }

    //Detailed Clan
    public void UpdateDetailedInfo(ServerPacket packet)
    {
        _detailedClan.UpdateDetailedInfo(packet , _detailedInfoElement , _packet);
        ShowDetailedInfo();
    }

    public void ShowGradeInfo(ServerPacket packet)
    {
        _detailedClan.UpdateDetailedInfo(packet, _detailedInfoElement, _packet);
        ShowDetailedInfo();
    }


    //Master Clan 
    public void AddClanData(PledgeShowMemberListAll packet )
    {
        _dataProviderClanInfo.SetMasterClanInfo(_windowEle , packet);
        _listDropDown = _masterClan.SetDropdownList(_dropdown , packet.PledgeName);
        _masterClan.CreateMembersTable(packet.Members, _creatorTableWindows);
        _packet = packet;
        SetDisabledButton(packet.SubPledgeLeaderName , packet.Members);
    }

    //not wornig 
    public void DeleteMemberData()
    {
        //_packet.Members.Clear();
        //_creatorTableWindows.UpdateTableData(_masterClan.GetEmptyData());
        //_masterClan.Up(_creatorTableWindows);
    }

    public void UpdateMemberData(PledgeShowMemberListUpdate packetUpdate)
    {
        ClanMember clanMember = new ClanMember(packetUpdate.MemberName, packetUpdate.Level, packetUpdate.ClassId, packetUpdate.Sex, packetUpdate.Race, packetUpdate.IsOnline, packetUpdate.PledgeType);
        _masterClan.UpdateMemberData(clanMember, _packet, _creatorTableWindows);
    }

    public void DeleteMemberData(PledgeShowMemberListDelete packetDelete)
    {
        _masterClan.DeleteMemeberTable(packetDelete.MemberName, _packet, _creatorTableWindows);
    }

    public void AddMemberData(PledgeShowMemberListAdd packetAdd)
    {
        _masterClan.AddMemberData(packetAdd.ClanMember, _packet, _creatorTableWindows);
    }

    public void UpdatePledge(PledgeInfo pledge)
    {
        _dataProviderClanInfo.UpdateClanInfo(_windowEle, pledge);
    }

    public void UpdateClanIdInfo(PledgeStatusChanged changed)
    {
        if(_packet != null & changed != null)
        {
            _packet.ClanId = changed.ClanId;
            _packet.CrestId = changed.CrestId;
            _packet.LeaderId = changed.LeaderId;
            _packet.AllyId = changed.AllyId;
            _packet.AllyCrestId = changed.AllyCrestId;

        }
    }


    private void OnClickShowMember(ClickEvent evt)
    {
        //if (!string.IsNullOrEmpty(_masterClan.GetSelectMemberName()))
        //{
        //    SendGameDataQueue.Instance().AddItem(
        //        CreatorPacketsUser.CreateRequestPladgeMemberInfo(_masterClan.GetSelectMemberName()), 
        //        GameClient.Instance.IsCryptEnabled(), 
        //        GameClient.Instance.IsCryptEnabled());
        //}

    }

    private void OnClickPrivileges(ClickEvent evt)
    {
        //if (!string.IsNullOrEmpty(_masterClan.GetSelectMemberName()))
        //{
        //    SendGameDataQueue.Instance().AddItem(
        //        CreatorPacketsUser.CreateRequestPledgeMemberPowerInfo(_masterClan.GetSelectMemberName()),
        //        GameClient.Instance.IsCryptEnabled(),
        //        GameClient.Instance.IsCryptEnabled());
        //}

    }




    private void OnClickEditAuthButton(ClickEvent evt)
    {
        //if (!string.IsNullOrEmpty(_masterClan.GetSelectMemberName()))
        //{
        //    SendGameDataQueue.Instance().AddItem(
        //        CreatorPacketsUser.CreateRequestPledgePowerGradeList(),
        //        GameClient.Instance.IsCryptEnabled(),
        //        GameClient.Instance.IsCryptEnabled());
        //}

    }


    private void OnClickLeave(ClickEvent evt)
    {
        if(_packet != null && !IsLeader(_packet.SubPledgeLeaderName))
        {
            SystemMessageWindow.Instance.OnButtonOk += OkLeave;
            SystemMessageWindow.Instance.OnButtonClosed += OnРЎancel;
            SystemMessageWindow.Instance.ShowWindowDialogYesOrNot("Are you sure you want to leave the clan?");
        }

    }

    private void OnClickInvite(ClickEvent evt)
    {
        if (_packet != null && IsLeader(_packet.SubPledgeLeaderName))
        {
            SystemMessageWindow.Instance.ShowWindowDialogDropdownYesOrNot(" Select a Unit " , new List<string> { _packet.PledgeName});
            SystemMessageWindow.Instance.OnButtonOk += OkInvite;
            SystemMessageWindow.Instance.OnButtonClosed += OnРЎancel;
        }

    }

    private bool IsLeader(string leaderName)
    {
        string userName = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.Name;
        return leaderName == userName;
    }

    private void OnClickPenalty(ClickEvent evt)
    {

            SendGameDataQueue.Instance().AddItem(
                CreatorPacketsUser.CreateRequestUserCommand(100),
                GameClient.Instance.IsCryptEnabled(),
                GameClient.Instance.IsCryptEnabled());
        
    }

    private void OnClickClanInfo(ClickEvent evt)
    {
        _detailedClan.UpdateDetailedInfo(_pladgeClanInfo, _detailedInfoElement, _packet);
        ShowDetailedInfo();
    }


    public void SelectMember(int selectIndex, string select_text)
    {
        if (_detailedInfoElement.style.display == DisplayStyle.Flex)
        {


            switch (_detailedClan.GetShowPanel())
            {
                case 0:
                    OnClickShowMember(null);
                    break;

                case 1:
                    OnClickPrivileges(null);
                    break;
                default:
                    break;
            }
        }
    }




    private const string USS_STYLE_YELLOW = "button-label-yellow";
    private const string USS_STYLE_DISABLED = "button-label-disabled";
    private void SetDisabledButton(string leaderName, List<ClanMember> Members)
    {
        string userName = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.Name;
        ResetAllButtons();

        if (IsLeader(leaderName))
            EnabledLeaderButtons();
        else
        {
            if (Members.FirstOrDefault(m => m.MemberName == userName) == null)
                EnabledUnkButtons();
            else
                EnabledUserButtons();
        }
    }


    private void ResetAllButtons()
    {
        _labelEditAuth.AddToClassList(USS_STYLE_DISABLED);
        _labelMemberInfo.AddToClassList(USS_STYLE_DISABLED);
        _labelPrivileges.AddToClassList(USS_STYLE_DISABLED);
        _labelClanInfo.AddToClassList(USS_STYLE_DISABLED);
        _labelPenalty.AddToClassList(USS_STYLE_DISABLED);
        _labelLeave.AddToClassList(USS_STYLE_DISABLED);
        _labelClanEntry.AddToClassList(USS_STYLE_DISABLED);
        _labelInvite.AddToClassList(USS_STYLE_DISABLED);
    }


    private void EnabledLeaderButtons()
    {
        _labelEditAuth.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelMemberInfo.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelPrivileges.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelClanInfo.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelPenalty.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelLeave.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelInvite.RemoveFromClassList(USS_STYLE_DISABLED);


        _labelEditAuth.AddToClassList(USS_STYLE_YELLOW);
        _labelMemberInfo.AddToClassList(USS_STYLE_YELLOW);
        _labelPrivileges.AddToClassList(USS_STYLE_YELLOW);
        _labelClanInfo.AddToClassList(USS_STYLE_YELLOW);
        _labelPenalty.AddToClassList(USS_STYLE_YELLOW);
        _labelLeave.AddToClassList(USS_STYLE_YELLOW);
        _labelInvite.AddToClassList(USS_STYLE_YELLOW);

    }

    private void EnabledUserButtons()
    {
        _labelMemberInfo.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelPrivileges.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelPenalty.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelClanInfo.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelLeave.RemoveFromClassList(USS_STYLE_DISABLED);

        _labelMemberInfo.AddToClassList(USS_STYLE_YELLOW);
        _labelPrivileges.AddToClassList(USS_STYLE_YELLOW);
        _labelPenalty.AddToClassList(USS_STYLE_YELLOW);
        _labelClanInfo.AddToClassList(USS_STYLE_YELLOW);
        _labelLeave.AddToClassList(USS_STYLE_YELLOW);
    }

    private void EnabledUnkButtons()
    {

        _labelPenalty.RemoveFromClassList(USS_STYLE_DISABLED);
        _labelLeave.RemoveFromClassList(USS_STYLE_DISABLED);

        _labelPenalty.AddToClassList(USS_STYLE_YELLOW);
        _labelLeave.AddToClassList(USS_STYLE_YELLOW);

    }

    private void OkLeave()
    {
        SendGameDataQueue.Instance().AddItem(
               CreatorPacketsUser.CreateRequestWithdrawPledge(),
               GameClient.Instance.IsCryptEnabled(),
               GameClient.Instance.IsCryptEnabled());
        CancelEvent();
    }

    private void OkInvite()
    {
        if (TargetManager.Instance.HasTarget() && TargetManager.Instance.Target.GetEntity() != null)
        {
            int id = TargetManager.Instance.Target != null ? TargetManager.Instance.Target.GetEntity().IdentityInterlude.Id : 0;

            SendGameDataQueue.Instance().AddItem(
                   CreatorPacketsUser.CreateRequestJoinPledge(id),
                   GameClient.Instance.IsCryptEnabled(),
                   GameClient.Instance.IsCryptEnabled());
        }

        CancelEvent();
    }

    private void OnРЎancel()
    {
        CancelEvent();
    }
    private void CancelEvent()
    {
        SystemMessageWindow.Instance.OnButtonOk -= OkLeave;
        SystemMessageWindow.Instance.OnButtonClosed -= OnРЎancel;
    }


    private void OnDestroy()
    {
        _instance = null; 
        _creatorTableWindows = null;
        _masterClan = null;
        _detailedClan = null;
        _dataProviderClanInfo = null;
    }
}
