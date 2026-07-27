/// <summary>
/// Temporary diagnostic switch for ItemServer's raw incoming-packet-id logging - very noisy
/// (logs every single packet), only meant to be flipped on briefly to find a real opcode.
/// </summary>
public static class PartyDebugRawLog
{
    public static bool Enabled = false;
}
