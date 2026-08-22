using System.Collections.Generic;

/// <summary>
/// Full party roster snapshot - sent once when joining/forming a party (mirrors
/// org.l2jmobius.gameserver.network.serverpackets.PartySmallWindowAll#writeImpl exactly).
/// The receiving member is excluded from the member list server-side.
/// </summary>
public sealed class PartySmallWindowAllDto : IWireDto
{
    public int LeaderObjectId { get; private set; }
    public PartyDistributionType DistributionType { get; private set; }
    public List<PartyMemberSnapshot> Members { get; } = new List<PartyMemberSnapshot>();

    public void ReadFrom(PacketReader reader)
    {
        LeaderObjectId = reader.ReadI();
        int distributionTypeId = reader.ReadI();
        DistributionType = PartyDistributionTypeExtensions.FindById(distributionTypeId) ?? PartyDistributionType.FindersKeepers;

        int memberCount = reader.ReadI();
        for (int i = 0; i < memberCount; i++)
        {
            PartyMemberSnapshot member = new PartyMemberSnapshot();
            member.ObjectId = reader.ReadI();
            member.Name = reader.ReadOtherS();
            member.CurCp = reader.ReadI();
            member.MaxCp = reader.ReadI();
            member.CurHp = reader.ReadI();
            member.MaxHp = reader.ReadI();
            member.CurMp = reader.ReadI();
            member.MaxMp = reader.ReadI();
            member.Level = reader.ReadI();
            member.ClassId = reader.ReadI();
            reader.ReadI(); // unused
            reader.ReadI(); // race ordinal - not needed client-side (already known from CharInfo/spawn)
            Members.Add(member);
        }
    }
}
