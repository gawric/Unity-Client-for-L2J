using System.Collections.Generic;
using UnityEngine;

public class SellList : ServerPacket
{
    private int _money;
    private int _listId;
    public int _size;
    public int CurrentMoney { get => _money; }
    private List<Product> _listProduct;
    public List<Product> Products { get => _listProduct; }
    public int ListID { get => _listId; }

    public SellList(byte[] d) : base(d)
    {
        _listProduct = new List<Product>();
        Parse();
    }

    public override void Parse()
    {
        _money = ReadI();
        _listId = ReadI();
        _size = ReadSh();

        for (int i = 0; i < _size; i++)
        {
            int itemType1 = ReadSh();
            int objId = ReadI();
            int itemId = ReadI();
            int count = ReadI();

            int itemType2 = ReadSh();
            /** Custom item types (used loto, race tickets) */
            int isEquip = ReadSh();

            int bodyPart = ReadI();
            int enchant = ReadSh();
            int unknow1 = ReadSh();
            int unknow2 = ReadSh();

            int price = ReadI();

            _listProduct.Add(new Product(itemType1, objId, count, itemType2, isEquip, bodyPart, enchant, price, itemId));
        }
        Debug.Log("");

    }


}
