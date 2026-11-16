using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class LocalPartyChecks
{
    public static void Run()
    {
        byte[] all = Roster(new[] { 201, 202 });
        var roster = new PartySmallWindowAllDto();
        var reader = new PacketReader(all);
        roster.ReadFrom(reader);
        Check(roster.LeaderId == 101 && roster.DistributionType == 0 && roster.Members.Count == 2, "Party roster header");
        Check(roster.Members[1].ObjectId == 202 && roster.Members[1].Name == "Маг202" && reader.Remaining == 0, "Interlude row tails must not shift the next member");
        Check(roster.Members[0].Hp == 73 && roster.Members[0].MaxHp == 98 && roster.Members[0].Mp == 32 && roster.Members[0].ClassId == 10, "Party HP/MP and class layout");

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0x4F);
            writer.Write(101);
            writer.Write(0);
            WriteMember(writer, 203);
            writer.Write(0);
            writer.Write(0);
            var add = new PartySmallWindowAddDto();
            reader = new PacketReader(stream.ToArray());
            add.ReadFrom(reader);
            Check(add.Member.ObjectId == 203 && reader.Remaining == 0, "Party add row layout");
        }
        PartyMemberInfo update;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0x52);
            WriteMember(writer, 201);
            var packet = new PartySmallWindowUpdateDto();
            reader = new PacketReader(stream.ToArray());
            packet.ReadFrom(reader);
            update = packet.Member;
            Check(update.ObjectId == 201 && update.Mp == 32 && reader.Remaining == 0, "Update has no trailing race fields");
        }
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0x51);
            writer.Write(201);
            WriteString(writer, "Маг201");
            var packet = new PartySmallWindowDeleteDto();
            reader = new PacketReader(stream.ToArray());
            packet.ReadFrom(reader);
            Check(packet.ObjectId == 201 && reader.Remaining == 0, "Party delete layout");
        }

        var state = new PlayerParty();
        int changes = 0;
        state.Changed += () => changes++;
        state.Replace(101, 0, roster.Members);
        Check(state.IsInParty && state.Members.Count == 2, "Roster creates group state");
        state.Update(update);
        state.Remove(202);
        Check(state.Members.Count == 1 && changes == 3, "Roster updates and removal");
        state.Clear();
        state.Update(update);
        Check(!state.IsInParty && state.Members.Count == 0 && changes == 4, "Late updates must not recreate a departed party");
        state.Add(301, 2, update);
        state.Remove(update.ObjectId);
        Check(!state.IsInParty && state.LeaderId == 0, "Removing the last peer must clear the leader");
        state.Add(301, 2, update);
        state.Replace(401, 1, new List<PartyMemberInfo>());
        Check(state.LeaderId == 0 && state.DistributionType == 0 && state.Members.Count == 0, "Empty roster must clear the old session");

        Reject(RosterHeader(-1));
        Reject(RosterHeader(9));
        Reject(new byte[] { 0x4E, 1, 0 });
        Reject(Roster(new[] { 201, 201 }));
        byte[] truncated = all;
        Array.Resize(ref truncated, truncated.Length - 5);
        Reject(truncated);
        var maximum = new PartySmallWindowAllDto();
        maximum.ReadFrom(new PacketReader(Roster(new[] { 201, 202, 203, 204, 205, 206, 207, 208 })));
        Check(maximum.Members.Count == 8, "Maximum Interlude roster excludes self");

        var registry = new OutgoingPacketAutoRegistry();
        var invite = registry.Create(new RequestJoinPartyCommand("PartyMember", 0));
        Check(invite != null && invite.GetPacketType() == 0x29, "Invitation must be registered with opcode 0x29");
        reader = new PacketReader(invite.GetData());
        Check(reader.ReadOtherS() == "PartyMember" && reader.ReadI() == 0, "Invite name and loot format");
        var leave = registry.Create(new RequestWithDrawalPartyCommand());
        Check(leave != null && leave.GetData()[0] == 0x2B, "Leave opcode must be 0x2B");
        Debug.Log("[LocalPartyChecks] Party layouts, limits, state transitions and outgoing registry passed");
    }

    static byte[] RosterHeader(int count)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0x4E);
            writer.Write(101);
            writer.Write(0);
            writer.Write(count);
            return stream.ToArray();
        }
    }

    static byte[] Roster(int[] ids)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(RosterHeader(ids.Length));
            foreach (int id in ids)
            {
                WriteMember(writer, id);
                writer.Write(0);
                writer.Write(1);
            }
            return stream.ToArray();
        }
    }

    static void WriteMember(BinaryWriter writer, int id)
    {
        writer.Write(id);
        WriteString(writer, "Маг" + id);
        foreach (int value in new[] { 31, 49, 73, 98, 32, 102, 4, 10 })
            writer.Write(value);
    }

    static void WriteString(BinaryWriter writer, string text)
    {
        writer.Write(Encoding.Unicode.GetBytes(text + "\0"));
    }

    static void Reject(byte[] payload)
    {
        try
        {
            new PartySmallWindowAllDto().ReadFrom(new PacketReader(payload));
        }
        catch (InvalidDataException)
        {
            return;
        }
        throw new InvalidOperationException("Malformed roster was accepted");
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
