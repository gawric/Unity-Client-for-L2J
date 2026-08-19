using UnityEngine;

public class PledgeReceiveMemberInfoDto : IWireDto
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

    
    public void ReadFrom(PacketReader reader)
    {
        _pledgeType = reader.ReadI();
        _name = reader.ReadOtherS();
        _title = reader.ReadOtherS();
        _powerGrade = reader.ReadI();

        _subPledgeName = reader.ReadOtherS();
        _apprenticeOrSponsorName = reader.ReadOtherS();
    }
}
