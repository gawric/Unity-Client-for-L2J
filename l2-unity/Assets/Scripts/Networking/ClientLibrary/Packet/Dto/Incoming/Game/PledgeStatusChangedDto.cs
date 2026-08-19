using UnityEngine;

public class PledgeStatusChangedDto : IWireDto
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

    
    public void ReadFrom(PacketReader reader)
    {
        _leaderId = reader.ReadI();
        _clanId = reader.ReadI();
        _crestId = reader.ReadI();

        _allyId = reader.ReadI();
        _allyCrestId = reader.ReadI();
        
        int unk1 = reader.ReadI();
        int unk2 = reader.ReadI();

        Debug.Log("PledgeStatusChanged _leaderId " + _leaderId + " _clanId " + _clanId);
    }
}
