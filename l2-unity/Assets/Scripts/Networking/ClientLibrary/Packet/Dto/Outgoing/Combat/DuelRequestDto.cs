public sealed class RequestDuelStartDto : IOutgoingDto
{
    public string Name;
    public bool Party;
    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherShort(0x27);
        writer.WriteOtherS(Name);
        writer.WriteI(Party ? 1 : 0);
    }
}

public sealed class RequestDuelAnswerStartDto : IOutgoingDto
{
    public bool Party;
    public bool Accepted;
    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherShort(0x28);
        writer.WriteI(Party ? 1 : 0);
        writer.WriteI(0);
        writer.WriteI(Accepted ? 1 : 0);
    }
}

public sealed class RequestDuelSurrenderDto : IOutgoingDto
{
    public void WriteTo(PacketWriter writer) { writer.WriteOtherShort(0x30); }
}
