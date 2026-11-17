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

    public static PledgeShowMemberListAddDto From(
        string name, int level, int classId, int online, int pledgeType, int race, int sex)
    {
        return new PledgeShowMemberListAddDto
        {
            _memberName = name,
            _lvl = level,
            _classId = classId,
            _isOnline = online,
            _pledgeType = pledgeType,
            _race = race,
            _sex = sex,
            _clanMember = new ClanMember(name, level, classId, sex, race, online, pledgeType),
        };
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
