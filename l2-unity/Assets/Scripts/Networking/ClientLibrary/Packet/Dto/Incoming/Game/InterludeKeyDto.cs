public class InterludeKeyDto : IWireDto
{
    public byte[] BlowFishKey { get; private set; }
    public bool AuthAllowed { get; private set; }
    public int ServerId { get; private set; }
    public bool UseBlowfish { get; private set; }

    /// <summary>Adaptador para la rama Lucera: construye el mismo modelo que espera el handler.</summary>
    public static InterludeKeyDto From(bool authAllowed, byte[] key8, bool useBlowfish, int serverId)
    {
        return new InterludeKeyDto
        {
            AuthAllowed = authAllowed,
            BlowFishKey = key8,
            UseBlowfish = useBlowfish,
            ServerId = serverId,
        };
    }

    public void ReadFrom(PacketReader reader)
    {
        int auth = reader.ReadB();
        AuthAllowed = auth != 0;
        BlowFishKey = reader.ReadB(8);
        UseBlowfish = reader.ReadI() != 0;
        ServerId = reader.ReadI();
    }
}
