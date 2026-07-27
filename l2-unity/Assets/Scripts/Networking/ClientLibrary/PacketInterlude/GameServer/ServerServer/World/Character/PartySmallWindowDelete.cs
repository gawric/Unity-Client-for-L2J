using System;
using UnityEngine;

/// <summary>
/// A single member left/was kicked from the party (the party itself still exists).
/// </summary>
public class PartySmallWindowDelete : ServerPacket
{
    private int _objectId;
    private string _name;

    public int ObjectId => _objectId;
    public string Name => _name;

    public PartySmallWindowDelete(byte[] data) : base(data)
    {
        Parse();
    }

    public override void Parse()
    {
        try
        {
            _objectId = ReadI();
            _name = ReadOtherS();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PartySmallWindowDelete] Parse error: {ex.Message}");
        }
    }
}
