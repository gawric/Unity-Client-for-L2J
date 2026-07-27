using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buffs/debuffs currently active on one party member (or party pet/servitor). Mirrors
/// org.l2jmobius.gameserver.network.serverpackets.PartySpelled#writeImpl - note the skill level
/// is written as a short (writeShort), unlike most other int fields in this packet family.
/// </summary>
public class PartySpelled : ServerPacket
{
    private int _creatureType; // 0 = player, 1 = pet, 2 = servitor
    private int _objectId;
    private readonly List<PartyBuffInfo> _effects = new List<PartyBuffInfo>();

    public int CreatureType => _creatureType;
    public int ObjectId => _objectId;
    public List<PartyBuffInfo> Effects => _effects;

    public PartySpelled(byte[] data) : base(data)
    {
        Parse();
    }

    public override void Parse()
    {
        try
        {
            _creatureType = ReadI();
            _objectId = ReadI();
            int count = ReadI();
            for (int i = 0; i < count; i++)
            {
                int skillId = ReadI();
                int skillLevel = ReadSh();
                int time = ReadI();
                _effects.Add(new PartyBuffInfo(skillId, skillLevel, time));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PartySpelled] Parse error: {ex.Message}");
        }
    }
}
