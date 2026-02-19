using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class PledgePowerCheckBoxInstaller
{
    private static CheckBoxRootElements _defaultTrueTrue;
    private static CheckBoxRootElements _defaultFalseTrue;

    private static SettingCheckBox[] CreateHeaders() => new[]
    {
        new SettingCheckBox("System Privileges", false, false),
        new SettingCheckBox("Clan Hall Privileges", false, false),
        new SettingCheckBox("Castle/Fortress", false, false)
    };

    private static void CreateAndDisplayPanels(ModelPowerCheckBox model, List<List<SettingCheckBox>> left, List<List<SettingCheckBox>> right)
    {
        var elements = new CheckBoxRootElements(model.Element, left, right);
        model.CreatePanelCheckBox.CreateTwoPanels(elements);
    }

    private static (List<List<SettingCheckBox>> left, List<List<SettingCheckBox>> right) PrepareCheckBoxLists(ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled)
    {
        var headers = CreateHeaders();
        return (
            CheckBoxInstaller.CreateAllLeft1(model.LeftCheckBoxes, defaultChecked, defaultDisabled, headers),
            CheckBoxInstaller.CreateAllRight1(model.RightCheckBoxes, defaultChecked, defaultDisabled, headers)
        );
    }

    private static bool TrySetPrivilege(List<List<SettingCheckBox>> checkBoxLists, int privilege, bool defaultChecked, bool defaultDisabled)
    {
        foreach (var checkBoxList in checkBoxLists)
        {
            var matchingCheckBox = checkBoxList.FirstOrDefault(cb => cb.GetData() == privilege);
            if (matchingCheckBox != null)
            {
                var header = matchingCheckBox.GetMyHeaderSetting();
                header?.SetChecked(defaultChecked);
                header?.SetDisabled(defaultDisabled);
                //matchingCheckBox.SetChecked(true);
                matchingCheckBox.SetChecked(defaultChecked);
                matchingCheckBox.SetDisabled(defaultDisabled);
                return true;
            }
        }
        return false;
    }

    private static void ApplyPrivileges(int[] splitData, List<List<SettingCheckBox>> left, List<List<SettingCheckBox>> right, bool defaultChecked, bool defaultDisabled)
    {
        foreach (int privilege in splitData)
        {
            if (!TrySetPrivilege(left, privilege, defaultChecked, defaultDisabled))
                TrySetPrivilege(right, privilege, defaultChecked, defaultDisabled);
        }
    }

    private static void CreateDefaultElements(ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled, ref CheckBoxRootElements elements)
    {
        if (elements == null)
        {
            Debug.Log($"Create default elements (Checked: {defaultChecked}, Disabled: {defaultDisabled})");
            model.LeftCheckBoxes = new List<List<SettingCheckBox>>();
            model.RightCheckBoxes = new List<List<SettingCheckBox>>();
            var headers = CreateHeaders();

            elements = new CheckBoxRootElements(
                model.Element,
                CheckBoxInstaller.CreateAllLeft1(model.LeftCheckBoxes, defaultChecked, defaultDisabled, headers),
                CheckBoxInstaller.CreateAllRight1(model.RightCheckBoxes, defaultChecked, defaultDisabled, headers)
            );
        }

        elements.RefreshElements(model.Element);
        model.LeftCheckBoxes = elements.GetLeftAll();
        model.RightCheckBoxes = elements.GetRightAll();
        model.CreatePanelCheckBox.CreateTwoPanels(elements);
    }

    private static void UsePowerGradeCpAll(ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled)
    {
        var headers = CreateHeaders();
        CreateAndDisplayPanels(model,
            CheckBoxInstaller.CreateAllLeft1(model.LeftCheckBoxes, defaultChecked, defaultDisabled, headers),
            CheckBoxInstaller.CreateAllRight1(model.RightCheckBoxes, defaultChecked, defaultDisabled, headers));
    }

    private static void UsePowerGradeDefaultTrueTrue(ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled)
     => CreateDefaultElements(model, defaultChecked, defaultDisabled, ref _defaultTrueTrue);

    private static void UsePowerGradeDefaultFalseTrue(ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled)
        => CreateDefaultElements(model, defaultChecked, defaultDisabled, ref _defaultFalseTrue);



    private static void HandlePowerNotAll(int powerValue, ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled, bool isGrade)
    {
        if (powerValue == 0)
        {
            if (isGrade)
                UsePowerGradeDefaultFalseTrue(model, false, true);
            else
                UsePowerGradeCpAll(model, false, false);
            return;
        }

        if (powerValue == ClanPrivileges.CP_ALL)
        {
            if(isGrade)
                UsePowerGradeDefaultTrueTrue(model, true, true);
            else
                UsePowerGradeCpAll(model, true, isGrade);
            return;
        }

        var (left, right) = PrepareCheckBoxLists(model, false, isGrade);
        ApplyPrivileges(ByteUtils.SplitMask(powerValue), left, right, defaultChecked, defaultDisabled);
        CreateAndDisplayPanels(model, left, right);
    }

    public static void CreateCheckboxUsePowerGrade(int powerGrade, ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled)
        => HandlePowerNotAll(powerGrade, model, defaultChecked, defaultDisabled, true);

    public static void CreateCheckboxUsePowerRanked(int powerRanked, ModelPowerCheckBox model, bool defaultChecked, bool defaultDisabled)
        => HandlePowerNotAll(powerRanked, model, defaultChecked, defaultDisabled, false);
}
