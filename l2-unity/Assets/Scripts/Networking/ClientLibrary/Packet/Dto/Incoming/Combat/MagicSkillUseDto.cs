using UnityEngine;

public class MagicSkillUseDto : IWireDto
{
    private int _attackerObjId;
    private int _targetObjId;
    private Vector3 _attackerPos;
    private Vector3 _targetPos;
    private Skillgrp _skillGrp;
    private int _aX;
    private int _aY;
    private int _aZ;

    private int _tX;
    private int _tY;
    private int _tZ;

    private int _skillId;
    private int _skilllvl;
    private int _hittime;
    private int _reusedelay;
    private int _critical;
    private Entity attacker;

    public void SetAttacker(Entity entity)
    {
        attacker = entity;
    }
    public Entity EntityAttacker { get => attacker; }
    public int SkillId { get => _skillId; }

    public int SkillLvl { get => _skilllvl; }

    public int HitTime { get => _hittime; }

    public int Reusedelay { get => _reusedelay; }

    public int AttackerObjId { get => _attackerObjId; }

    public int TargetId { get => _targetObjId; }

    public Vector3 AttackerPos { get => _attackerPos; }
    public Vector3 TargetPos { get => _targetPos; }

    public Skillgrp SkillGrp { get => _skillGrp; }

    public static MagicSkillUseDto From(
        int attackerId, int targetId, int skillId, int skillLevel,
        int hitTime, int reuseDelay, Vector3 attackerPos, Vector3 targetPos)
    {
        MagicSkillUseDto dto = new MagicSkillUseDto
        {
            _attackerObjId = attackerId,
            _targetObjId = targetId,
            _skillId = skillId,
            _skilllvl = skillLevel,
            _hittime = hitTime,
            _reusedelay = reuseDelay,
            _attackerPos = attackerPos,
            _targetPos = targetPos,
        };
        dto._aX = (int)attackerPos.x;
        dto._aY = (int)attackerPos.y;
        dto._aZ = (int)attackerPos.z;
        dto._tX = (int)targetPos.x;
        dto._tY = (int)targetPos.y;
        dto._tZ = (int)targetPos.z;
        dto._skillGrp = SkillgrpTable.Instance.GetSkill(skillId, skillLevel);
        return dto;
    }

    

    public void ReadFrom(PacketReader reader)
    {
        _attackerObjId = reader.ReadI();
        _targetObjId = reader.ReadI();

        _skillId = reader.ReadI();
        // Debug.Log("MagicSkillUse->skillID " + _skillId);
        _skilllvl = reader.ReadI();
        _hittime = reader.ReadI();
        // Debug.Log("MagicSkillUse->hittime " + _hittime);
        _reusedelay = reader.ReadI();

        _aX = reader.ReadI();
        _aY = reader.ReadI();
        _aZ = reader.ReadI();
        _attackerPos = VectorUtils.ConvertPosToUnity(new Vector3(_aX, _aY, _aZ));
        
        _critical = reader.ReadI();

        if(_critical == 1)
        {
            reader.ReadSh();
        }

        _tX = reader.ReadI();
        _tY = reader.ReadI();
        _tZ = reader.ReadI();
        _targetPos = VectorUtils.ConvertPosToUnity(new Vector3(_tX, _tY, _tZ));
        _skillGrp = SkillgrpTable.Instance.GetSkill(_skillId, _skilllvl);
    }


}
