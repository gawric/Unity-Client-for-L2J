using System.Collections.Generic;

/// <summary>
/// Buffs/debuffs currently active on one party member (or party pet/servitor). Note the skill
/// level is written as a short (writeShort), unlike most other int fields in this packet family.
/// </summary>
public sealed class PartySpelledDto : IWireDto
{
    /// <summary>0 = player, 1 = pet, 2 = servitor.</summary>
    public int CreatureType { get; private set; }
    public int ObjectId { get; private set; }
    public List<PartyBuffInfo> Effects { get; } = new List<PartyBuffInfo>();

    public void ReadFrom(PacketReader reader)
    {
        CreatureType = reader.ReadI();
        ObjectId = reader.ReadI();
        int count = reader.ReadI();
        for (int i = 0; i < count; i++)
        {
            int skillId = reader.ReadI();
            int skillLevel = reader.ReadSh();
            int time = reader.ReadI();
            Effects.Add(new PartyBuffInfo(skillId, skillLevel, time));
        }
    }
}
