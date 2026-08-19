using UnityEngine;

[IncomingGamePacket(GameServerPacketType.InterludeKeyPacket)]
public sealed class InterludeKeyIncoming : IncomingWirePacket<InterludeKeyDto>
{
    private bool _keyAuthCompleted;

    protected override void OnParsed(InterludeKeyDto packet)
    {
        LobbyFlowLog.Info(
            "InterludeKey OnParsed authAllowed=" + packet.AuthAllowed +
            " useBlowfish=" + packet.UseBlowfish + " serverId=" + packet.ServerId +
            " alreadyDone=" + _keyAuthCompleted);

        if (_keyAuthCompleted || !packet.AuthAllowed)
        {
            if (!packet.AuthAllowed)
                LobbyFlowLog.Warn("InterludeKey AuthAllowed=false — wait Apply to disconnect");
            return;
        }

        if (packet.UseBlowfish)
        {
            byte[] equalsKey = BlowFishStaticKey.GetCreateFullKeyBlowFish(packet.BlowFishKey);
            IncomingPacketActions.Game.EnableCrypt(equalsKey);
            LobbyFlowLog.Info("InterludeKey crypt enabled");
        }

        LobbyFlowLog.Info("TX AuthLogin account=" + IncomingPacketActions.Login.Account);
        IncomingPacketActions.Game.Send(new AuthLoginCommand(
            IncomingPacketActions.Login.Account,
            IncomingPacketActions.Game.PlayKey1,
            IncomingPacketActions.Game.PlayKey2,
            IncomingPacketActions.Game.SessionKey1,
            IncomingPacketActions.Game.SessionKey2));
        _keyAuthCompleted = true;
    }

    public override void Apply(InterludeKeyDto packet)
    {
        if (packet.AuthAllowed)
            return;

        LobbyFlowLog.Error("InterludeKey Apply REJECT — disconnect login+game");
        IncomingPacketActions.Game.Disconnect();
        IncomingPacketActions.Login.Disconnect();
        _keyAuthCompleted = true;
    }
}
