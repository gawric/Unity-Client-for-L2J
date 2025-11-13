using System.Collections.Generic;

public class PackageSendableList : ServerPacket
{

    private List<Product> _items;
    private int _playerAdena;
    private int _playerObject;

    public int CurrentMoney { get => _playerAdena; }
    public int PlayerObject { get => _playerObject; }

    public List<Product> Items { get => _items; }

    public PackageSendableList(byte[] d) : base(d)
    {
        _items = new List<Product>();
        Parse();
    }

    public override void Parse()
    {
        _playerObject = ReadI();
        _playerAdena = ReadI();
        int size = ReadI();

        for (int i = 0; i < size; i++)
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

            Product product = new Product(type1, objectId, count, type2, 0, bodyPart, enchantLevel, 1000, itemId);
            _items.Add(product);
        }

    }
}
