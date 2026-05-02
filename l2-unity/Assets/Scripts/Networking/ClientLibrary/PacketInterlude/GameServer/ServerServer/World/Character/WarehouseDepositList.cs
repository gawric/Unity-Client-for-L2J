using System.Collections.Generic;


public class WarehouseDepositList : ServerPacket
{
    private int _playerAdena;
    private List<Product> _items;
    private int _whType;

    public List<Product> Products { get => _items; }
    public List<Product> WhType { get => _items; }
    public int CurrentMoney { get => _playerAdena; }

    public WarehouseDepositList(byte[] d) : base(d)
    {
        _items = new List<Product>();
        Parse();
    }

    public override void Parse()
    {
        _whType = ReadSh();
        _playerAdena = ReadI();
        int size = ReadSh();

        for(int i =0; i < size; i++)
        {
            int type1 = ReadSh();
            int objectId = ReadI();
            int itemId = ReadI();
            int count = ReadI();
            int type2 = ReadSh();
            int customType1 = ReadSh();
            int bodyPart = ReadI();
            int enchantLevel = ReadSh();
            int customType2 = ReadSh();
            int unk1 = ReadSh();
            int objectId2 = ReadI();
            long augmented = ReadLOther();

            Product product = new Product(type1, objectId , count, type2, 0, bodyPart, enchantLevel, 0 , itemId);
            _items.Add(product);
        }
    }
}

public enum WhType : byte
{
    PRIVATE = 1,
    CLAN = 2,
    CASTLE = 3, 
    FREIGHT = 4,
}
