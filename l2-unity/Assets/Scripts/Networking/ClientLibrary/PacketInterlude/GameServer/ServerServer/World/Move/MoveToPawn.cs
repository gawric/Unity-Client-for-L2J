
using UnityEngine;


public class MoveToPawn : ServerPacket
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
    public MoveToPawn(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        //example objectId player test1
        _objectId = ReadI();
        //example npcId merchant
        _targetId = ReadI();
        
        _distance = VectorUtils.ConvertL2jDistance(ReadI());

        _x = ReadI();
        _y = ReadI();
        _z = ReadI();

        //_tx = ReadI();
        //_ty = ReadI();
       // _tz = ReadI();

        _objPos =  VectorUtils.ConvertPosToUnity(new Vector3(_x, _y, _z));
        //_targetPos = VectorUtils.ConvertPosToUnity(new Vector3(_tx, _ty, _tz));

        Debug.Log("");


    }
}
