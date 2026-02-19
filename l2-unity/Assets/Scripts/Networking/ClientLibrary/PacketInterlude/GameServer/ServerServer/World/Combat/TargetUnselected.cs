using UnityEngine;

public class TargetUnselected : ServerPacket
{
    private int _objectId;
    private int _x;
    private int _y;
    private int _z;

    private Vector3 _lastPosition;

    public int ObjId { get => _objectId; }
    public Vector3 LastPos { get => _lastPosition; }

    public TargetUnselected(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();

        _x = ReadI();
        _y = ReadI();
        _z = ReadI();


        _lastPosition = VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
    }
}
