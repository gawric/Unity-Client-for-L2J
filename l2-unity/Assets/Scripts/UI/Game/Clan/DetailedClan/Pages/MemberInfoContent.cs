
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

public class MemberInfoContent : AbstractClanContent
{
    private const string _templateNameElementChangeRank = "Data/UI/_Elements/Game/Clan/DetailedContent/MemberInfoElement/ChangeRank";
    private const string _templateNameElementChangeTitle = "Data/UI/_Elements/Game/Clan/DetailedContent/MemberInfoElement/ChangeTitle";
    private VisualTreeAsset _tamplateChangeRank;
    private VisualTreeAsset _tamplateChangeTitle;
    private DropdownField _dropdown;
    private TextField _textfield;
    private string _selectMemeberName = "";
    private VisualElement _centerBox;

    private const string _dropdownName = "comboBox";
    private const string _textboxName = "userInputField";
    private const string _cancelButtonName = "CancelButtonBox2";
    private const string _centerBoxContentName = "Center";
    private const string _buttonRankName = "RankButton";
    private const string _buttonTitleName = "TitleButton";
    private const string _buttonDismissName = "DismissButton";

    private const string _labelRankName = "RankLabel";
    private const string _labelTitleName = "TitleLabel";
    private const string _dismissTitleName = "DismissLabel";
    private readonly List<string> _itemsDropdown = new List<string> { "1st level Privilege", "2st level Privilege", "3st level Privilege", "4st level Privilege", "Joint Rank" };


    private const string _insideTitleTemplateButtonOkName = "OkTitleButton";
    private const string _insideTitleTemplateButtonCancelName = "CancelTitleButton";
    private const string _insideTitleTemplateButtonDeleteName = "DelTitleButton";


    private const string _insideRankTemplateButtonOkName = "OkRankButton";
    private const string _insideRankTemplateButtonCancelName = "CancelRankButton";



    private Button cancelButton;


    public Action<string , int> OnOutsideClickRank;
    public Action<string , string> OnOutsideClickTitle;
    public Action<string> OnOutsideClickDismiss;
    public Action<string> OnOutsideClickDeleteTitle;


    private bool _isLeader = false;
    public MemberInfoContent(DataProviderClanInfo dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        _tamplateChangeRank = loaderFunc(_templateNameElementChangeRank);
        _tamplateChangeTitle = loaderFunc(_templateNameElementChangeTitle);
    }

    public void PreShow(PledgeReceiveMemberInfo memberInfo, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        content = LoadContent(content, detailedInfoElement);
        ClearContent(content);
        SetLeader(memberInfo, packetAll);
        UnregisterCallBackAllButtons();

        Show(memberInfo, detailedInfoElement, packetAll);
    }
    public void Show(PledgeReceiveMemberInfo memberInfo, VisualElement detailedInfoElement, PledgeShowMemberListAll packetAll)
    {
        VisualElement page = ToolTipsUtils.CloneOne(template);

        _dataProvider.SetMemberInfo(page, memberInfo, packetAll);

        cancelButton = page.Q<Button>(_cancelButtonName);
        var rankButton = page.Q<Button>(_buttonRankName);
        var titleButton = page.Q<Button>(_buttonTitleName);
        var dismissButton = page.Q<Button>(_buttonDismissName);
        _centerBox = page.Q<VisualElement>(_centerBoxContentName);

        var rankEvent = GetSubscribeOnRank(memberInfo.Name, rankButton, _centerBox, detailedInfoElement);
        var titleEvent = GetSubscribeOnTitle(titleButton, _centerBox, detailedInfoElement);
        var dismissEvent = GetSubscribeOnDismiss(memberInfo.Name, dismissButton, _centerBox, detailedInfoElement);



        _selectMemeberName = memberInfo.Name;


        Label dismissLabel = page.Q<Label>(_dismissTitleName);
        Label rankLabel = page.Q<Label>(_labelRankName);
        Label titleLabel= page.Q<Label>(_labelTitleName);

        OnEnabledButton(memberInfo.Name , _isLeader, rankLabel, titleLabel , dismissLabel);




        RegisterCallBackAllButtons(new Button[3] { rankButton, titleButton, dismissButton },
            new EventCallback<ClickEvent>[3] { rankEvent, titleEvent, dismissEvent });

        SubscribeCloseButton(cancelButton, detailedInfoElement);

        if (content != null & page != null)
        {
            content.Add(page);
        }
    }


    private void SetLeader(PledgeReceiveMemberInfo memberInfo, PledgeShowMemberListAll packetAll)
    {
        _isLeader = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.Name == packetAll.SubPledgeLeaderName;
    }


    private EventCallback<ClickEvent> GetSubscribeOnRank(string selectName, Button rankButton, VisualElement centerBox, VisualElement detailedInfoElement)
    {
        string selfName = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.Name;

        return _isLeader && selectName != selfName && rankButton != null && !GetStatusCallBack(0)
            ? new EventCallback<ClickEvent>(evt => OnRankButtonClick(centerBox))
            : null;
    }


    private EventCallback<ClickEvent> GetSubscribeOnTitle(Button titleButton, VisualElement centerBox, VisualElement detailedInfoElement)
    {
        return _isLeader && titleButton != null && !GetStatusCallBack(1)
            ? new EventCallback<ClickEvent>(evt => OnTitleButtonClick(centerBox))
            : null;
    }

    private EventCallback<ClickEvent> GetSubscribeOnDismiss(string selectName, Button rankButton, VisualElement centerBox, VisualElement detailedInfoElement)
    {
        string selfName = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.Name;

        return _isLeader && selectName != selfName && rankButton != null && !GetStatusCallBack(2)
            ? new EventCallback<ClickEvent>(evt => OnDismissButtonClick(centerBox))
            : null;
    }

    private void OnRankButtonClick(VisualElement centerBox)
    {
        centerBox.Clear();

        InsideUnregisterAll();

        VisualElement elementChangeRank = ToolTipsUtils.CloneOne(_tamplateChangeRank);
        _dropdown = elementChangeRank.Q<DropdownField>(_dropdownName);
        _dropdown.value = "";

        var _insideOkButton = elementChangeRank.Q<Button>(_insideRankTemplateButtonOkName);
        var _insideCancelButton = elementChangeRank.Q<Button>(_insideRankTemplateButtonCancelName);

        var _isInsideRankOkSubscribed = GetStatusInsideCallBack(0);
        var _isInsideRankCancelSubscribed = GetStatusInsideCallBack(1);

        var _rankOkCallback = new EventCallback<ClickEvent>(evt => OnRankOkClick());
        var _rankCancelCallback = new EventCallback<ClickEvent>(evt => OnTitleCancelClick(centerBox));

        RegisterInsideCallBackAllButtons(new Button[2] { _insideOkButton , _insideCancelButton } , new EventCallback<ClickEvent>[2] { _rankOkCallback  , _rankCancelCallback } );


        SetDropdownList(_dropdown, _itemsDropdown);
        centerBox?.Add(elementChangeRank);
    }




    private void OnTitleButtonClick(VisualElement centerBox)
    {
        centerBox.Clear();

        InsideUnregisterAll();

        VisualElement elementChangeTitle= ToolTipsUtils.CloneOne(_tamplateChangeTitle);
        _textfield = elementChangeTitle.Q<TextField>(_textboxName);


        var _insideOkButton = elementChangeTitle.Q<Button>(_insideTitleTemplateButtonOkName);
        var _insideCancelButton = elementChangeTitle.Q<Button>(_insideTitleTemplateButtonCancelName);
        var _insideDeleteButton = elementChangeTitle.Q<Button>(_insideTitleTemplateButtonDeleteName);


        var _titleOkCallback = new EventCallback<ClickEvent>(evt => OnTitleOkClick());
        var _titleCancelCallback = new EventCallback<ClickEvent>(evt => OnTitleCancelClick(centerBox));
        var _titleDeleteCallback = new EventCallback<ClickEvent>(evt => OnTitleDeleteClick());

        RegisterInsideCallBackAllButtons(new Button[3] { _insideOkButton, _insideCancelButton , _insideDeleteButton }, new EventCallback<ClickEvent>[3] { _titleOkCallback, _titleCancelCallback , _titleDeleteCallback });

        centerBox?.Add(elementChangeTitle);
    }

    private void OnTitleOkClick(){
        _centerBox.Clear();
        OnOutsideClickTitle?.Invoke(_selectMemeberName, _textfield?.value);
    }

    private void OnTitleDeleteClick()
    {
        _centerBox.Clear();
        OnOutsideClickDeleteTitle?.Invoke(_selectMemeberName);
    }
    private void OnRankOkClick() {
        _centerBox.Clear();
        int adjustedIndex = _dropdown != null ? _dropdown.index + 1 : -1;
        OnOutsideClickRank?.Invoke(_selectMemeberName, adjustedIndex);
    } 
    private void OnDismissButtonClick(VisualElement centerBox){ centerBox.Clear(); OnOutsideClickDismiss?.Invoke(_selectMemeberName); }


    private void OnTitleCancelClick(VisualElement centerBox)
    {
        centerBox.Clear();
    }

    public void SetDropdownList(DropdownField dropdown, List<string> items)
    {
        if (items == null)
        {
            dropdown.value = null;
            dropdown.choices = null;
        }
        else
        {
            dropdown.choices = items;
        }

    }

    private void InsideUnregisterAll()
    {
        UnregisterInsideCallBackAllButtons();
    }


    //style code
    private void OnEnabledButton(string selectName, bool isLeader, UnityEngine.UIElements.Label rankLabel,
UnityEngine.UIElements.Label titleLabel, UnityEngine.UIElements.Label dismisslabel)
    {
        ApplyLeaderStyles(isLeader, rankLabel, titleLabel, dismisslabel);
        ApplySelfRestrictions(selectName, rankLabel, dismisslabel);
    }

    private void ApplyLeaderStyles(bool isLeader, UnityEngine.UIElements.Label rankLabel,
    UnityEngine.UIElements.Label titleLabel, UnityEngine.UIElements.Label dismisslabel)
    {
        var labels = new[] { rankLabel, titleLabel, dismisslabel };
        string activeStyle = isLeader ? USS_STYLE_YELLOW : USS_STYLE_DISABLED;
        string inactiveStyle = isLeader ? USS_STYLE_DISABLED : USS_STYLE_YELLOW;

        foreach (var label in labels)
        {
            UpdateLabelStyle(label, activeStyle, inactiveStyle);
        }
    }

    private void ApplySelfRestrictions(string selectName, UnityEngine.UIElements.Label rankLabel,
    UnityEngine.UIElements.Label dismisslabel)
    {
        string selfName = StorageNpc.getInstance().GetFirstUser().PlayerInfoInterlude.Identity.Name;

        if (selectName == selfName)
        {
            UpdateLabelStyle(rankLabel, USS_STYLE_DISABLED, USS_STYLE_YELLOW);
            UpdateLabelStyle(dismisslabel, USS_STYLE_DISABLED, USS_STYLE_YELLOW);
        }
    }

    private void UpdateLabelStyle(UnityEngine.UIElements.Label label, string addStyle, string removeStyle)
    {
        label.RemoveFromClassList(removeStyle);
        label.AddToClassList(addStyle);
    }
}
