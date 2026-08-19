public sealed class AuthLoginDto : IOutgoingDto
{
    public string Account;
    public int PlayKey1;
    public int PlayKey2;
    public int LoginKey1;
    public int LoginKey2;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteSOther(Account);
        writer.WriteChar((char)0);
        writer.WriteI(PlayKey2);
        writer.WriteI(PlayKey1);
        writer.WriteI(LoginKey1);
        writer.WriteI(LoginKey2);
    }
}
