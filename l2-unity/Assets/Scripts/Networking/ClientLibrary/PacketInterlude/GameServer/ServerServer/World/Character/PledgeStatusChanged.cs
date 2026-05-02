using UnityEngine;

public class PledgeStatusChanged : ServerPacket
{
    private int _leaderId;

    private int _clanId;
    private int _crestId;

    private int _allyId;
    private int _allyCrestId;

   
    public int LeaderId => _leaderId;
    public int ClanId => _clanId;
    public int CrestId => _crestId;
    public int AllyId => _allyId;
    public int AllyCrestId => _allyCrestId;

    public PledgeStatusChanged(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
        _leaderId = ReadI();
        _clanId = ReadI();
        _crestId = ReadI();

        _allyId = ReadI();
        _allyCrestId = ReadI();
        
        int unk1 = ReadI();
        int unk2 = ReadI();

        Debug.Log("PledgeStatusChanged _leaderId " + _leaderId + " _clanId " + _clanId);
    }
}
