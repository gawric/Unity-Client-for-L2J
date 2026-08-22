using System;

public class LoginServerPacketHandler : ServerPacketHandler
{
    private readonly IProtocol _protocol;
    private readonly INetworkDispatcher _dispatcher;

    public LoginServerPacketHandler(IProtocol protocol, INetworkDispatcher dispatcher)
    {
        _protocol = protocol;
        _dispatcher = dispatcher;
    }

    public void Handle(IncomingRawPacket item)
    {
        INetworkModel model;
        try
        {
            if (!_protocol.TryParseLogin(item.Data, item.Init, item.CryptEnabled, out model))
            {
                byte raw0 = item.Data != null && item.Data.Length > 0 ? item.Data[0] : (byte)0;
                LobbyFlowLog.Warn(
                    "Login RX DROP len=" + (item.Data != null ? item.Data.Length : 0) +
                    " init=" + item.Init + " crypt=" + item.CryptEnabled +
                    " raw0=0x" + raw0.ToString("X2"));
                return;
            }
        }
        catch (Exception ex)
        {
            LobbyFlowLog.Exception("Login RX parse", ex);
            return;
        }

        LobbyFlowLog.Info("Login RX OK " + model.GetType().Name + " → Apply");
        _dispatcher.Dispatch(model);
    }
}
