public sealed class RequestBypassToServerDto : IOutgoingDto
{
    public string Command;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteSOther(Command);
    }
}
