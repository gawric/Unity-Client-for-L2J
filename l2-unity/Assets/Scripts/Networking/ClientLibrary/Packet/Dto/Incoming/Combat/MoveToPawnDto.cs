
using UnityEngine;


public class MoveToPawnDto : IWireDto
{
    private int _objectId;
    private int _targetId;
    private float _distance;

    private int _x;
    private int _y;
    private int _z;


    private Vector3 _objPos;


    public Vector3 ObjPos { get => _objPos; }


    public float Distance { get => _distance; }

    public int ObjId { get => _objectId; }

    public int TarObjid { get => _targetId; }
    

    public void ReadFrom(PacketReader reader)
    {
        //example objectId player test1
        _objectId = reader.ReadI();
        //example npcId merchant
        _targetId = reader.ReadI();
        
        // Engine stores Dist as UU on APawn+0x6C4 and compares to 2D Location (also UU).
        _distance = VectorUtils.ConvertL2UuToMeters(reader.ReadI());

        _x = reader.ReadI();
        _y = reader.ReadI();
        _z = reader.ReadI();

        //_tx = reader.ReadI();
        //_ty = reader.ReadI();
       // _tz = reader.ReadI();

        _objPos =  VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
        //_targetPos = VectorUtils.ConvertPosToUnity(new Vector3(_tx, _ty, _tz));

        Debug.Log("");


    }
}
