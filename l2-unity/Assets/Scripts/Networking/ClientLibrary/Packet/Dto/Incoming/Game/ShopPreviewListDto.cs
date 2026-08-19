using System.Collections.Generic;
using UnityEngine;

public class ShopPreviewListDto : IWireDto
{
    private int _money;
    private int _listId;

    private List<Product> _listProduct;
    public List<Product> Products { get => _listProduct; }
    public int CurrentMoney { get => _money; }
    public int ListID { get => _listId; }
    public ShopPreviewListDto()
    {
        _listProduct = new List<Product>();
    }

    public void ReadFrom(PacketReader reader)
    {
        var unk1 = reader.ReadB();
        var unk2 = reader.ReadB();
        var unk3 = reader.ReadB();
        var unk4 = reader.ReadB();

        _money = reader.ReadI(); //current money
        _listId = reader.ReadI();
        int size = reader.ReadSh();

        for (int i = 0; i < size; i++)
        {
            int itemId = reader.ReadI();
            int itemType2 = reader.ReadSh();
            int bodyPart = reader.ReadSh();
            int price = reader.ReadI();

            _listProduct.Add(new Product(0, 0, 1, itemType2, 0, bodyPart, 0, price, itemId));
        }

    }

}
