public sealed class ValidatePositionDto : IOutgoingDto
{
    public int X;
    public int Y;
    public int Z;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(X);
        writer.WriteI(Y);
        writer.WriteI(Z);
        writer.WriteI(0);
        writer.WriteI(0);
    }
}
