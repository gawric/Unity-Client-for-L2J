using System.Collections.Generic;
using UnityEngine.UIElements;


public class PrivilegesInfoContent : AbstractClanContent
{
    private ICreatorPanelCheckBox _createPanelCheckBox;
    private const string _panelSystemPrivilegesName = "PanelCheckBoxSystemPrivileges";
    private const string _panelSystemClanHallName = "PanelCheckBoxClanHall";
    private const string _panelSystemCastleName = "PanelCheckBoxCastle";


    // Reusable lists
    private List<List<SettingCheckBox>> _leftCheckBoxes;
    private List<List<SettingCheckBox>> _rightCheckBoxes;

    public PrivilegesInfoContent(DataProviderClanInfo dataProvider , ICreatorPanelCheckBox createPanelCheckBox)
    {
        _dataProvider = dataProvider;
        _createPanelCheckBox = createPanelCheckBox;

        // Initialize lists once
        _leftCheckBoxes = new List<List<SettingCheckBox>>();
        _rightCheckBoxes = new List<List<SettingCheckBox>>();

    }

    public void PreShow(ServerPacket serverPacket, VisualElement detailedInfoElement)
    {
        content = LoadContent(content, detailedInfoElement);
        ClearContent(content);
        _createPanelCheckBox.DestroyTempElements();

        Show(serverPacket, detailedInfoElement);
    }

    private void Show(ServerPacket serverPacket, VisualElement detailedInfoElement)
    {


        VisualElement page = ToolTipsUtils.CloneOne(template);


        Button cancelButton = page.Q<Button>("CancelButtonBox2");
        Button applyButton = page.Q<Button>("ApplyButtonBox2"); 

        SubscribeCloseButton(cancelButton, detailedInfoElement);


        var panelPrivilages = page.Q<VisualElement>(_panelSystemPrivilegesName);
        var panelClanHall = page.Q<VisualElement>(_panelSystemClanHallName);
        var panelCastle = page.Q<VisualElement>(_panelSystemCastleName);

        if(serverPacket.GetType() == typeof(PledgeReceivePowerInfo))
        {
            HideElement(applyButton);
            PledgeReceivePowerInfo privilegesInfo = serverPacket as PledgeReceivePowerInfo;
            PrepareUsePowerGrade(privilegesInfo.PowerGradeByRank, panelPrivilages, panelClanHall, panelCastle);
        }

        if (serverPacket.GetType() == typeof(ManagePledgePower))
        {
            ManagePledgePower managePledgePower = serverPacket as ManagePledgePower;
            SubscribeApplyButton(applyButton, detailedInfoElement , managePledgePower.Rank);
            ShowElement(applyButton);

            UseRank(managePledgePower.PrivilegesByRank, panelPrivilages, panelClanHall, panelCastle);
        }


        // Clear and reuse lists
        _leftCheckBoxes.Clear();
        _rightCheckBoxes.Clear();

        if (content != null & page != null)
        {
            content.Add(page);
        }
    }





    private void PrepareUsePowerGrade(int powerGrade , VisualElement _panelPrivilages , VisualElement _panelClanHall, VisualElement _panelCastle)
    {
        //var elements = new VisualElement[] { _panelPrivilages, _panelClanHall, _panelCastle };
        UsePowerGrade(powerGrade, _panelPrivilages, _panelClanHall, _panelCastle);
        //switch (powerGrade)
        //{
        //case -1:
        //  CheckBoxInstaller.UsePowerGradeMinus1(_createPanelCheckBox , _leftCheckBoxes , _rightCheckBoxes, elements);
        //  break;
        //case 1:
        // CheckBoxInstaller.UsePowerGrade1(_createPanelCheckBox, _leftCheckBoxes, _rightCheckBoxes, elements);
        /// break;
        // case 6:
        // CheckBoxInstaller.UsePowerGrade6(_createPanelCheckBox, _leftCheckBoxes, _rightCheckBoxes, elements);
        // break;
        //default:
        //   break;
        //}
    }

    private void UsePowerGrade(int powerGrade, VisualElement panelPrivilages, VisualElement panelClanHall, VisualElement panelCastle)
    {
        PledgePowerCheckBoxInstaller.CreateCheckboxUsePowerGrade(powerGrade,
            new ModelPowerCheckBox(_createPanelCheckBox, _leftCheckBoxes, _rightCheckBoxes, new VisualElement[]
        { panelPrivilages, panelClanHall, panelCastle }), true, true);
    }
    private void UseRank(int rank, VisualElement panelPrivilages, VisualElement panelClanHall, VisualElement panelCastle)
    {
        PledgePowerCheckBoxInstaller.CreateCheckboxUsePowerRanked(rank , 
            new ModelPowerCheckBox(_createPanelCheckBox , _leftCheckBoxes , _rightCheckBoxes , new VisualElement[]
            { panelPrivilages, panelClanHall, panelCastle }), true, false);
    }



}
