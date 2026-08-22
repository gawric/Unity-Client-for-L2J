using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class PledgeShowMemberListDeleteDto : IWireDto
{
    private string _memberName;

    public string MemberName
    {
        get => _memberName;
    }

    

    public void ReadFrom(PacketReader reader)
    {
        _memberName = reader.ReadOtherS();
    }
}
