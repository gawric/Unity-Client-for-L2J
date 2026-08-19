[IncomingLoginPacket(LoginServerPacketType.LoginFail)]
public sealed class LoginFailIncoming : IncomingWirePacket<LoginFailDto>
{
    public override void Apply(LoginFailDto packet)
    {
        IncomingPacketActions.LoginWindow.ShowErrorText(packet.Message);
        IncomingPacketActions.Login.Disconnect();
    }
}
