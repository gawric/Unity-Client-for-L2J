using System;
using System.Text;
using UnityEngine;

[OutgoingCommandPacket(typeof(RequestAuthLoginCommand))]
public sealed class RequestAuthLogin : OutgoingWirePacket<RequestAuthLoginDto>
{
    private const int TestRsaSize = 113;
    private const int MaxAccountBytes = 15;
    private const int MaxPasswordBytes = 15;
    private bool _valid;

    protected override byte Opcode => (byte)LoginClientPacketType.RequestAuthLogin;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.Login;
    protected override int LoginExtraZeroBytes => 4;

    public RequestAuthLogin(RequestAuthLoginCommand command) : this(command.Account, command.Password, command.Response) { }

    public RequestAuthLogin(string account, string password, int responce)
    {
        if (!TryCreate(account, password))
            return;

        byte[] accountBytes = Encoding.UTF8.GetBytes(account ?? string.Empty);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
        byte[] testRsa = new byte[TestRsaSize];
        Array.Copy(accountBytes, 0, testRsa, 79, accountBytes.Length);
        Array.Copy(passwordBytes, 0, testRsa, 93, passwordBytes.Length);
        Dto.RsaBlock = IncomingPacketActions.Login.RSACrypt.EncryptRSABlockNoPaddingBoundleCastle(testRsa);
        Dto.Response = responce;
        _valid = true;
    }

    public override byte[] GetData()
    {
        if (!_valid)
            return new byte[0];
        return base.GetData();
    }

    private static bool TryCreate(string account, string password)
    {
        byte[] accountBytes = Encoding.UTF8.GetBytes(account ?? string.Empty);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
        if (accountBytes.Length > MaxAccountBytes)
        {
            Debug.LogWarning("Account name too long: " + accountBytes.Length + " bytes (max " + MaxAccountBytes + ").");
            return false;
        }
        if (passwordBytes.Length > MaxPasswordBytes)
        {
            Debug.LogWarning("Password too long: " + passwordBytes.Length + " bytes (max " + MaxPasswordBytes + ").");
            return false;
        }
        return true;
    }
}
