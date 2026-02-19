using UnityEngine;

public class PledgeClanInfo : ServerPacket
{
    public PledgeClanInfo(byte[] d) : base(d)
    {
    }

    public override void Parse()
    {
        throw new System.NotImplementedException();
    }
}
