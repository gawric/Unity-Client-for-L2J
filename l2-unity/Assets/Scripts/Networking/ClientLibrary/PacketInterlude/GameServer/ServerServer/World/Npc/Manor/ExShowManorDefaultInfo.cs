using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class ExShowManorDefaultInfo : ServerPacket
{
    private bool _hideButtons;
    private List<Seed> _list = new List<Seed>();
    public List<Seed> List { get => _list; }
    public ExShowManorDefaultInfo(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _hideButtons = ReadB() == 1;
        var size = ReadI();

        for (int i = 0; i < size; i++)
        {
            int cropId = ReadI();
            int level = ReadI();
            int seedPrice = ReadI();
            int cropPrice = ReadI();
            int reward1 = ReadB();
            int rewardItemId1 = ReadI();
            int reward2 = ReadB();
            int rewardItemId2 = ReadI();
            Seed seed = new Seed(cropId, level, seedPrice, cropPrice);
            seed.SetReward(reward1, rewardItemId1, reward2, rewardItemId2);
            _list.Add(seed);    
        }
    }

}
