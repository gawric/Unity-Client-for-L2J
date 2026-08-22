using Org.BouncyCastle.Utilities.Encoders;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PledgeShowMemberListAddDto : IWireDto
{
    private string _memberName;
    private int _lvl;
    private int _classId;
    private int _isOnline;
    private int _pledgeType;
    private int _race;
    private int _sex;
    private ClanMember _clanMember;

    public ClanMember ClanMember
    {
        get => _clanMember;
    }

    

    public void ReadFrom(PacketReader reader)
    {
        _memberName = reader.ReadOtherS();
        _lvl = reader.ReadI();
        _classId = reader.ReadI();
        _isOnline = reader.ReadI();
        _pledgeType = reader.ReadI();
        _race = reader.ReadI();
        _sex = reader.ReadI();
        _clanMember = new ClanMember(_memberName, _lvl, _classId, _sex, _race, _isOnline, _pledgeType);
    }
}
