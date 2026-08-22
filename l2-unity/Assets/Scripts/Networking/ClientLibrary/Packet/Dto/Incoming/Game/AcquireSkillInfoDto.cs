using System.Collections.Generic;
using UnityEngine;

public class AcquireSkillInfoDto : IWireDto
{
    private List<RequiredSkillInfo> _requiredSkillInfo;
    public List<RequiredSkillInfo> RequiredSkillInfo { get => _requiredSkillInfo; }

    public int GetId() { return _id; }
    public int GetLevel() { return _level; }
    public int GetSpCoast() { return _spCost; }

    public int GetMode() { return _mode; }

    private int _id;
    private int _level;
    private int _spCost;
    private int _mode;

    public AcquireSkillInfoDto()
    {
        _requiredSkillInfo = new List<RequiredSkillInfo>();
    }

    public void ReadFrom(PacketReader reader)
    {
        _id = reader.ReadI();
        _level = reader.ReadI();
        _spCost = reader.ReadI();
        _mode = reader.ReadI();
        int size = reader.ReadI();

        for(int i = 0; i < size; i++)
        {
            int type = reader.ReadI();
            int itemId = reader.ReadI();
            int count = reader.ReadI();
            int unk1 = reader.ReadI();
            _requiredSkillInfo.Add(new RequiredSkillInfo(type, itemId, count, unk1));
        }
    }

    
}


public class RequiredSkillInfo
{

    int _type;
    int _itemId;
    int _count;
    int _unk;
    public RequiredSkillInfo(int type , int itemId , int count , int unk1)
    {
        _type = type;
        _itemId = itemId;
        _count = count;
        _unk = unk1;
    }

    public int GetItemId()
    {
        return _itemId;
    }

    public int GetCount()
    {
        return _count;
    }

    public int GetType()
    {
        return _type;
    }
}