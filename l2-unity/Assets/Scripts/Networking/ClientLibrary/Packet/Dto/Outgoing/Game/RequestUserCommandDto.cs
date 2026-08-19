public sealed class RequestUserCommandDto : IOutgoingDto
{
    public int CommandId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(CommandId);
    }
}
