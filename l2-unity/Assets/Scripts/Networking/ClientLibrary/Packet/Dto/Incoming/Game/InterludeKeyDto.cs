public class InterludeKeyDto : IWireDto
{
    public byte[] BlowFishKey { get; private set; }
    public bool AuthAllowed { get; private set; }
    public int ServerId { get; private set; }
    public bool UseBlowfish { get; private set; }

    

    public void ReadFrom(PacketReader reader)
    {
        int auth = reader.ReadB();
        AuthAllowed = auth != 0;
        BlowFishKey = reader.ReadB(8);
        UseBlowfish = reader.ReadI() != 0;
        ServerId = reader.ReadI();
    }
}
