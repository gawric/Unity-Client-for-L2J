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
    public int Flags { get; private set; }
    public int AX { get => _aX; }
    public int AY { get => _aY; }
    public int AZ { get => _aZ; }

    public Vector3 AttackerPos { get => _attackerPos; }
    public Vector3 TargetPos { get => _targetPos; }
    public Hit[] ArrHit { get => array; }

    public Hit FirstHit { get => _firstHit; }

    public static AttackDto From(
        int attackerId, int targetId, int damage, int flags,
        Vector3 attackerPos, Vector3 targetPos, Hit[] hits)
    {
        Hit[] allHits = hits ?? new Hit[0];
        AttackDto dto = new AttackDto
        {
            _attackerObjId = attackerId,
            _targetObjId = targetId,
            _damage = damage,
            Flags = flags,
            _attackerPos = attackerPos,
            _targetPos = targetPos,
            _aX = (int)attackerPos.x,
            _aY = (int)attackerPos.y,
            _aZ = (int)attackerPos.z,
            _tX = (int)targetPos.x,
            _tY = (int)targetPos.y,
            _tZ = (int)targetPos.z,
            array = allHits,
            _firstHit = allHits.Length > 0 ? allHits[0] : new Hit(targetId, damage, flags),
        };
        return dto;
    }

    

    public void ReadFrom(PacketReader reader)
    {
        _attackerObjId = reader.ReadI();
        _targetObjId =  reader.ReadI();
        _damage = reader.ReadI();
       Flags =  reader.ReadB();
        _firstHit = new Hit(_targetObjId, _damage, Flags);

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
