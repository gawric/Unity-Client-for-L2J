
public class TradeOtherAdd : ServerPacket
{
    public TradeItem Item {get;set;}
    public AbstractItem BaseItem { get;set; }

    public TradeOtherAdd(byte[] d) 
        : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        Item = new TradeItem();
        
        Item.Count = ReadSh();
        Item.Type1 = ReadSh();
        Item.ObjectId = ReadI();
        var displayId = ReadI();
        Item.Count = ReadSh();
        Item.Type2 = ReadSh();
        var customType = ReadSh();
        Item.BodyPart = ReadI();
        Item.Enchant = ReadSh();
        var blank = ReadSh();
        var customType2 = ReadSh();

        //var itemName = ItemNameTable.Instance.GetItemName(displayId);

        BaseItem = ItemTable.Instance.GetEtcItem(displayId);
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

    public int BodyPart { get; set; }
}