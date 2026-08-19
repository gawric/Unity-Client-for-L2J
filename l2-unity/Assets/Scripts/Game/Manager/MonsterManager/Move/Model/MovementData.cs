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
    private bool _hasLastPos;
    private bool _keepFollowLogged;


    public MovementData(Entity mEntity , MovementTarget movementTarget)
    {
        _entity = mEntity;
        _movementTarget = movementTarget;
        _isMove = true;
    }

    public bool ConsumeKeepFollowLog()
    {
        if (_keepFollowLogged)
            return false;
        _keepFollowLogged = true;
        return true;
    }

    public Entity GetEntity()
    {
        return _entity;
    }

    public bool IsEntity()
    {
        return _entity != null;
    }

    public void SetLastPosition(Vector3 lastPos)
    {
        _lastPos = lastPos;
        _hasLastPos = true;
    }

    public bool HasLastPosition()
    {
        return _hasLastPos;
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
        Vector3 lookDir = new Vector3(direction.x, 0f, direction.z);
        direction = ApplyGravity(direction);
        CharacterMove(_entity, direction, speed);
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);
            _entity.transform.rotation = Quaternion.Slerp(_entity.transform.rotation, lookRotation, Time.deltaTime * 5.0f);
        }
    }

    private void CharacterMove(Entity entity , Vector3 direction , float speed)
    {
        CharacterController character = GetControllerToTypeEntity(entity);
        StartMove(character, direction, speed);
        
    }

   

    private void StartMove(CharacterController character, Vector3 direction, float speed)
    {
        if (character == null || !character.enabled || !character.gameObject.activeInHierarchy)
            return;

        if (character.radius <= 0.001f || character.height <= 0.001f)
            return;

        character.Move(direction * speed * Time.deltaTime);
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
        Vector3 dest;
        if (_movementTarget.IsActorTarget())
        {
            // MoveToPawn: already inside Dist of the pawn. Do not snap to pawn origin
            // (that ran UserEntity into the monster center).
            dest = _entity.transform.position;
        }
        else
        {
            dest = new Vector3(target.x, _entity.transform.position.y, target.z);
        }
        dest = GroundSnapHelper.SnapToGroundOrKeep(dest);
        Entity pawn = EntityActionCombatLog.PawnOf(_entity);
        EntityActionCombatLog.LogCiPawn(_entity,
            "Move.Finish nick=" + EntityActionCombatLog.NameOf(_entity) +
            " actorTarget=" + _movementTarget.IsActorTarget() +
            " finishDest=" + EntityActionCombatLog.Vec(dest) +
            " now=" + EntityActionCombatLog.Vec(_entity.transform.position) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " nowToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(_entity.transform.position, pawn.transform.position).ToString("F2")
                : "-") +
            " finishToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(dest, pawn.transform.position).ToString("F2")
                : "-") +
            " " + EntityActionCombatLog.ClassifyDest(dest, pawn) +
            EntityActionCombatLog.ChaseDump(_entity, pawn));
        if (_entity is UserEntity)
            EntitySpawnShared.ApplyGroundedTransform(_entity.gameObject, dest, _entity.transform.rotation);
        else
            _entity.transform.position = dest;
        SetEventToTypeEntity(_entity);
    }


    public void SetEventToTypeEntity(Entity entity)
    {
        if (entity is PlayerEntity)
            return;

        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.NotifyArrived(entity);
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
        else if (entity.GetType() == typeof(UserEntity))
        {
            character = ((UserEntity)entity).GetCharacterController();
        }
        return character;
    }

}
