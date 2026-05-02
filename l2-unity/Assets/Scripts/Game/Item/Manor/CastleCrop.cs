using UnityEngine;

public class CastleCrop : ItemInstance
{
    private int _level;
    private int _price;
    private int _reward_crop;
    private int _amount;
    private Seed _seed;

    public CastleCrop(int objectId, int itemId, ItemLocation location, int slot, int count, ItemCategory category, bool equipped, ItemSlot bodyPart, int enchantLevel, long remainingTime)
        : base(objectId, itemId, location, slot, count, category, equipped, bodyPart, enchantLevel, remainingTime)
    {

    }

    public void SetCrop(int amount, int price, int reward_crop)
    {
        _amount = amount;
        _price = price;
        _reward_crop = reward_crop;
    }

    public void SetSeed(Seed seed)
    {
        _seed = seed;
    }

    public int Level => _level;
    public int RewardCrop => _reward_crop;
    public int RewardIdSeed1 => _seed.Reward1ItemId;
    public int RewardIdSeed2 => _seed.Reward2ItemId;

    public int Price => _price;
    public int Amount => _amount;


}
