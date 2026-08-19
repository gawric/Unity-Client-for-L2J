using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PledgePowerGradeListDto : IWireDto
{
    private List<GradeList> _gradeList = new List<GradeList>();

    public List<GradeList> GradeList {get{return _gradeList;}}

    

    public void ReadFrom(PacketReader reader)
    {
       int size = reader.ReadI();

       for(int i =0; i < size; i++)
       {
            int rank = reader.ReadI();
            int power = reader.ReadI();
            _gradeList.Add(new GradeList(rank, power));
       }
    }
}

public class GradeList
{
    private int _rank;
    private int _power;

    public GradeList(int rank, int power)
    {
        _rank = rank;
        _power = power;
    }

    public int GetRank()
    {
        return _rank;
    }

    public int GetPower()
    {
        return _power;
    }
}
