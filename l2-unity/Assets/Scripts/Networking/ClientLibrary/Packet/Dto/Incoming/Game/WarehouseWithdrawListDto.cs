using System.Collections.Generic;
using UnityEngine;

public class WarehouseWithdrawListDto : IWireDto
{
    private int _playerAdena;
    private List<Product> _items;
    private int _whType;

    public List<Product> Products { get => _items; }
    public List<Product> WhType { get => _items; }
    public int CurrentMoney { get => _playerAdena; }

    public WarehouseWithdrawListDto()
    {
        _items = new List<Product>();
    }

    public static WarehouseWithdrawListDto From(int whType, int adena, List<Product> products)
    {
        WarehouseWithdrawListDto dto = new WarehouseWithdrawListDto { _whType = whType, _playerAdena = adena };
        if (products != null) dto._items.AddRange(products);
        return dto;
    }

    public void ReadFrom(PacketReader reader)
    {
        _whType = reader.ReadSh();
        _playerAdena = reader.ReadI();
        int size = reader.ReadSh();

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
            long augmented = reader.ReadLOther();

            Product product = new Product(type1, objectId, count, type2, 0, bodyPart, enchantLevel, 0, itemId);
            _items.Add(product);
        }
    }
}
