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

    // NetworkTransformReceive has its own independent "safety" position sync (FixedUpdate,
    // always on unless paused) that snaps the transform back toward the last position it was
    // told about via SetNewPosition - which nothing ever calls. Left unpaused, it fights the
    // CharacterController.Move() calls below: the entity slides toward the target, gets yanked
    // back once the drift crosses its threshold, then slides forward again - repeating forever.
    // NetworkCharacterControllerReceive (the OTHER, player-input movement path) already pauses it
    // during moves; MoveAllCharacters needs to do the same for the entities it drives.
    private readonly NetworkTransformReceive _networkTransformReceive;
    private bool _syncPaused = false;

    // The UserState* animator state behaviours (Wait/Run for other players) decide whether to
    // switch into the running animation by polling NetworkCharacterControllerReceive.IsMoving() -
    // which otherwise only reflects system A's own _direction field, never touched by this mover.
    private readonly NetworkCharacterControllerReceive _networkCharacterControllerReceive;

    public MovementData(Entity mEntity , MovementTarget movementTarget)
    {
        _entity = mEntity;
        _movementTarget = movementTarget;
        _isMove = true;
        _networkTransformReceive = mEntity.GetComponent<NetworkTransformReceive>();
        _networkCharacterControllerReceive = mEntity.GetComponent<NetworkCharacterControllerReceive>();
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
        if (!isMove)
        {
            ResumeSync();
        }
    }

    private void PauseSync()
    {
        if (_syncPaused) return;
        _syncPaused = true;
        _networkTransformReceive?.PausePositionSync();
        _networkCharacterControllerReceive?.SetExternalMoveActive(true);
        PlayUserAnimation(AnimationNames.RUN);
    }

    private void ResumeSync()
    {
        if (!_syncPaused) return;
        _syncPaused = false;

        // _serverPosition/_lastPos are still frozen at wherever they were the last time
        // SetNewPosition ran (nothing else ever calls it) - resuming without refreshing them means
        // the safety net immediately sees the whole distance just traveled as "desync" and yanks
        // the entity straight back to that stale point. Telling it "here is valid" first prevents
        // that snap and keeps it from comparing against stale data on the next move too.
        if (_networkTransformReceive != null)
        {
            _networkTransformReceive.SetNewPosition(_entity.transform.position);
            _networkTransformReceive.ResumePositionSync();
        }

        _networkCharacterControllerReceive?.SetExternalMoveActive(false);
        PlayUserAnimation(AnimationNames.WAIT);
    }

    /// <summary>
    /// The local player and NPCs both switch run/wait explicitly (NewRunningState/NewIdleState via
    /// AnimationManager.PlayAnimation, NpcEntity.OnStartL2jMoving/OnStopL2jMoving) rather than by
    /// polling for movement - UserStateWait's own IsMoving() polling turned out not to actually
    /// trigger the Wait->Run switch in practice, so other players get the same explicit treatment
    /// here instead of relying on it.
    /// </summary>
    private void PlayUserAnimation(Animation animation)
    {
        if (_entity is UserEntity user)
        {
            AnimationManager.Instance.PlayMonsterAnimation(user.IdentityInterlude.Id, animation.ToString() + user.Gear.WeaponAnim);
        }
    }

    public float GetSpeed()
    {
        return (_entity.Running) ? _entity.Stats.UnitySpeedRun : _entity.Stats.UnitySpeedWalking;
    }

    public void Move(Vector3 direction , float speed)
    {
        PauseSync();
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
        // Position must be settled before SetIsMove(false) resumes the safety net below, since
        // that snapshots the current transform position as the new known-good one.
        _entity.transform.position = new Vector3(target.x, 0, target.z);
        SetIsMove(false);
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
        else if (entity.GetType() == typeof(UserEntity))
        {
            var _uEntity = (UserEntity)entity;
            character = _uEntity.GetCharacterController();

        }
        return character;
    }

}
