using System.Collections.Generic;

public class PackageSendableListDto : IWireDto
{

    private List<Product> _items;
    private int _playerAdena;
    private int _playerObject;

    public int CurrentMoney { get => _playerAdena; }
    public int PlayerObject { get => _playerObject; }

    public List<Product> Items { get => _items; }

    public PackageSendableListDto()
    {
        _items = new List<Product>();
    }

    public void ReadFrom(PacketReader reader)
    {
        _playerObject = reader.ReadI();
        _playerAdena = reader.ReadI();
        int size = reader.ReadI();

        for (int i = 0; i < size; i++)
        {
            int type1 = reader.ReadSh();
            int objectId = reader.ReadI();
            int itemId = reader.ReadI();
            int count = reader.ReadI();
            int type2 = reader.ReadSh();
            int customType1 = reader.ReadSh();
            int bodyPart = reader.ReadI();
            int enchantLevel = reader.ReadSh();
            int customType2 = reader.ReadSh();
            int unk1 = reader.ReadSh();
            int objectId2 = reader.ReadI();

            Product product = new Product(type1, objectId, count, type2, 0, bodyPart, enchantLevel, 1000, itemId);
            _items.Add(product);
        }

    }
}
