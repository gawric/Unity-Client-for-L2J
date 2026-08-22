/// <summary>Sent to already-existing party members when a new member joins.</summary>
public sealed class PartySmallWindowAddDto : IWireDto
{
    public int LeaderObjectId { get; private set; }
    public PartyDistributionType DistributionType { get; private set; }
    public PartyMemberSnapshot Member { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        LeaderObjectId = reader.ReadI();
        int distributionTypeId = reader.ReadI();
        DistributionType = PartyDistributionTypeExtensions.FindById(distributionTypeId) ?? PartyDistributionType.FindersKeepers;

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
        reader.ReadI(); // unused
        Member = member;
    }
}
