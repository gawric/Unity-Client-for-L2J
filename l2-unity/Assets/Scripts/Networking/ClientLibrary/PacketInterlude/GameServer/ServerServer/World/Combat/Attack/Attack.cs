using UnityEngine;


public class Attack : ServerPacket
{
    private int _attackerObjId;
    private int _targetObjId;
    private int _damage;
    private Vector3 _attackerPos;
    private Vector3 _targetPos;
    private int _aX;
    private int _aY;
    private int _aZ;

    private int _tX;
    private int _tY;
    private int _tZ;

    private Hit _firstHit;
    private Hit[] array;

    public int AttackerObjId { get => _attackerObjId; }

    public int TargetId { get => _targetObjId; }

    public int Damage { get => _damage; }

    public Vector3 AttackerPos { get => _attackerPos; }
    public Vector3 TargetPos { get => _targetPos; }
    public Hit[] ArrHit { get => array; }

    public Hit FirstHit { get => _firstHit; }

    public Attack(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _attackerObjId = ReadI();
        _targetObjId =  ReadI();
        _damage = ReadI();
       int _flags =  ReadB();
        _firstHit = new Hit(_targetObjId, _damage, _flags);

        _aX = ReadI();
       _aY = ReadI();
       _aZ = ReadI();
       int sizeHit = ReadSh();

        _attackerPos = VectorUtils.ConvertPosToUnity(new Vector3(_aX, _aY, _aZ));

        array = new Hit[sizeHit];

       for(int i=0; i< sizeHit; i++)
       {
            int _tId = ReadI();
            int _dam = ReadI();
            int _fl = (int)ReadB();
            Hit hit1 = new Hit(_tId, _dam, _fl);
            array[i] = hit1;
        }

        _tX = ReadI();
        _tY = ReadI();
        _tZ = ReadI();
        _targetPos = VectorUtils.ConvertPosToUnity(new Vector3(_tX, _tY, _tZ));
    }

   
    
}
