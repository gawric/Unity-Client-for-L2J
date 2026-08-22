using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class BuyListSeedDto : IWireDto
{
    private  int _manorId;
    private List<Product> _list = new List<Product>();
    private  int _money;

    public int ManorId { get => _manorId; }
    public List<Product> List{ get => _list;}
    public int CurrentMoney { get => _money; }

    

    public void ReadFrom(PacketReader reader)
    {
        _money = reader.ReadI();
        _manorId = reader.ReadI();
        int size = reader.ReadSh();
        Debug.Log("");

        for (int i = 0; i < size; i++)
        {
            int unk1 = reader.ReadSh();
            int id1 = reader.ReadI();
            int id_1 = reader.ReadI();
            int amount = reader.ReadI();
            int unk4 = reader.ReadSh();
            int unk5 = reader.ReadSh();
            int price = reader.ReadI();

            _list.Add(new Product(0, -1, amount, -1, 0, 0, 0, price, id1));
        }
    }
}



