public sealed class CharacterDeleteDto : IOutgoingDto
{
    public int Slot;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Slot);
    }
}
