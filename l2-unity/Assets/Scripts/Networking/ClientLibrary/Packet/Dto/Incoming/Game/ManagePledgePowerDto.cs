using UnityEngine;

public class ManagePledgePowerDto : IWireDto
{
    private int _rank;
    private int _atcion;
    private int _privilegesByRank;

    public int Rank => _rank;
    public int Action => _atcion;
    public int PrivilegesByRank => _privilegesByRank;
    
    public void ReadFrom(PacketReader reader)
    {
        _rank = reader.ReadI();
        _atcion = reader.ReadI();
        _privilegesByRank = reader.ReadI();
    }
}


