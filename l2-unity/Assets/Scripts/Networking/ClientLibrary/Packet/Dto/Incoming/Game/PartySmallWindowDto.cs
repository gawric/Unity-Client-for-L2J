using System.Collections.Generic;
using System.IO;

static class PartyMemberWire
{
    public static void Require(PacketReader reader, int bytes)
    {
        if (!reader.HasRemaining(bytes))
            throw new InvalidDataException("Truncated Interlude party packet");
    }

    public static PartyMemberInfo ReadStats(PacketReader reader)
    {
        Require(reader, 6);
        var member = new PartyMemberInfo { ObjectId = reader.ReadI(), Name = reader.ReadOtherS() };
        if (member.ObjectId <= 0 || string.IsNullOrEmpty(member.Name))
            throw new InvalidDataException("Invalid party member identity");
        Require(reader, 32);
        member.Cp = reader.ReadI();
        member.MaxCp = reader.ReadI();
        member.Hp = reader.ReadI();
        member.MaxHp = reader.ReadI();
        member.Mp = reader.ReadI();
        member.MaxMp = reader.ReadI();
        member.Level = reader.ReadI();
        member.ClassId = reader.ReadI();
        return member;
    }

    public static PartyMemberInfo ReadFull(PacketReader reader)
    {
        PartyMemberInfo member = ReadStats(reader);
        Require(reader, 8);
        reader.ReadI();
        reader.ReadI();
        return member;
    }
}

public sealed class PartySmallWindowAllDto : IWireDto
{
    public int LeaderId;
    public int DistributionType;
    public readonly List<PartyMemberInfo> Members = new List<PartyMemberInfo>();

    public void ReadFrom(PacketReader reader)
    {
        PartyMemberWire.Require(reader, 12);
        LeaderId = reader.ReadI();
        DistributionType = reader.ReadI();
        int count = reader.ReadI();
        if (LeaderId <= 0 || count < 0 || count > 8)
            throw new InvalidDataException("Invalid Interlude party roster");
        var ids = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            PartyMemberInfo member = PartyMemberWire.ReadFull(reader);
            if (!ids.Add(member.ObjectId))
                throw new InvalidDataException("Duplicate party member");
            Members.Add(member);
        }
    }
}

public sealed class PartySmallWindowAddDto : IWireDto
{
    public int LeaderId;
    public int DistributionType;
    public PartyMemberInfo Member;

    public void ReadFrom(PacketReader reader)
    {
        PartyMemberWire.Require(reader, 8);
        LeaderId = reader.ReadI();
        DistributionType = reader.ReadI();
        if (LeaderId <= 0)
            throw new InvalidDataException("Invalid party leader");
        Member = PartyMemberWire.ReadFull(reader);
    }
}

public sealed class PartySmallWindowUpdateDto : IWireDto
{
    public PartyMemberInfo Member;

    public void ReadFrom(PacketReader reader)
    {
        Member = PartyMemberWire.ReadStats(reader);
    }
}

public sealed class PartySmallWindowDeleteDto : IWireDto
{
    public int ObjectId;

    public void ReadFrom(PacketReader reader)
    {
        PartyMemberWire.Require(reader, 6);
        ObjectId = reader.ReadI();
        reader.ReadOtherS();
    }
}

public sealed class PartySmallWindowDeleteAllDto : IWireDto
{
    public void ReadFrom(PacketReader reader) { }
}
