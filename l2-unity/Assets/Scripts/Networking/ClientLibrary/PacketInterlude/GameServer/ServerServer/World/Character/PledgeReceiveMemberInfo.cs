using UnityEngine;

public class PledgeReceiveMemberInfo : ServerPacket
{
    private int _pledgeType;
    private string _name;
    private string _title;
    private int _powerGrade;
    private string _subPledgeName;
    private string _apprenticeOrSponsorName;
    public int PledgeType => _pledgeType;
    public string Name => _name;
    public string Title => _title;
    public int PowerGrade => _powerGrade;
    public string SubPledgeName => _subPledgeName;
    public string ApprenticeOrSponsorName => _apprenticeOrSponsorName;

    public PledgeReceiveMemberInfo(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
        _pledgeType = ReadI();
        _name = ReadOtherS();
        _title = ReadOtherS();
        _powerGrade = ReadI();

        _subPledgeName = ReadOtherS();
        _apprenticeOrSponsorName = ReadOtherS();
    }
}
