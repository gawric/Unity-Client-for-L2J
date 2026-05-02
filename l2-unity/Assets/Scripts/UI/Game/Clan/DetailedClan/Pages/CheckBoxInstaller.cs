using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CheckBoxInstaller
{
    private static (string name, int privilege)[] PrivilegeLeftItems = new[]
    {
        ("Invite", ClanPrivileges.CP_CL_JOIN_CLAN),
        ("Manage Titles", ClanPrivileges.CP_CL_GIVE_TITLE),
        ("Warehouse Search", ClanPrivileges.CP_CL_VIEW_WAREHOUSE),
        ("Manage Ranks", ClanPrivileges.CP_CL_MANAGE_RANKS),
        ("ClanWar", ClanPrivileges.CP_CL_PLEDGE_WAR)
    };

    private static (string name, int privilege)[] PrivilegeRightItems = new[]
    {
        ("Dismiss", ClanPrivileges.CP_CL_DISMISS),
        ("Edit Crest", ClanPrivileges.CP_CL_REGISTER_CREST),
        ("Basic rights", ClanPrivileges.CP_CL_MASTER_RIGHTS),
        ("Manage levels", ClanPrivileges.CP_CL_MANAGE_LEVELS)
    };

    private static (string name, int privilege)[] ClanHallLeftItems = new[]
    {
        ("Entry/Exit Rights", ClanPrivileges.CP_CH_OPEN_DOOR),
        ("Use Functions", ClanPrivileges.CP_CH_USE_FUNCTIONS),
        ("Auction", ClanPrivileges.CP_CH_AUCTION)
    };

    private static (string name, int privilege)[] ClanHallRightItems = new[]
    {
        ("Right to Dismiss", ClanPrivileges.CP_CH_DISMISS),
        ("Set Functions", ClanPrivileges.CP_CH_SET_FUNCTIONS)
    };

    private static (string name, int privilege)[] CastleLeftItems = new[]
    {
        ("Entry/Exit Rights", ClanPrivileges.CP_CS_OPEN_DOOR),
        ("Siege War", ClanPrivileges.CP_CS_MANAGE_SIEGE),
        ("Use Functions", ClanPrivileges.CP_CS_USE_FUNCTIONS),
        ("Set Functions", ClanPrivileges.CP_CS_SET_FUNCTIONS)
    };

    private static (string name, int privilege)[] CastleRightItems = new[]
    {
        ("Right to Dismiss", ClanPrivileges.CP_CS_DISMISS),
        ("Manage Taxes", ClanPrivileges.CP_CS_TAXES),
        ("Mercenaries", ClanPrivileges.CP_CS_MERCENARIES)
    };

    private static List<SettingCheckBox> CreateCheckBoxList(SettingCheckBox header,
        (string name, int privilege)[] items, bool isChecked, bool isDisabled)
    {
        header.SetChecked(isChecked);
        header.SetDisabled(isDisabled);

        var list = new List<SettingCheckBox> { header };
        foreach (var item in items)
        {
            list.Add(new SettingCheckBox(item.name, isChecked, isDisabled, item.privilege, header));
        }
        return list;
    }

    private static List<SettingCheckBox> CreateRegularList(SettingCheckBox header,
        (string name, int privilege)[] items, bool isChecked, bool isDisabled)
    {
        var list = new List<SettingCheckBox>();
        foreach (var item in items)
        {
            list.Add(new SettingCheckBox(item.name, isChecked, isDisabled, item.privilege, header));
        }
        return list;
    }

    public static List<SettingCheckBox> InitChecBoxPrivilegesLeft(bool isChecked, bool isDisabled, SettingCheckBox header)
        => CreateCheckBoxList(header, PrivilegeLeftItems, isChecked, isDisabled);

    public static List<SettingCheckBox> InitChecBoxPrivilegesRight(bool isChecked, bool isDisabled, SettingCheckBox header)
        => CreateRegularList(header, PrivilegeRightItems, isChecked, isDisabled);

    public static List<SettingCheckBox> InitChecBoxClanHallLeft(bool isChecked, bool isDisabled, SettingCheckBox header)
        => CreateCheckBoxList(header, ClanHallLeftItems, isChecked, isDisabled);

    public static List<SettingCheckBox> InitChecBoxClanHallRight(bool isChecked, bool isDisabled, SettingCheckBox header)
        => CreateRegularList(header, ClanHallRightItems, isChecked, isDisabled);

    public static List<SettingCheckBox> InitChecBoxCastleLeft(bool isChecked, bool isDisabled, SettingCheckBox header)
        => CreateCheckBoxList(header, CastleLeftItems, isChecked, isDisabled);

    public static List<SettingCheckBox> InitChecBoxCastleRight(bool isChecked, bool isDisabled, SettingCheckBox header)
        => CreateRegularList(header, CastleRightItems, isChecked, isDisabled);

    public static List<List<SettingCheckBox>> CreateAllLeft1(List<List<SettingCheckBox>> checkBoxes, bool isChecked, bool isDisabled, SettingCheckBox[] headers)
    {
        checkBoxes.AddRange(new List<List<SettingCheckBox>>
        {
            InitChecBoxPrivilegesLeft(isChecked, isDisabled, headers[0]),
            InitChecBoxClanHallLeft(isChecked, isDisabled, headers[1]),
            InitChecBoxCastleLeft(isChecked, isDisabled, headers[2])
        });
        return checkBoxes;
    }

    public static List<List<SettingCheckBox>> CreateAllRight1(List<List<SettingCheckBox>> checkBoxes, bool isChecked, bool isDisabled, SettingCheckBox[] headers)
    {
        checkBoxes.AddRange(new List<List<SettingCheckBox>>
        {
            InitChecBoxPrivilegesRight(isChecked, isDisabled, headers[0]),
            InitChecBoxClanHallRight(isChecked, isDisabled, headers[1]),
            InitChecBoxCastleRight(isChecked, isDisabled, headers[2])
        });
        return checkBoxes;
    }
}
