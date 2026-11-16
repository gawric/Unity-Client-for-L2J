public sealed class AttackRequestDto : IOutgoingDto
{
    public int ObjectId;
    public int X;
    public int Y;
    public int Z;
    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
        writer.WriteI(X);
        writer.WriteI(Y);
        writer.WriteI(Z);
        writer.WriteB(0);
    }
}
