using System;
using UnityEngine;

/// <summary>
/// Live HP/MP/CP/level/class update for one existing party member.
/// </summary>
public class PartySmallWindowUpdate : ServerPacket
{
    private PartyMemberSnapshot _member;

    public PartyMemberSnapshot Member => _member;

    public PartySmallWindowUpdate(byte[] data) : base(data)
    {
        Parse();
    }

    public override void Parse()
    {
        try
        {
            _member = new PartyMemberSnapshot();
            _member.ObjectId = ReadI();
            _member.Name = ReadOtherS();
            _member.CurCp = ReadI();
            _member.MaxCp = ReadI();
            _member.CurHp = ReadI();
            _member.MaxHp = ReadI();
            _member.CurMp = ReadI();
            _member.MaxMp = ReadI();
            _member.Level = ReadI();
            _member.ClassId = ReadI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PartySmallWindowUpdate] Parse error: {ex.Message}");
        }
    }
}
