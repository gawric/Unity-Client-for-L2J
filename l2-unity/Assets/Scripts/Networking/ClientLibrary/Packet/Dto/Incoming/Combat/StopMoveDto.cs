using UnityEngine;

public class StopMoveDto : IWireDto
{
    private int _objectId;
    private int _x;
    private int _y;
    private int _z;
    private int _heading;
    private Vector3 _stopPos;

    public int ObjId { get => _objectId; }
    public Vector3 StopPos { get => _stopPos; }
    

    public void ReadFrom(PacketReader reader)
    {
        _objectId = reader.ReadI();

        _x = reader.ReadI();
        _y = reader.ReadI();
        _z = reader.ReadI();
        _heading = reader.ReadI();

        _stopPos = VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
    }
}
