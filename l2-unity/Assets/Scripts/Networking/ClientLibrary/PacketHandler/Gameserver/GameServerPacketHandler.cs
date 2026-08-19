using System;
using UnityEngine;

public class GameServerPacketHandler : ServerPacketHandler
{
    private readonly IProtocol _protocol;
    private readonly INetworkDispatcher _dispatcher;

    public GameServerPacketHandler(IProtocol protocol, INetworkDispatcher dispatcher)
    {
        _protocol = protocol;
        _dispatcher = dispatcher;
    }

    public void Handle(IncomingRawPacket item)
    {
        int len = item.Data != null ? item.Data.Length : 0;
        INetworkModel model;
        try
        {
            if (!_protocol.TryParseGame(item.Data, item.CryptEnabled, out model))
            {
                byte raw0 = len > 0 ? item.Data[0] : (byte)0;
                LobbyFlowLog.Warn(
                    "Game RX DROP len=" + len + " crypt=" + item.CryptEnabled +
                    " raw0=0x" + raw0.ToString("X2") + " — TryParseGame failed (unknown opcode or decrypt)");
                return;
            }
        }
        catch (Exception ex)
        {
            LobbyFlowLog.Exception("Game RX parse len=" + len + " crypt=" + item.CryptEnabled, ex);
            return;
        }

        LobbyFlowLog.Info("Game RX OK " + model.GetType().Name + " len=" + len + " crypt=" + item.CryptEnabled + " → Apply");
        _dispatcher.Dispatch(model);
    }
}
