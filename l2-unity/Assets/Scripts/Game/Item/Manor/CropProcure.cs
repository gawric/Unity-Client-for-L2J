using UnityEngine;

public class CropProcure : SeedProduction
{
    private int _reward;
    public CropProcure(int id, int amount, int price, int startAmount , int reward) 
        : base(id, amount, price, startAmount)
    {
        _reward = reward;
    }

    public int GetReward()
    {
        return _reward;
    }
}
