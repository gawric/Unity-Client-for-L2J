using UnityEngine;

public class Seed
{
    public int SeedLevel { get; set; }
    public int Rewared1 { get; set; }
    public int Reward1ItemId { get; set; }
    public int Reward2 { get; set; }
    public int Reward2ItemId { get; set; }
    public int CropId { get; set; }
    public int SeedReferencePrice { get; set; }
    public int CropReferencePrice { get; set; }

    public Seed(int seedLevel, int rewared1, int reward1ItemId, int reward2, int reward2ItemId)
    {
        SeedLevel = seedLevel;
        Rewared1 = rewared1;
        Reward1ItemId = reward1ItemId;
        Reward2 = reward2;
        Reward2ItemId = reward2ItemId;
    }

    public Seed(int cropId , int level , int seedReferencePrice , int cropReferencePrice)
    {
        CropId = cropId;
        SeedLevel = level;
        SeedReferencePrice = seedReferencePrice;
        CropReferencePrice = cropReferencePrice;
    }

    public void SetReward(int rewared1, int reward1ItemId, int reward2, int reward2ItemId)
    {
        Rewared1 = rewared1;
        Reward1ItemId = reward1ItemId;
        Reward2 = reward2;
        Reward2ItemId = reward2ItemId;
    }
}
