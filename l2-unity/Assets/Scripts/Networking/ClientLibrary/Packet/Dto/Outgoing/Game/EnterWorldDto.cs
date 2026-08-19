public sealed class EnterWorldDto : IOutgoingDto
{
    public void WriteTo(PacketWriter writer)
    {
        writer.WriteB(new byte[32]);
        writer.WriteI(1);
        writer.WriteI(1);
        writer.WriteI(1);
        writer.WriteI(1);
        writer.WriteB(new byte[32]);
        writer.WriteI(1);
        for (int i = 0; i < 5; i++)
        {
            for (int o = 0; o < 4; o++)
                writer.WriteB(1);
        }
    }
}
