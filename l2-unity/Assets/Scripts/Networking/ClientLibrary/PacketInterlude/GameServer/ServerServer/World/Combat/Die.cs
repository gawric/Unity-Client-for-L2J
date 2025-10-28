public class Die : ServerPacket
{
    private int _objectId;
    public bool _canTeleport;
    public bool _sweepable;
    public bool _allowFixedRes;
    public int ObjectId { get =>_objectId; }

    public Die(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();
        _canTeleport = ReadI() == 1;

        int hideoutId = ReadI(); // 6d 01 00 00 00 - to hide away
        int castleId = ReadI(); // 6d 02 00 00 00 - to castle
        int siegeHQ = ReadI(); // 6d 05 00 00 00 - to siege HQ

        _sweepable = ReadI() == 1; // sweepable (blue glow)
        _allowFixedRes = ReadI() == 1; // fixed
    }
}
