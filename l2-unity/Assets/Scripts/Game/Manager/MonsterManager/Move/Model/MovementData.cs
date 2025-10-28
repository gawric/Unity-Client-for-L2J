using UnityEngine;

public class MovementData
{
    private Entity _entity;
    private MovementTarget _movementTarget;

    private float _verticalVelocity = 0;
    private float _gravity = 28;
    private bool _isMove;
    private bool _isRotate = false;
    private Vector3 _lastPos;


    public MovementData(Entity mEntity , MovementTarget movementTarget)
    {
        _entity = mEntity;
        _movementTarget = movementTarget;
        _isMove = true;
    }

    public bool IsEntity()
    {
        return _entity != null;
    }

    public void SetLastPosition(Vector3 lastPos)
    {
        _lastPos = lastPos;
    }

    public Vector3 GetLastPosition()
    {
        return _lastPos;
    }

    public MovementTarget GetMovementTarget()
    {
        return _movementTarget;
    }

    public Transform GetTransform()
    {
        return _entity.transform;
    }

    public float GetDistance()
    {
        return _movementTarget.GetDistance();
    }
    public bool IsMove()
    {
        return _isMove;
    }

    public void SetIsMove(bool isMove)
    {
        _isMove = isMove;
    }

    public float GetSpeed()
    {
        return (_entity.Running) ? _entity.Stats.UnitySpeedRun : _entity.Stats.UnitySpeedWalking;
    }

    public void Move(Vector3 direction , float speed)
    {
        direction = ApplyGravity(direction);
        CharacterMove(_entity, direction, speed);
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        _entity.transform.rotation = Quaternion.Slerp(_entity.transform.rotation, lookRotation, Time.deltaTime * 5.0f);

    }

    private void CharacterMove(Entity entity , Vector3 direction , float speed)
    {
        CharacterController character = GetControllerToTypeEntity(entity);
        StartMove(character, direction, speed);
        
    }

   

    private void StartMove(CharacterController character , Vector3 direction, float speed)
    {
        
        if (character != null)
        {
            character.Move(direction * speed * Time.deltaTime);
        }
    }

    private Vector3 ApplyGravity(Vector3 dir)
    {
        /* Handle gravity */
        var character = GetControllerToTypeEntity(_entity);

        if(character != null)
        {
            if (character.isGrounded)
            {
                if (_verticalVelocity < -1.25f)
                {
                    _verticalVelocity = -1.25f;
                }
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }
            dir.y = _verticalVelocity;

            return dir;
        }

        return dir;
    }

    public void OnFinish(Vector3 target)
    {
        _isMove = false;
        _entity.transform.position = new Vector3(target.x, 0, target.z); ;
        SetEventToTypeEntity(_entity);
    }


    public void SetEventToTypeEntity(Entity entity)
    {
        if (entity.GetType() == typeof(MonsterEntity))
        {
            var _mEntity = (MonsterEntity)entity;
           _mEntity.GetStateMachine().NotifyEvent(Event.ARRIVED);

        }
        else if (entity.GetType() == typeof(NpcEntity))
        {
            var _mEntity = (NpcEntity)entity;
            _mEntity.GetStateMachine().NotifyEvent(Event.ARRIVED);
        }
    }
    public CharacterController GetControllerToTypeEntity(Entity entity)
    {
        CharacterController character = null;

        if (entity.GetType() == typeof(MonsterEntity))
        {
            var _mEntity = (MonsterEntity)entity;
            character = _mEntity.GetCharacterController();

        }
        else if (entity.GetType() == typeof(NpcEntity))
        {
            var _mEntity = (NpcEntity)entity;
            character = _mEntity.GetCharacterController();

        }
        return character;
    }

}
