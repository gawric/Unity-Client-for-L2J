using System.Collections.Generic;
using UnityEngine;

public class PledgeInfoDto : IWireDto
{
    private int _clanId;
    private string _clanName;
    private string _allyName;

    public int ClanId => _clanId;
    public string ClanName => _clanName;
    public string AllyName => _allyName;
    
    public void ReadFrom(PacketReader reader)
    {
        _clanId = reader.ReadI();
        _clanName = reader.ReadOtherS();
        _allyName = reader.ReadOtherS();
    }
}
