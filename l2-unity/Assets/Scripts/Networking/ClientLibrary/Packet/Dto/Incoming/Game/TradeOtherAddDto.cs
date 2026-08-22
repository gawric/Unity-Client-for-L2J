/// <summary>
/// TradeOtherAdd (see server's TradeOtherAdd.java) - the other side of an active trade added one
/// item to their offer. Always describes exactly one item; the wire format leads with a hardcoded
/// item-count short (always 1) before the item fields themselves.
/// </summary>
public class TradeOtherAddDto : IWireDto
{
    public int Type1 { get; private set; }
    public int ObjectId { get; private set; }
    public int ItemId { get; private set; }
    public int Count { get; private set; }
    public int Type2 { get; private set; }
    public int CustomType1 { get; private set; }
    public int BodyPart { get; private set; }
    public int EnchantLevel { get; private set; }
    public int CustomType2 { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        reader.ReadSh(); // item count, always 1 for this packet
        Type1 = reader.ReadSh();
        ObjectId = reader.ReadI();
        ItemId = reader.ReadI();
        Count = reader.ReadI();
        Type2 = reader.ReadSh();
        CustomType1 = reader.ReadSh();
        BodyPart = reader.ReadI();
        EnchantLevel = reader.ReadSh();
        reader.ReadSh(); // blank
        CustomType2 = reader.ReadSh();
    }
}
