/// <summary>Live HP/MP/CP/level/class update for one existing party member.</summary>
public sealed class PartySmallWindowUpdateDto : IWireDto
{
    public PartyMemberSnapshot Member { get; private set; }

    public void ReadFrom(PacketReader reader)
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
        Member = member;
    }
}
