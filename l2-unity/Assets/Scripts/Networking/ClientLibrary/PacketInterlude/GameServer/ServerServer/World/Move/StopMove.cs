using UnityEngine;

public class StopMove : ServerPacket
{
    private int _objectId;
    private int _x;
    private int _y;
    private int _z;
    private int _heading;
    private Vector3 _stopPos;

    public int ObjId { get => _objectId; }
    public Vector3 StopPos { get => _stopPos; }
    public StopMove(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();

        _x = ReadI();
        _y = ReadI();
        _z = ReadI();
        _heading = ReadI();

        _stopPos = VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
    }
}
