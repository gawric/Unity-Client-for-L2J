[IncomingLoginPacket(LoginServerPacketType.PlayOk)]
public sealed class PlayOkIncoming : IncomingWirePacket<PlayOkDto>
{
    protected override void OnParsed(PlayOkDto packet)
    {
        IncomingPacketActions.Game.PlayKey1 = packet.PlayOk1;
        IncomingPacketActions.Game.PlayKey2 = packet.PlayOk2;
    }

    public override void Apply(PlayOkDto packet)
    {
        LobbyFlowLog.Info(
            "PlayOk Apply playKey1=" + packet.PlayOk1 + " playKey2=" + packet.PlayOk2 +
            " state=" + (IncomingPacketActions.Manager != null ? IncomingPacketActions.Manager.GameState.ToString() : "null"));

        IncomingPacketActions.Manager.OnLoginServerPlayOk();

        if (IncomingPacketActions.Manager.GameState != GameState.READY_TO_CONNECT)
        {
            LobbyFlowLog.Warn("PlayOk skip Game.Connect — GameState=" + IncomingPacketActions.Manager.GameState);
            return;
        }
        if (IncomingPacketActions.Manager.IsSwitchingServer)
        {
            LobbyFlowLog.Warn("PlayOk skip Game.Connect — already switching server");
            return;
        }

        IncomingPacketActions.Manager.IsSwitchingServer = true;
        IncomingPacketActions.Login.Disconnect();
        LobbyFlowLog.Info("PlayOk → Login.Disconnect + Game.Connect");
        IncomingPacketActions.Game.Connect();
    }
}
