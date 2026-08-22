/// <summary>Party was disbanded, or the local player left/was removed - no payload beyond the opcode.</summary>
public sealed class PartySmallWindowDeleteAllDto : IWireDto
{
    public void ReadFrom(PacketReader reader)
    {
    }
}
