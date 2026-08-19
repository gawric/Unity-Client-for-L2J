[IncomingLoginPacket(LoginServerPacketType.LoginOk)]
public sealed class LoginOkIncoming : IncomingWirePacket<LoginOkDto>
{
    protected override void OnParsed(LoginOkDto packet)
    {
        IncomingPacketActions.Login.SessionKey1 = packet.SessionKey1;
        IncomingPacketActions.Login.SessionKey2 = packet.SessionKey2;
        IncomingPacketActions.Game.SessionKey1 = packet.SessionKey1;
        IncomingPacketActions.Game.SessionKey2 = packet.SessionKey2;
    }

    public override void Apply(LoginOkDto packet)
    {
        IncomingPacketActions.Login.OnAuthAllowed();
    }
}
