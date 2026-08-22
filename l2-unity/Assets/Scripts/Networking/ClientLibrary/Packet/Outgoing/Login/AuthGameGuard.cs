[OutgoingCommandPacket(typeof(AuthGameGuardCommand))]
public sealed class AuthGameGuard : OutgoingWirePacket<AuthGameGuardDto>
{
    protected override byte Opcode => (byte)LoginClientPacketType.AuthGameGuard;

    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.Login;
    protected override int LoginExtraZeroBytes => 1;

    public AuthGameGuard(AuthGameGuardCommand command) : this(command.SessionId, command.Gg) { }

    public AuthGameGuard(int sessionId, int[] gg)
    {
        Dto.SessionId = sessionId;
        Dto.Gg0 = gg[0];
        Dto.Gg1 = gg[1];
        Dto.Gg2 = gg[2];
        Dto.Gg3 = gg[3];
    }
}
