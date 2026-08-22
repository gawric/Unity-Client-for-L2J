[OutgoingCommandPacket(typeof(AuthLoginCommand))]
public sealed class AuthLogin : OutgoingWirePacket<AuthLoginDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.AuthLogin;

    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameOverwriteOpcode;

    public AuthLogin(AuthLoginCommand command) : this(command.Account, command.PlayKey1, command.PlayKey2, command.LoginKey1, command.LoginKey2) { }

    public AuthLogin(string account, int playKey1, int playKey2, int loginKey1, int loginKey2)
    {
        Dto.Account = account;
        Dto.PlayKey1 = playKey1;
        Dto.PlayKey2 = playKey2;
        Dto.LoginKey1 = loginKey1;
        Dto.LoginKey2 = loginKey2;
    }
}
