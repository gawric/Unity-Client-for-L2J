public sealed class RequestRecipeBookOpenDto : IOutgoingDto
{
    public int IsDwarven;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(IsDwarven);
    }
}
