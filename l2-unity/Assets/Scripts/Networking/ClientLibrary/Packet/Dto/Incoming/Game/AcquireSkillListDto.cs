using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AcquireSkillListDto : IWireDto
{
    private List<OtherModel> _acquireList;
    public List<OtherModel> AcquireList { get => _acquireList; }
    public AcquireSkillListDto()
    {
        _acquireList = new List<OtherModel>();
    }


    public void ReadFrom(PacketReader reader)
    {
        int skillType = reader.ReadI();
        int size = reader.ReadI();

        switch (skillType)
        {
            case (int)AcquireSkillType.USUAL:
                ParceUsual(reader, size, _acquireList, skillType);
                break;
            case (int)AcquireSkillType.FISHING:
                ParceFishing(reader, size, _acquireList, skillType);
                break;
            case (int)AcquireSkillType.CLAN:
                ParceClan(reader, size, _acquireList, skillType);
                break;
        }
    }
    //writeD(gsn.getId());
    //writeD(gsn.getValue());
    //writeD(gsn.getValue());
    //writeD(gsn.getCorrectedCost());
    //writeD(0);
    private void ParceUsual(PacketReader reader, int size, List<OtherModel> _acquireList, int type)
    {
        for(int i = 0; i < size; i++)
        {
            int id = reader.ReadI();
            int value1=  reader.ReadI();
            int value2 = reader.ReadI();
            int correctCost =  reader.ReadI();
            int unk1 = reader.ReadI();
            _acquireList.Add(new OtherModel(new AcquireData(id, value1, value2, correctCost , type)));
        }
    }

    private void ParceFishing(PacketReader reader, int size, List<OtherModel> _acquireList, int type)
    {
        for (int i = 0; i < size; i++)
        {
            int id = reader.ReadI();
            int value1 = reader.ReadI();
            int value2 = reader.ReadI();
            int unk1 = reader.ReadI();
            int unk2 = reader.ReadI();
            _acquireList.Add(new OtherModel(new AcquireData(id, value1, value2, unk1, type)));
        }
    }


    private void ParceClan(PacketReader reader, int size, List<OtherModel> _acquireList, int type)
    {
        for (int i = 0; i < size; i++)
        {
            int id = reader.ReadI();
            int value1 = reader.ReadI();
            int value2 = reader.ReadI();
            int cost = reader.ReadI();
            int unk2 = reader.ReadI();
            //OtherModel otherModel = new OtherModel(new AcquireData(id, value1, value2, cost));
            _acquireList.Add(new OtherModel(new AcquireData(id, value1, value2, cost, type)));
        }
    }
}

public class AcquireData
{
    private int _id;
    private int _value1;
    private int _value2;
    private int _correctCost;
    private int _type;

    public AcquireData(int id , int value1 , int value2 , int correctCost , int type)
    {
        _id = id;
        _value1 = value1;
        _value2 = value2;
        _correctCost = correctCost;
        _type = type;

    }

    public int GetId()
    {
        return _id;
    }

    public int GetCost()
    {
        return _correctCost;
    }

    public int GetValue1()
    {
        return _value1;
    }

    public int GetAcqType()
    {
        return _type;
    }

}
