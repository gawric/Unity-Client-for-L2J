/// <summary>
/// Party was disbanded, or the local player left/was removed - no payload beyond the opcode.
/// </summary>
public class PartySmallWindowDeleteAll : ServerPacket
{
    public PartySmallWindowDeleteAll(byte[] data) : base(data)
    {
        Parse();
    }

    public override void Parse()
    {
    }
}
