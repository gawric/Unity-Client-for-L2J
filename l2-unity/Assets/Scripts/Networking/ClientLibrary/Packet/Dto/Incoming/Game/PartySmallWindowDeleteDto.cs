/// <summary>A single member left/was kicked from the party (the party itself still exists).</summary>
public sealed class PartySmallWindowDeleteDto : IWireDto
{
    public int ObjectId { get; private set; }
    public string Name { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        ObjectId = reader.ReadI();
        Name = reader.ReadOtherS();
    }
}
