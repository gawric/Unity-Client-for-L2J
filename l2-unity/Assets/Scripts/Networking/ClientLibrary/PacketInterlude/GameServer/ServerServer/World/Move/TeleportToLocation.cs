using UnityEngine;

public class TeleportToLocation : ServerPacket
{
    private int _targetObjId;
    private int _x;
    private int _y;
    private int _z;
    private int _heading;
    private Vector3 _telePos;

    public int TarObjId { get => _targetObjId; }
    public Vector3 TeleportPos { get => _telePos; }
    public TeleportToLocation(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _targetObjId = ReadI();

        _x = ReadI();
        _y = ReadI();
        _z = ReadI();
        int point = ReadI(); // Fade 0, Instant 1.
        _heading = ReadI();

        _telePos = VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
    }
}
