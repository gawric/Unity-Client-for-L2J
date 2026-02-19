using Org.BouncyCastle.Bcpg;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DataProviderClanInfo : AbstractDataFunction
{
    private const string IMG_ONLINE = "Data/UI/Clan/PConline_up";
    private const string IMG_OFFLINE= "Data/UI/Clan/PConline_down";
    public void SetMasterClanInfo(VisualElement container, PledgeShowMemberListAll packet)
    {
        if (container != null)
        {
            Label leaderLabel = container.Q<Label>("labelLeaderName");
            AddElementIfNotEmpty(leaderLabel, leaderLabel, packet.SubPledgeLeaderName);

            Label titleLabel = container.Q<Label>("labelClanName");
            AddElementIfNotEmpty(titleLabel, titleLabel, packet.PledgeName);


            Label lvllabel = container.Q<Label>("lvlName");
            AddElementIfNotEmpty(lvllabel, lvllabel, packet.Level.ToString());

            int countOnline = 0;
            int allOnline = 0;

            if (packet.Members != null)
            {
                 countOnline = packet.Members.Count(x => x.Online > 0);
                 allOnline = packet.Members.Count;
            }

            Label onlineLabel = container.Q<Label>("labelCountOnline");
            AddElementIfNotEmpty(onlineLabel, onlineLabel, "(" + countOnline + "/"+ allOnline + ")");

            var icon = container.Q<VisualElement>("ImageOnline");

            if (countOnline == 0)
            {

                AddElementIfNotNull(icon, icon, IconManager.Instance.LoadTextureOtherSources(IMG_OFFLINE));
            }
            else
            {
                AddElementIfNotNull(icon, icon, IconManager.Instance.LoadTextureOtherSources(IMG_ONLINE));
            }


        }
    }

    public void UpdateClanInfo(VisualElement container, PledgeInfo packet)
    {
        Label titleLabel = container.Q<Label>("labelClanName");
        AddElementIfNotEmpty(titleLabel, titleLabel, packet.ClanName);
    }

    public void SetMemberInfo(VisualElement container, PledgeReceiveMemberInfo packet ,  PledgeShowMemberListAll packetAll)
    {
        Label titleName = container.Q<Label>("titleLabel");
        AddElementIfNotEmpty(titleName, titleName, packet.Name);

        Label titlePlayerName = container.Q<Label>("titlePlayerLabel");
        AddElementIfNotEmpty(titlePlayerName, titlePlayerName, packet.Title);

        string memberRank = packetAll.SubPledgeLeaderName == packet.Name? "Leader" : "Member";
 

        Label rankLabel = container.Q<Label>("rankLabel");
        AddElementIfNotEmpty(rankLabel, rankLabel, memberRank);


        Label statuslabel = container.Q<Label>("statusLabel");
        AddElementIfNotEmpty(statuslabel, statuslabel, "Main Clan - " + packet.SubPledgeName);
    }

    public void SetSubjectClanInfo(VisualElement container , PledgeShowMemberListAll packetAll)
    {
        Label titleName = container.Q<Label>("titleLabel");
        AddElementIfNotEmpty(titleName, titleName, packetAll.PledgeName);
    }
}
