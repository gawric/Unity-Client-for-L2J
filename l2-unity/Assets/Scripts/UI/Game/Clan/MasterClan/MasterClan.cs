using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MasterClan 
{
    private const string ROLE_CREATE = "Data/UI/Clan/Role_create";
    private const string MEMBER_ONLINE = "Data/UI/Clan/Clan_sword_online";
    private const string MEMBER_OFFLINE = "Data/UI/Clan/Clan_sword_offline";
    private const string DEFAULT_OFFLINE_COLOR = "#B3B2B2";
    private string _table_select_name_member = string.Empty;

    public string GetSelectMemberName()
    {
        return _table_select_name_member;
    }
    public void ForEachClan( ICreatorTables _creatorTableWindows)
    {
        List<TableColumn> listTableColumn = GetEmptyData();
        _creatorTableWindows.CreateTable(listTableColumn);
    }

    public List<TableColumn> GetEmptyData()
    {
        var namesList = new List<string>();
        var conditionsList = new List<string>();
        var levelList = new List<string>();
        var activity = new List<string>();


        for (int i = 0; i < 7; i++)
        {
            namesList.Add("");
            conditionsList.Add("");
            levelList.Add("");
            activity.Add("");
        }

        return GetColumns(namesList, conditionsList, levelList, activity);
    }

    public List<string> SetDropdownList(DropdownField dropdown , string clanName)
    {
        if (string.IsNullOrEmpty(clanName))
        {
            dropdown.value = null;
            dropdown.choices = null;
            return null;
        }
        else
        {
            var _listDropDown = new List<string> { "Main Clan - " + clanName };
            dropdown.value = "Main Clan - " + clanName;
            dropdown.choices = _listDropDown;
            return _listDropDown;
        }

    }


    public void UpdateMembersTable(ClanMember clanMember, ICreatorTables _creatorTableWindows)
    {
        _creatorTableWindows.UpdateRowByName(clanMember.MemberName , "" , new string[4] { clanMember.MemberName, clanMember.Level.ToString(), ROLE_CREATE, GetOnline(clanMember.Online) });
    }
    public void CreateMembersTable(List<ClanMember> listMemebers, ICreatorTables _creatorTableWindows)
    {

        var namesList = new List<string>();
        var lvlList = new List<string>();
        var roleList = new List<string>();
        var activity = new List<string>();


        PopulateMemberData(listMemebers, namesList, lvlList, roleList, activity);
        EnsureMinimumRowCount(namesList, lvlList, roleList, activity, 7);


        var tableColumns = GetColumns(namesList, lvlList, roleList, activity);
        SetOfflineMemberColors(listMemebers, tableColumns);

        _creatorTableWindows.UpdateTableData(tableColumns);
    }

    public void DeleteMemeberTable(string memberName , PledgeShowMemberListAll packet , ICreatorTables _creatorTableWindows)
    {
        if(packet != null)
        {
            var deletemember = packet.Members.FirstOrDefault(m => m.MemberName == memberName);
            packet.Members?.Remove(deletemember);
            _creatorTableWindows.RemoveByName(memberName);
        }

    }

    public void AddMemberData(ClanMember clanMember, PledgeShowMemberListAll packet, ICreatorTables _creatorTableWindows)
    {
        packet.Members.Add(clanMember);
        _creatorTableWindows.AddRow(new string[4] { clanMember.MemberName , clanMember.Level.ToString() , ROLE_CREATE, GetOnline(clanMember.Online) });
    }

    private void PopulateMemberData(List<ClanMember> members, List<string> names,
    List<string> levels, List<string> roles, List<string> activities)
    {
        foreach (var member in members)
        {
            names.Add(member.MemberName);
            levels.Add(member.Level.ToString());
            roles.Add(ROLE_CREATE);
            SetOnline(member, activities);
        }
    }

    private void EnsureMinimumRowCount(List<string> names, List<string> levels,
    List<string> roles, List<string> activities, int minimumCount)
    {
        int currentCount = names.Count;
        if (currentCount >= minimumCount) return;

        int emptySlots = minimumCount - currentCount;
        for (int i = 0; i < emptySlots; i++)
        {
            names.Add("");
            levels.Add("");
            roles.Add("");
            activities.Add("");
        }
    }

    private void SetOfflineMemberColors(List<ClanMember> members, List<TableColumn> columns)
    {
        UserInfo usrInfo = StorageNpc.getInstance().GetFirstUser();
        for (int i=0; i < members.Count; i++)
        {
            ClanMember member = members[i];
            if(usrInfo.PlayerInfoInterlude.Identity.Name != member.MemberName)
            {
                 columns[0].SetColor(i + member.MemberName, DEFAULT_OFFLINE_COLOR);
                 columns[1].SetColor(i + member.Level.ToString(), DEFAULT_OFFLINE_COLOR);
            }
            //if (member.Online == 0)
            //{
            // columns[0].SetColor(i + member.MemberName, "#B3B2B2");
            // columns[1].SetColor(i + member.Level.ToString(), "#B3B2B2");
            //}

        }
    }

    private void SetOnline(ClanMember member, List<string> activity)
    {
        if (member.Online == 0)
        {
            activity.Add(MEMBER_OFFLINE);

        }
        else
        {
            activity.Add(MEMBER_ONLINE);
        }
    }

    public string GetOnline(int online)
    {
        return (online >= 1) ? MEMBER_ONLINE : MEMBER_OFFLINE;
    }

    private string  GetOnlineHexColors(int online)
    {
        return (online >= 1) ? "" : "#B3B2B2";
    }



    private List<TableColumn> GetColumns(List<string> namesList , List<string> lvlList, List<string> roleList, List<string> activity)
    {
        var name = new TableColumn(false, "Name", 100, namesList, 13);
        var lvl = new TableColumn(true, "Lv.", 0, lvlList, 0);
        var role = new TableColumn(true, "Role", 15, roleList, 0, true);
        var act = new TableColumn(true, "Activity.", 20, activity, 0, true);

        return new List<TableColumn> { name, lvl, role, act };
    }

    public void DeleteAllMember(ICreatorTables creatorTableWindows)
    {
        creatorTableWindows.ClearTable();
    }
    public void UpdateMemberData(ClanMember clanMember, PledgeShowMemberListAll packet , ICreatorTables creatorTableWindows)
    {
        if (packet != null && clanMember != null)
        {

            var memberToUpdate = packet.Members.FirstOrDefault(m => m.MemberName == clanMember.MemberName);

            if (memberToUpdate != null)
            {

                memberToUpdate.MemberName = clanMember.MemberName;
                memberToUpdate.Level = clanMember.Level;
                memberToUpdate.ClassId = clanMember.ClassId;
                memberToUpdate.Sex = clanMember.Sex;
                memberToUpdate.Race = clanMember.Race;
                memberToUpdate.Online = clanMember.Online;
                //memberToUpdate. = packetUpdate.PledgeType;
                //memberToUpdate.HasSponsor = packetUpdate.HasSponsor;

                UpdateMembersTable(clanMember, creatorTableWindows);

               // CreateMembersTable(packet.Members, creatorTableWindows);
            }
        }
    }

  

    public void SelectMember(int selectIndex, string select_text)
    {

        if (string.IsNullOrEmpty(select_text))
        {
            Debug.LogWarning("Input string is null or empty");
            return;
        }

        string[] row_text = select_text.Split("___");


        if (row_text.Length < 2)
        {
            Debug.LogWarning($"Invalid input format. Expected at least 2 parts separated by '___', but got {row_text.Length} parts");
            return;
        }


        if (string.IsNullOrEmpty(row_text[1]))
        {
            Debug.LogWarning("Second part of the split string is empty");
            return;
        }


        _table_select_name_member = row_text[1];
    }
}
