/// <summary>
/// ActionType.PartyInvite ("Rm. Invite" / /invite, action id=7 in ActionName_Classic-eu.txt) -
/// invites the currently selected player to the local player's party. The server looks the target
/// up by name (RequestJoinParty's _name field), not by object id, so a name is all that's sent;
/// the loot distribution type only matters when this creates a brand new party (the server ignores
/// it once a party already exists) and defaults to Finders Keepers, since there's no party-options
/// UI yet to choose one explicitly.
/// </summary>
public class PartyInviteAction : L2Action
{
    public override void UseAction()
    {
        if (!TargetManager.Instance.HasTarget())
        {
            return;
        }

        Entity targetEntity = TargetManager.Instance.Target.GetEntity();
        // Only other players (UserEntity) can be invited - this also naturally excludes the local
        // player (PlayerEntity), who is never targetable as a UserEntity.
        if (targetEntity == null || !(targetEntity is UserEntity))
        {
            return;
        }

        string targetName = TargetManager.Instance.Target.Identity.Name;
        if (string.IsNullOrEmpty(targetName))
        {
            return;
        }

        int distributionTypeId = PartyManager.Instance.IsInParty
            ? PartyManager.Instance.DistributionType.GetId()
            : PartyDistributionType.FindersKeepers.GetId();

        var sendPacket = CreatorPacketsUser.CreateRequestJoinParty(targetName, distributionTypeId);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPacket, enable, enable);
    }
}
