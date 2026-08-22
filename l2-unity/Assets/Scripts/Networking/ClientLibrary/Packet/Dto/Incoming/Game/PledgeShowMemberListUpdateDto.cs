using UnityEngine;

public class PledgeShowMemberListUpdateDto : IWireDto
{
    private string _name;
    private int _level;
    private int _classId;
    private int _sex;
    private int _race;
    private int _isOnline;
    private int _pledgeType;
    private int _hasSponsor;

    public string MemberName => _name;
    public int Level => _level;
    public int ClassId => _classId;
    public int Sex => _sex;
    public int Race => _race;
    public int IsOnline => _isOnline;
    public int PledgeType => _pledgeType;
    public int HasSponsor => _hasSponsor;

    
    public void ReadFrom(PacketReader reader)
    {
        _name = reader.ReadOtherS();
        _level = reader.ReadI();
        _classId = reader.ReadI();
        _sex = reader.ReadI();
        _race = reader.ReadI();
        _isOnline = reader.ReadI();
        _pledgeType = reader.ReadI();
        _hasSponsor = reader.ReadI();

    }


}
