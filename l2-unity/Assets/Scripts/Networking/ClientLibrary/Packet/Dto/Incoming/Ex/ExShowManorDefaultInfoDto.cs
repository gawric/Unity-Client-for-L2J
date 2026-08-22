using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class ExShowManorDefaultInfoDto : IWireDto
{
    private bool _hideButtons;
    private List<Seed> _list = new List<Seed>();
    public List<Seed> List { get => _list; }
    

    public void ReadFrom(PacketReader reader)
    {
        _hideButtons = reader.ReadB() == 1;
        var size = reader.ReadI();

        for (int i = 0; i < size; i++)
        {
            int cropId = reader.ReadI();
            int level = reader.ReadI();
            int seedPrice = reader.ReadI();
            int cropPrice = reader.ReadI();
            int reward1 = reader.ReadB();
            int rewardItemId1 = reader.ReadI();
            int reward2 = reader.ReadB();
            int rewardItemId2 = reader.ReadI();
            Seed seed = new Seed(cropId, level, seedPrice, cropPrice);
            seed.SetReward(reward1, rewardItemId1, reward2, rewardItemId2);
            _list.Add(seed);    
        }
    }

}
