[IncomingLoginPacket(LoginServerPacketType.GGAuth)]
public sealed class GGAuthIncoming : IncomingWirePacket<GGAuthDto>
{
    protected override void OnParsed(GGAuthDto packet)
    {
        IncomingPacketActions.Login.Send(new RequestAuthLoginCommand(
            IncomingPacketActions.Login.Account,
            IncomingPacketActions.Login.Password,
            packet.Response));
    }

    public override void Apply(GGAuthDto packet)
    {
    }
}
