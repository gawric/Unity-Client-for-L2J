using System.Collections.Generic;
using UnityEngine;

public class ShopPreviewList : ServerPacket
{
    private int _money;
    private int _listId;

    private List<Product> _listProduct;
    public List<Product> Products { get => _listProduct; }
    public int CurrentMoney { get => _money; }
    public int ListID { get => _listId; }
    public ShopPreviewList(byte[] d) : base(d)
    {
        _listProduct = new List<Product>();
        Parse();
    }

    public override void Parse()
    {
        var unk1 = ReadB();
        var unk2 = ReadB();
        var unk3 = ReadB();
        var unk4 = ReadB();

        _money = ReadI(); //current money
        _listId = ReadI();
        int size = ReadSh();

        for (int i = 0; i < size; i++)
        {
            int itemId = ReadI();
            int itemType2 = ReadSh();
            int bodyPart = ReadSh();
            int price = ReadI();

            _listProduct.Add(new Product(0, 0, 1, itemType2, 0, bodyPart, 0, price, itemId));
        }

    }

}
