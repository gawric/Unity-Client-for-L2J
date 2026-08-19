using UnityEngine;

public class TeleportToLocationDto : IWireDto
{
    private int _targetObjId;
    private int _x;
    private int _y;
    private int _z;
    private int _heading;
    private Vector3 _telePos;

    public int TarObjId { get => _targetObjId; }
    public Vector3 TeleportPos { get => _telePos; }
    

    public void ReadFrom(PacketReader reader)
    {
        _targetObjId = reader.ReadI();

        _x = reader.ReadI();
        _y = reader.ReadI();
        _z = reader.ReadI();
        int point = reader.ReadI(); // Fade 0, Instant 1.
        _heading = reader.ReadI();

        _telePos = VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
    }
}
