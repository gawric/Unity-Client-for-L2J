using UnityEngine;

public class ManagePledgePower : ServerPacket
{
    private int _rank;
    private int _atcion;
    private int _privilegesByRank;

    public int Rank => _rank;
    public int Action => _atcion;
    public int PrivilegesByRank => _privilegesByRank;
    public ManagePledgePower(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
        _rank = ReadI();
        _atcion = ReadI();
        _privilegesByRank = ReadI();
    }
}


