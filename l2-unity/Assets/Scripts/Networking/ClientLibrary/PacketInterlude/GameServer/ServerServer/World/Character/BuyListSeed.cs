using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class BuyListSeed : ServerPacket
{
    private  int _manorId;
    private List<Product> _list = new List<Product>();
    private  int _money;

    public int ManorId { get => _manorId; }
    public List<Product> List{ get => _list;}
    public int CurrentMoney { get => _money; }

    public BuyListSeed(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _money = ReadI();
        _manorId = ReadI();
        int size = ReadSh();
        Debug.Log("");

        for (int i = 0; i < size; i++)
        {
            int unk1 = ReadSh();
            int id1 = ReadI();
            int id_1 = ReadI();
            int amount = ReadI();
            int unk4 = ReadSh();
            int unk5 = ReadSh();
            int price = ReadI();

            _list.Add(new Product(0, -1, amount, -1, 0, 0, 0, price, id1));
        }
    }
}



