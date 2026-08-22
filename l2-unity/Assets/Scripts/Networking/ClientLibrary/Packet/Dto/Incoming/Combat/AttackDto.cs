using UnityEngine;


public class AttackDto : IWireDto
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

    

    public void ReadFrom(PacketReader reader)
    {
        _attackerObjId = reader.ReadI();
        _targetObjId =  reader.ReadI();
        _damage = reader.ReadI();
       int _flags =  reader.ReadB();
        _firstHit = new Hit(_targetObjId, _damage, _flags);

       _aX = reader.ReadI();
       _aY = reader.ReadI();
       _aZ = reader.ReadI();

       int sizeHit = reader.ReadSh();

        _attackerPos = VectorUtils.ConvertPosToUnity(new Vector3(_aX, _aY, _aZ));

        array = new Hit[sizeHit];

       for(int i=0; i< sizeHit; i++)
       {
            int _tId = reader.ReadI();
            int _dam = reader.ReadI();
            int _fl = (int)reader.ReadB();
            Hit hit1 = new Hit(_tId, _dam, _fl);
            array[i] = hit1;
        }

        _tX = reader.ReadI();
        _tY = reader.ReadI();
        _tZ = reader.ReadI();
        _targetPos = VectorUtils.ConvertPosToUnity(new Vector3(_tX, _tY, _tZ));
    }

   
    
}
