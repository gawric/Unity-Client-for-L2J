using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PledgePowerGradeList : ServerPacket
{
    private List<GradeList> _gradeList = new List<GradeList>();

    public List<GradeList> GradeList {get{return _gradeList;}}

    public PledgePowerGradeList(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
       int size = ReadI();

       for(int i =0; i < size; i++)
       {
            int rank = ReadI();
            int power = ReadI();
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
