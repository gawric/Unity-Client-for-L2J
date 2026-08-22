using UnityEngine;

public class PledgeReceivePowerInfoDto : IWireDto
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

    
    public void ReadFrom(PacketReader reader)
    {
        _powerGrade = reader.ReadI();
        _name = reader.ReadOtherS();
        _powerGradeByRank = reader.ReadI();
    }
}