using JetBrains.Annotations;
using UnityEngine;


public class SeedProduction
{
    private int _seedId;
    private int _price;
    private int _startAmount;
    private int _amount;
    private Seed _seed;
    public SeedProduction(int id, int amount, int price, int startAmount)
    {
        _seedId = id;
        _amount = amount;
        _price = price;
        _startAmount = startAmount;
    }

    public void AddSeed(Seed seed)
    {
        _seed = seed;
    }

    public int GetSeedId() { return _seedId; }
    public int GetPrice() { return _price; }

    public int GetStartAmount() { return _startAmount; }

    public int GetAmount() { return _amount; }

    public Seed GetSeed() { return _seed; }

}


