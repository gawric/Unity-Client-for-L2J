using UnityEngine;

public class MovementData
{
    private Entity _entity;
    private MovementTarget _movementTarget;

    private float _verticalVelocity = 0;
    private float _gravity = 28;
    private bool _isMove;
    private Vector3 _lastPos;
    private bool _hasLastPos;
    private bool _spdTripStarted;
    private float _spdTripStartTime;
    private float _spdTripDist2d;
    private float _spdLastLogTime;
    private bool _spdLastRunning;
    private string _spdLastGait;
    private bool _spdHasGait;
    private readonly float _locomotionCheckAt;
    private readonly Vector3 _locomotionCheckStart;
    private bool _locomotionChecked;

    const float LocomotionCheckDelay = 0.4f;
    const float LocomotionCheckRadius = 50f;

    // Interlude MoveTimer. A new click while still moving keeps this value.
    private float _moveTimer;
    private string _gaitAnim;

    readonly L2PawnYaw _pawnYaw;

    public MovementData(Entity mEntity , MovementTarget movementTarget, L2PawnYaw pawnYaw)
    {
        _entity = mEntity;
        _movementTarget = movementTarget;
        _pawnYaw = pawnYaw;
        _isMove = true;
        _locomotionCheckAt = Time.time + LocomotionCheckDelay;
        _locomotionCheckStart = mEntity != null ? mEntity.transform.position : Vector3.zero;
    }

    public bool IsOwnerDead()
    {
        return _entity != null && _entity.IsDead();
    }

    public static bool IsPlayerPawn(Entity entity)
    {
        return entity is UserEntity || entity is PlayerEntity;
    }

    public void InheritMoveTimer(MovementData prev)
    {
        if (prev == null || !IsPlayerPawn(_entity))
            return;
        _moveTimer = prev._moveTimer;
        _gaitAnim = prev._gaitAnim;
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

    public bool ReconcileStoppedAnimation()
    {
        if (_entity == null || _entity.IsDead() || _entity.ActionSlot == null)
            return true;

        EntityActionKind action = _entity.ActionSlot.Action;
        if (action == EntityActionKind.Attack ||
            action == EntityActionKind.Skill ||
            action == EntityActionKind.Stop)
            return false;

        NetworkAnimationController controller = _entity.GetAnimatorController();
        Animator animator = controller != null ? controller.GetAnimator() : null;
        if (animator == null)
            return true;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool staleLocomotion = state.IsName("walk") || state.IsName("run");
        if (!staleLocomotion)
            return true;

        Debug.Log(
            "[MOVE_ANIM_RECONCILE] nick=" + EntityActionCombatLog.NameOf(_entity) +
            " objectId=" + (_entity.Identity != null ? _entity.Identity.Id : 0) +
            " action=" + action +
            " state=" + (state.IsName("run") ? "run" : "walk") +
            " moving=false -> idle");

        if (action == EntityActionKind.Move && EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Set(_entity, EntityActionKind.Idle, null);
        else
            EntityActionVisual.PlayStandWait(_entity);
        return true;
    }

    public float GetSpeed()
    {
        if (_entity.Stats == null)
            return 0f;
        bool npc = !IsPlayerPawn(_entity);
        return L2PlayerMoveGait.Speed(
            _moveTimer,
            _entity.Running,
            npc,
            _entity.Stats.UnitySpeedWalking,
            _entity.Stats.UnitySpeedRun);
    }

    public bool IsWalkGait()
    {
        return L2PlayerMoveGait.IsWalkStart(_moveTimer, _entity.Running, !IsPlayerPawn(_entity));
    }

    public void AdvanceMoveTimer()
    {
        if (IsPlayerPawn(_entity))
            _moveTimer += Time.deltaTime;
    }

    public float GetMoveTimer()
    {
        return _moveTimer;
    }

    public void SyncUserGait()
    {
        if (!IsPlayerPawn(_entity))
            return;
        bool walking = IsWalkGait();
        string anim = walking ? "walk" : "run";
        if (_gaitAnim == anim)
            return;
        _gaitAnim = anim;
        EntityActionVisual.StartMove(_entity, walking);
    }

    public void ReplayUserGait()
    {
        _gaitAnim = null;
        SyncUserGait();
    }

    public void SpeedAccum2d(float dist2d)
    {
        if (!_spdTripStarted)
        {
            _spdTripStarted = true;
            _spdTripStartTime = Time.time;
            _spdTripDist2d = 0f;
        }
        _spdTripDist2d += dist2d;
    }

    public bool SpeedShouldLog(float now, float intervalSec)
    {
        if (now - _spdLastLogTime < intervalSec)
            return false;
        _spdLastLogTime = now;
        return true;
    }

    public bool SpeedMarkGait(bool running, string gait)
    {
        if (!_spdHasGait)
        {
            _spdHasGait = true;
            _spdLastRunning = running;
            _spdLastGait = gait;
            return false;
        }
        bool changed = _spdLastRunning != running || _spdLastGait != gait;
        _spdLastRunning = running;
        _spdLastGait = gait;
        return changed;
    }

    public float SpeedTripSec(float now)
    {
        if (!_spdTripStarted)
            return 0f;
        return now - _spdTripStartTime;
    }

    public float SpeedTripDist2d()
    {
        return _spdTripDist2d;
    }

    public void Move(Vector3 direction , float speed)
    {
        Vector3 lookDir = new Vector3(direction.x, 0f, direction.z);
        direction = ApplyGravity(direction);
        CharacterMove(_entity, direction, speed);
        CheckLocomotionAnimation();
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            int id = _entity.Identity != null ? _entity.Identity.Id : 0;
            if (id == 0 || CombatFacingService.Instance == null || !CombatFacingService.Instance.HasFollow(id))
                if (_pawnYaw != null)
                    _pawnYaw.RequestLook(_entity.transform, _entity.transform.position + lookDir, L2PawnYaw.RotationRateYaw);
        }
    }

    private void CheckLocomotionAnimation()
    {
        if (_locomotionChecked || Time.time < _locomotionCheckAt)
            return;
        if (!(_entity is MonsterEntity) && !(_entity is NpcEntity))
        {
            _locomotionChecked = true;
            return;
        }

        PlayerEntity player = PlayerEntity.Instance;
        if (player == null)
            return;

        float distanceToPlayer = VectorUtils.Distance2D(
            _entity.transform.position,
            player.transform.position);
        if (distanceToPlayer > LocomotionCheckRadius)
        {
            _locomotionChecked = true;
            return;
        }

        _locomotionChecked = true;
        string expected = _entity.Running ? "run" : "walk";
        NetworkAnimationController controller = _entity.GetAnimatorController();
        Animator animator = controller != null ? controller.GetAnimator() : null;
        AnimatorStateInfo current = animator != null
            ? animator.GetCurrentAnimatorStateInfo(0)
            : default;
        AnimatorStateInfo next = animator != null && animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : default;
        bool playingExpected = animator != null &&
            (current.IsName(expected) ||
             animator.IsInTransition(0) && next.IsName(expected));
        float moved = VectorUtils.Distance2D(_locomotionCheckStart, _entity.transform.position);

        Debug.Log(
            "MONSTER_WALK result=" + (playingExpected ? "OK" : "MISS") +
            " nick=" + EntityActionCombatLog.NameOf(_entity) +
            " objectId=" + (_entity.Identity != null ? _entity.Identity.Id : 0) +
            " expected=" + expected +
            " actual=" + EntityActionSkill.AnimatorStateName(animator) +
            " action=" + (_entity.ActionSlot != null ? _entity.ActionSlot.Action.ToString() : "-") +
            " moving=" + _isMove +
            " running=" + _entity.Running +
            " moved=" + moved.ToString("F3") +
            " distance=" + distanceToPlayer.ToString("F1") +
            " animator=" + (animator != null) +
            " enabled=" + (animator != null && animator.enabled) +
            " speed=" + (animator != null ? animator.speed.ToString("F3") : "-") +
            " walkBool=" + (animator != null ? animator.GetBool("walk").ToString() : "-") +
            " runBool=" + (animator != null ? animator.GetBool("run").ToString() : "-") +
            " hasState=" + (animator != null && animator.HasState(0, Animator.StringToHash(expected))));
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
        CharInfoSpeedLog.LogTripEnd(this, "Move.Finish");
        float finishD = VectorUtils.Distance2D(_entity.transform.position, dest);
        if (finishD >= 0.4f)
        {
            Vector3 nowL2 = VectorUtils.ConvertPosUnityToL2j(_entity.transform.position);
            Vector3 destL2 = VectorUtils.ConvertPosUnityToL2j(dest);
            Debug.Log("[MOVE_SNAP] FINISH nick=" + EntityActionCombatLog.NameOf(_entity) +
                " snapM=" + finishD.ToString("F2") +
                " nowL2=" + nowL2.x.ToString("F0") + "," + nowL2.y.ToString("F0") + "," + nowL2.z.ToString("F0") +
                " destL2=" + destL2.x.ToString("F0") + "," + destL2.y.ToString("F0") + "," + destL2.z.ToString("F0"));
        }
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
