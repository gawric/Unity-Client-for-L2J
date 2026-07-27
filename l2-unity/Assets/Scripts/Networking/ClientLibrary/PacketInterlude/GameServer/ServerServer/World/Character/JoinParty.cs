using System;
using UnityEngine;

/// <summary>
/// Sent back to the inviter once the invited player answers RequestAnswerJoinParty -
/// tells the requestor whether their invite was accepted (1) or declined (0).
/// </summary>
public class JoinParty : ServerPacket
{
    private int _response;

    public int Response => _response;
    public bool Accepted => _response == 1;

    public JoinParty(byte[] data) : base(data)
    {
        Parse();
    }

    public override void Parse()
    {
        try
        {
            _response = ReadI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JoinParty] Parse error: {ex.Message}");
        }
    }
}
