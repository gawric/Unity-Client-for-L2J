using UnityEngine;

public class PledgeShowMemberListDeleteAll : ServerPacket
{

    public PledgeShowMemberListDeleteAll(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
       
    }
}
