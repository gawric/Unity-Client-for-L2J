using UnityEngine;

public class ReplaceGearData
{
    private ItemInstance _source;
    private ItemInstance _gear;
    public ReplaceGearData(ItemInstance source , ItemInstance gear)
    {
            _source = source;
            _gear = gear;
    }

    public ItemInstance GetSource()
    {
        return _source;
    }

    public ItemInstance GetGear()
    {
        return _gear;
    }
}
