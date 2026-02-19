using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExShowSellCropList : ServerPacket
{

    private int _manorId;
    private List<CastleCrop> _list = new List<CastleCrop>();
    public List<CastleCrop> List { get => _list; }
    public int ManorId { get => _manorId; }

    public ExShowSellCropList(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _manorId = ReadI();
        int size = ReadI();

        for(int i=0; i < size; i++)
        {
            int objectId = ReadI();
            int item_id = ReadI();
            //seed
            int seedLevel = ReadI();
            int reward1 = ReadB();
            int reward1_itemId = ReadI();
            int reward2 = ReadB();
            int reward2_itemId = ReadI();


            int manorId = ReadI();
            //crop
            int amount = ReadI();
            int price = ReadI();
            int reward_crop = ReadB();


            int count = ReadI();

            CastleCrop crop = new CastleCrop(objectId, item_id, ItemLocation.Trade, 0, count, ItemCategory.Item, false, ItemSlot.none, 0, 9999);
            crop.SetCrop(amount,  price, reward_crop);
            crop.SetSeed(new Seed(seedLevel, reward1, reward1_itemId, reward2, reward2_itemId));
        }
    }
}


