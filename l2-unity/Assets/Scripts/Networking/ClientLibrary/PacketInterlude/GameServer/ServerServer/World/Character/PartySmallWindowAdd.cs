using System;
using UnityEngine;

/// <summary>
/// Sent to already-existing party members when a new member joins.
/// </summary>
public class PartySmallWindowAdd : ServerPacket
{
    private int _leaderObjectId;
    private PartyDistributionType _distributionType;
    private PartyMemberSnapshot _member;

    public int LeaderObjectId => _leaderObjectId;
    public PartyDistributionType DistributionType => _distributionType;
    public PartyMemberSnapshot Member => _member;

    public PartySmallWindowAdd(byte[] data) : base(data)
    {
        Parse();
    }

    public override void Parse()
    {
        try
        {
            _leaderObjectId = ReadI();
            int distributionTypeId = ReadI();
            _distributionType = PartyDistributionTypeExtensions.FindById(distributionTypeId) ?? PartyDistributionType.FindersKeepers;

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
            ReadI(); // unused
            ReadI(); // unused
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PartySmallWindowAdd] Parse error: {ex.Message}");
        }
    }
}
