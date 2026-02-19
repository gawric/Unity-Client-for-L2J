using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

public class ExShowSeedInfo : ServerPacket
{
    private  List<SeedProduction> _listSeedProduction;
    private bool _hideButtons;
    private int _manorId;
    public int ManorId { get => _manorId; }
    public List<SeedProduction> List { get => _listSeedProduction; }
    public ExShowSeedInfo(byte[] d) : base(d)
    {
        _listSeedProduction = new List<SeedProduction>();
        Parse();
    }

    public override void Parse()
    {
        _hideButtons = ReadB() == 1;
        _manorId = ReadI();
        var unk1 = ReadI();
        int size = ReadI();

        for(int i =0; i < size; i++)
        {
            //production
            var seedId = ReadI();
            var amount = ReadI();
            var start_amount = ReadI();
            var sell_price = ReadI();

            //seed
            var seed_level = ReadI();
            var reward_1 = ReadB();
            var reward_1_itemId = ReadI();
            var reward_2 = ReadB();
            var reward_2_itemId = ReadI();

            var seedProduct = new SeedProduction(seedId, amount, sell_price, start_amount);
            seedProduct.AddSeed(new Seed(seed_level, reward_1, reward_1_itemId, reward_2, reward_2_itemId));
            _listSeedProduction.Add(seedProduct);
        }

    }
}

