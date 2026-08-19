public sealed class CharacterSelectDto : IOutgoingDto
{
    public int Slot;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Slot);
        writer.WriteShort(0);
        writer.WriteI(Slot);
        writer.WriteI(Slot);
        writer.WriteI(Slot);
    }
}
