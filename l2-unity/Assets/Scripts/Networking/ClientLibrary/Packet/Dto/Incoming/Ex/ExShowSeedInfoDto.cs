using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

public class ExShowSeedInfoDto : IWireDto
{
    private  List<SeedProduction> _listSeedProduction;
    private bool _hideButtons;
    private int _manorId;
    public int ManorId { get => _manorId; }
    public List<SeedProduction> List { get => _listSeedProduction; }
    public ExShowSeedInfoDto()
    {
        _listSeedProduction = new List<SeedProduction>();
    }

    public void ReadFrom(PacketReader reader)
    {
        _hideButtons = reader.ReadB() == 1;
        _manorId = reader.ReadI();
        var unk1 = reader.ReadI();
        int size = reader.ReadI();

        for(int i =0; i < size; i++)
        {
            //production
            var seedId = reader.ReadI();
            var amount = reader.ReadI();
            var start_amount = reader.ReadI();
            var sell_price = reader.ReadI();

            //seed
            var seed_level = reader.ReadI();
            var reward_1 = reader.ReadB();
            var reward_1_itemId = reader.ReadI();
            var reward_2 = reader.ReadB();
            var reward_2_itemId = reader.ReadI();

            var seedProduct = new SeedProduction(seedId, amount, sell_price, start_amount);
            seedProduct.AddSeed(new Seed(seed_level, reward_1, reward_1_itemId, reward_2, reward_2_itemId));
            _listSeedProduction.Add(seedProduct);
        }

    }
}

