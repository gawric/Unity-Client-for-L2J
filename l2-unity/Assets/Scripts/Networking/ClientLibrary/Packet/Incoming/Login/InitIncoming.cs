[IncomingLoginPacket(LoginServerPacketType.Init)]
public sealed class InitIncoming : IncomingWirePacket<InitDto>
{
    protected override void OnParsed(InitDto packet)
    {
        IncomingPacketActions.Login.SetRSAKey(packet.PublicKey);
        IncomingPacketActions.Login.SetBlowFishKey(packet.BlowfishKey);
        IncomingPacketActions.Login.SetSessionId(packet.SessionId);
        IncomingPacketActions.Login.CompleteInitPacket();
        IncomingPacketActions.Login.Send(new AuthGameGuardCommand(packet.SessionId, packet.GG));
    }

    public override void Apply(InitDto packet)
    {
    }
}
