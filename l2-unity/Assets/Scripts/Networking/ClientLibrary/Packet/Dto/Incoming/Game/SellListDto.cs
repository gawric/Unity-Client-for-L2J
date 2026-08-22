using System.Collections.Generic;
using UnityEngine;

public class SellListDto : IWireDto
{
    private int _money;
    private int _listId;
    public int _size;
    public int CurrentMoney { get => _money; }
    private List<Product> _listProduct;
    public List<Product> Products { get => _listProduct; }
    public int ListID { get => _listId; }

    public SellListDto()
    {
        _listProduct = new List<Product>();
    }

    public void ReadFrom(PacketReader reader)
    {
        _money = reader.ReadI();
        _listId = reader.ReadI();
        _size = reader.ReadSh();

        for (int i = 0; i < _size; i++)
        {
            int itemType1 = reader.ReadSh();
            int objId = reader.ReadI();
            int itemId = reader.ReadI();
            int count = reader.ReadI();

            int itemType2 = reader.ReadSh();
            /** Custom item types (used loto, race tickets) */
            int isEquip = reader.ReadSh();

            int bodyPart = reader.ReadI();
            int enchant = reader.ReadSh();
            int unknow1 = reader.ReadSh();
            int unknow2 = reader.ReadSh();

            int price = reader.ReadI();

            _listProduct.Add(new Product(itemType1, objId, count, itemType2, isEquip, bodyPart, enchant, price, itemId));
        }
        Debug.Log("");

    }


}
