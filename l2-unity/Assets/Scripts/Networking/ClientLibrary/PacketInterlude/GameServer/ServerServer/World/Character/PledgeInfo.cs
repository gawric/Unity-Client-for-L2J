using System.Collections.Generic;
using UnityEngine;

public class PledgeInfo : ServerPacket
{
    private int _clanId;
    private string _clanName;
    private string _allyName;

    public int ClanId => _clanId;
    public string ClanName => _clanName;
    public string AllyName => _allyName;
    public PledgeInfo(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
        _clanId = ReadI();
        _clanName = ReadOtherS();
        _allyName = ReadOtherS();
    }
}
