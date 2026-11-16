public static class PartyActions
{
    public static bool Invite(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Entity target = PlayerEntity.Instance != null ? PlayerEntity.Instance.GetTargetEntity() : null;
            if (!(target is UserEntity) || target.Identity == null)
                return false;
            name = target.Identity.Name;
        }
        GameClient game = IncomingPacketActions.Game;
        if (game == null)
            return false;
        int distribution = game.Party.IsInParty ? game.Party.DistributionType : 0;
        game.Send(new RequestJoinPartyCommand(name.Trim(), distribution));
        return true;
    }

    public static void Leave()
    {
        IncomingPacketActions.Game?.Send(new RequestWithDrawalPartyCommand());
    }
}

public sealed class PartyInviteAction : L2Action
{
    public override void UseAction() { PartyActions.Invite(null); }
}

public sealed class PartyLeaveAction : L2Action
{
    public override void UseAction() { PartyActions.Leave(); }
}
