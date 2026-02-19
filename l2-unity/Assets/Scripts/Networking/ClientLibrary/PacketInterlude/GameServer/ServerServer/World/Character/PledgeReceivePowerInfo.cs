using UnityEngine;

public class PledgeReceivePowerInfo : ServerPacket
{

    private string _name;
    private int _powerGrade;
    private int _powerGradeByRank;

    public string Name => _name;
    public int PowerGrade
    {
        get => _powerGrade;
        set => _powerGrade = value;
    }

    public int PowerGradeByRank
    {
        get => _powerGradeByRank;
        set => _powerGradeByRank = value;
    }

    public PledgeReceivePowerInfo(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
        _powerGrade = ReadI();
        _name = ReadOtherS();
        _powerGradeByRank = ReadI();
    }
}