public sealed class RequestSay2Dto : IOutgoingDto
{
    public ChatTypeData Data;
    public string Message;
    public string Name;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherS(Message);
        writer.WriteI(Data.Type);
        writer.WriteOtherS(Name);
    }
}
