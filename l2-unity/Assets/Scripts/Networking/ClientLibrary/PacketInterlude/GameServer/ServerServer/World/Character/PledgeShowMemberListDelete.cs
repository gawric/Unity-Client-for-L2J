using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class PledgeShowMemberListDelete : ServerPacket
{
    private string _memberName;

    public string MemberName
    {
        get => _memberName;
    }

    public PledgeShowMemberListDelete(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _memberName = ReadOtherS();
    }
}
