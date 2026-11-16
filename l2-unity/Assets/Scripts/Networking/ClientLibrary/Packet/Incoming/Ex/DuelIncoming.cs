[IncomingExPacket(GameExServerPacketType.ExDuelAskStart)]
public sealed class ExDuelAskStartIncoming : IncomingWirePacket<ExDuelAskStartDto>
{
    public override void Apply(ExDuelAskStartDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.Game?.Duel.Invite(packet.Name, packet.Party);
            LocalDemoDuelProbe.TryRespond(packet.Name);
        });
    }
}

[IncomingExPacket(GameExServerPacketType.ExDuelReady)]
public sealed class ExDuelReadyIncoming : IncomingWirePacket<ExDuelReadyDto>
{
    public override void Apply(ExDuelReadyDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Duel.Ready(packet.Party));
    }
}

[IncomingExPacket(GameExServerPacketType.ExDuelStart)]
public sealed class ExDuelStartIncoming : IncomingWirePacket<ExDuelStartDto>
{
    public override void Apply(ExDuelStartDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Duel.Start(packet.Party));
    }
}

[IncomingExPacket(GameExServerPacketType.ExDuelEnd)]
public sealed class ExDuelEndIncoming : IncomingWirePacket<ExDuelEndDto>
{
    public override void Apply(ExDuelEndDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            if (UnityAutoLogin.IsActive)
                UnityEngine.Debug.Log("[LocalDemoDuel] server-end");
            IncomingPacketActions.Game?.Duel.Clear();
        });
    }
}

[IncomingExPacket(GameExServerPacketType.ExDuelUpdateUserInfo)]
public sealed class ExDuelUpdateUserInfoIncoming : IncomingWirePacket<ExDuelUpdateUserInfoDto>
{
    public override void Apply(ExDuelUpdateUserInfoDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Duel.Update(packet.Fighter));
    }
}
