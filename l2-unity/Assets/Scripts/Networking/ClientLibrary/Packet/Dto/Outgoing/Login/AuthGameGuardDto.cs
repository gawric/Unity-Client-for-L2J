public sealed class AuthGameGuardDto : IOutgoingDto
{
    public int SessionId;
    public int Gg0;
    public int Gg1;
    public int Gg2;
    public int Gg3;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(SessionId);
        writer.WriteI(Gg0);
        writer.WriteI(Gg1);
        writer.WriteI(Gg2);
        writer.WriteI(Gg3);
        writer.WriteB(0);
        writer.WriteB(0);
        writer.WriteB(0);
    }
}
