
public class TradeOtherAdd : ServerPacket
{
    public TradeOtherAdd(byte[] d) 
        : base(d)
    {
    }

    public override void Parse()
    {
    }
}
public class TradeItem
{
    public int ObjectId { get; set; }
    public int Location { get; set; }
    public int Enchant { get; set; }
    public int Type1 { get; set; }
    public int Type2 { get; set; }
    public long Count { get; set; }
    public int StoreCount { get; set; }
    public int Price { get; set; }
}