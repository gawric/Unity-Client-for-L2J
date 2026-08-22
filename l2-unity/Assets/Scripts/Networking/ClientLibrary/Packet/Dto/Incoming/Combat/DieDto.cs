public class DieDto : IWireDto
{
    private int _objectId;
    public bool _canTeleport;
    public bool _sweepable;
    public bool _allowFixedRes;
    public int ObjectId { get =>_objectId; }

    

    public void ReadFrom(PacketReader reader)
    {
        _objectId = reader.ReadI();
        _canTeleport = reader.ReadI() == 1;

        int hideoutId = reader.ReadI(); // 6d 01 00 00 00 - to hide away
        int castleId = reader.ReadI(); // 6d 02 00 00 00 - to castle
        int siegeHQ = reader.ReadI(); // 6d 05 00 00 00 - to siege HQ

        _sweepable = reader.ReadI() == 1; // sweepable (blue glow)
        _allowFixedRes = reader.ReadI() == 1; // fixed
    }
}
