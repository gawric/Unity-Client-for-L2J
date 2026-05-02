using UnityEngine;

public class GravityData
{
    private Entity _entity;

    public GravityData(Entity entity)
    {
        _entity = entity;
    }

    public bool IsDead()
    {
        return _entity.IsDead();
    }

    public CharacterController GetControllerToTypeEntity()
    {
        CharacterController character = null;

        if (_entity.GetType() == typeof(MonsterEntity))
        {
            var _mEntity = (MonsterEntity)_entity;
            character = _mEntity.GetCharacterController();

        }
        else if (_entity.GetType() == typeof(NpcEntity))
        {
            var _mEntity = (NpcEntity)_entity;
            character = _mEntity.GetCharacterController();

        }
        return character;
    }


}
