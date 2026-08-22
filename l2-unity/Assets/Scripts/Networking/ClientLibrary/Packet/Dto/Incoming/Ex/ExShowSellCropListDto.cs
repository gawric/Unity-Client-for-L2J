using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExShowSellCropListDto : IWireDto
{

    private int _manorId;
    private List<CastleCrop> _list = new List<CastleCrop>();
    public List<CastleCrop> List { get => _list; }
    public int ManorId { get => _manorId; }

    

    public void ReadFrom(PacketReader reader)
    {
        _manorId = reader.ReadI();
        int size = reader.ReadI();

        for(int i=0; i < size; i++)
        {
            int objectId = reader.ReadI();
            int item_id = reader.ReadI();
            //seed
            int seedLevel = reader.ReadI();
            int reward1 = reader.ReadB();
            int reward1_itemId = reader.ReadI();
            int reward2 = reader.ReadB();
            int reward2_itemId = reader.ReadI();


            int manorId = reader.ReadI();
            //crop
            int amount = reader.ReadI();
            int price = reader.ReadI();
            int reward_crop = reader.ReadB();


            int count = reader.ReadI();

            CastleCrop crop = new CastleCrop(objectId, item_id, ItemLocation.Trade, 0, count, ItemCategory.Item, false, ItemSlot.none, 0, 9999);
            crop.SetCrop(amount,  price, reward_crop);
            crop.SetSeed(new Seed(seedLevel, reward1, reward1_itemId, reward2, reward2_itemId));
        }
    }
}


