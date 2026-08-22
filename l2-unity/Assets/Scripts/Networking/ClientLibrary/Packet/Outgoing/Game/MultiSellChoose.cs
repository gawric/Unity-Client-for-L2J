[OutgoingCommandPacket(typeof(MultiSellChooseCommand))]
public sealed class MultiSellChoose : OutgoingWirePacket<MultiSellChooseDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.MultiSellChoose;

    public MultiSellChoose(MultiSellChooseCommand command) : this(command.ListId, command.EntryId, command.Amount) { }

    public MultiSellChoose(int listId, int entryId, int amount)
    {
        Dto.ListId = listId;
        Dto.EntryId = entryId;
        Dto.Amount = amount;
    }
}
