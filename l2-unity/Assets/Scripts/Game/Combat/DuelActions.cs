public static class DuelActions
{
    public static bool Challenge(string name, bool party = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Entity target = PlayerEntity.Instance != null ? PlayerEntity.Instance.GetTargetEntity() : null;
            if (!(target is UserEntity) || target.Identity == null)
                return false;
            name = target.Identity.Name;
        }
        GameClient game = IncomingPacketActions.Game;
        if (game == null || game.Duel.Phase != DuelPhase.None)
            return false;
        name = name.Trim();
        game.Duel.Challenge(name, party);
        game.Send(new RequestDuelStartCommand(name, party));
        return true;
    }

    public static void Respond(bool accepted)
    {
        GameClient game = IncomingPacketActions.Game;
        if (game == null || game.Duel.Phase != DuelPhase.Invitation)
            return;
        game.Send(new RequestDuelAnswerStartCommand(game.Duel.IsParty, accepted));
        if (accepted)
            game.Duel.AwaitStart();
        else
            game.Duel.Clear();
    }

    public static void Surrender()
    {
        GameClient game = IncomingPacketActions.Game;
        if (game != null && game.Duel.Phase == DuelPhase.Active)
            game.Send(new RequestDuelSurrenderCommand());
    }
}
