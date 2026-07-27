using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full party roster snapshot - sent once when joining/forming a party (mirrors
/// org.l2jmobius.gameserver.network.serverpackets.PartySmallWindowAll#writeImpl exactly).
/// The receiving member is excluded from the member list server-side.
/// </summary>
public class PartySmallWindowAll : ServerPacket
{
    private int _leaderObjectId;
    private PartyDistributionType _distributionType;
    private readonly List<PartyMemberSnapshot> _members = new List<PartyMemberSnapshot>();

    public int LeaderObjectId => _leaderObjectId;
    public PartyDistributionType DistributionType => _distributionType;
    public List<PartyMemberSnapshot> Members => _members;

    public PartySmallWindowAll(byte[] data) : base(data)
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

            int memberCount = ReadI();
            for (int i = 0; i < memberCount; i++)
            {
                PartyMemberSnapshot member = new PartyMemberSnapshot();
                member.ObjectId = ReadI();
                member.Name = ReadOtherS();
                member.CurCp = ReadI();
                member.MaxCp = ReadI();
                member.CurHp = ReadI();
                member.MaxHp = ReadI();
                member.CurMp = ReadI();
                member.MaxMp = ReadI();
                member.Level = ReadI();
                member.ClassId = ReadI();
                ReadI(); // unused
                ReadI(); // race ordinal - not needed client-side (already known from CharInfo/spawn)
                _members.Add(member);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PartySmallWindowAll] Parse error: {ex.Message}");
        }
    }
}
