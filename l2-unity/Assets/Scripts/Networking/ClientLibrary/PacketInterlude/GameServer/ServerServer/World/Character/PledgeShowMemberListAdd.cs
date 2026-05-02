using Org.BouncyCastle.Utilities.Encoders;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PledgeShowMemberListAdd : ServerPacket
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

    public PledgeShowMemberListAdd(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _memberName = ReadOtherS();
        _lvl = ReadI();
        _classId = ReadI();
        _isOnline = ReadI();
        _pledgeType = ReadI();
        _race = ReadI();
        _sex = ReadI();
        _clanMember = new ClanMember(_memberName, _lvl, _classId, _sex, _race, _isOnline, _pledgeType);
    }
}
