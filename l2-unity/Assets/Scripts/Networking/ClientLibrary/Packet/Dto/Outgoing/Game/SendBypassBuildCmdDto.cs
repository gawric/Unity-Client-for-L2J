public sealed class SendBypassBuildCmdDto : IOutgoingDto
{
    public string Command;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherS(Command);
    }
}
