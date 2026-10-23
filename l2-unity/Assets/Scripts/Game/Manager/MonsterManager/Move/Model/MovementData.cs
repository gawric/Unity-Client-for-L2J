using UnityEngine;

public class MovementData
{
    private Entity _entity;
    private Transform _transform;
    private L2PawnYawDrive _yawDrive;
    private MovementTarget _movementTarget;

    private float _verticalVelocity = 0;
    private float _gravity = 28;

    // PhysicsVolume.TerminalVelocity (2500 UU/s) scaled to Unity meters (1/52.5),
    // matching PlayerController so falling monsters/NPCs cap identically.
    const float TerminalVelocity = 47.62f;
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

    // Port of Interlude ULevel::ToFloor / APawn::processLanded ground settle
    // (TraceCallers.h / TraceBodies.h, flags 0x086 / 0x297). The pawn drops a
    // capsule (CollisionRadius, CollisionRadius, CollisionHeight) straight down
    // and its centre rests CollisionHeight above the hit surface.
    const float GroundCastStartAbove = 20f;   // dbl_1F3F9A8 (20.0) spawn-probe lift
    const float GroundCastMaxFall = 300f;      // matches GroundSnapHelper.DefaultMaxDistance

    // Interlude MoveTimer. A new click while still moving keeps this value.
    // Persisted on Entity.LocomotionTimer so the walk->run timer survives
    // MovementData teardown (OnFinish between move packets) instead of resetting
    // to 0 and replaying the 0.4s walk window every packet.
    private float _moveTimer
    {
        get { return _entity != null ? _entity.LocomotionTimer : 0f; }
        set { if (_entity != null) _entity.LocomotionTimer = value; }
    }

    // Local per-MovementData animation cache. NOT persisted on Entity: it must
    // reset with each MovementData so a fresh move always re-plays the locomotion
    // clip (StartMove) instead of being suppressed by a stale cached gait.
    private string _gaitAnim;
    private float _lastForceGaitAt;

    // [GAIT] diagnostics: per-entity log throttle + normalizedTime delta.
    [System.NonSerialized] static readonly System.Collections.Generic.Dictionary<int, float> _syncPrevLogAt =
        new System.Collections.Generic.Dictionary<int, float>();
    [System.NonSerialized] static readonly System.Collections.Generic.Dictionary<int, float> _syncPrevNorm =
        new System.Collections.Generic.Dictionary<int, float>();

    readonly L2PawnYaw _pawnYaw;

    public MovementData(Entity mEntity , MovementTarget movementTarget, L2PawnYaw pawnYaw)
    {
        _entity = mEntity;
        _transform = mEntity != null ? mEntity.transform : null;
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
        // Persist only the walk->run timer. Do NOT copy _gaitAnim: it is a
        // per-MovementData animation cache. Copying it makes SyncUserGait think the
        // clip is already playing and skip StartMove, so point->pawn transitions
        // (and other fresh moves) never re-trigger the run/walk animation.
        _moveTimer = prev._moveTimer;
    }

    /// <summary>
    /// Interlude AddMoveTo: overwrite Destination on the same move, keep timer/gait.
    /// </summary>
    public void RetargetPoint(Vector3 destination, bool running)
    {
        _isMove = true;
        if (_movementTarget == null)
            _movementTarget = new MovementTarget(destination, 0.1f, running);
        else
            _movementTarget.Set(destination, 0.1f, running);
    }

    /// <summary>
    /// Same as RetargetPoint but for MoveToPawn chase packets: keep the existing
    /// MovementData (and its gait cache) instead of tearing it down every packet.
    /// </summary>
    public void RetargetPawn(Transform pawn, float stopDistance, bool running)
    {
        _isMove = true;
        if (_movementTarget == null)
            _movementTarget = new MovementTarget(pawn, stopDistance, running);
        else
            _movementTarget.Set(pawn, stopDistance, running);
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
        if (_transform == null && _entity != null)
            _transform = _entity.transform;
        return _transform;
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
        {
            EntitySpawnShared.LogStriderGait("reconcile-skip", _entity, "reason=action");
            return false;
        }

        NetworkAnimationController controller = _entity.GetAnimatorController();
        Animator animator = controller != null ? controller.GetAnimator() : null;
        if (animator == null)
            return true;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool staleLocomotion = state.IsName("walk") || state.IsName("run");
        if (!staleLocomotion)
        {
            EntitySpawnShared.LogStriderGait("reconcile-keep", _entity, "reason=not-run-or-walk");
            return true;
        }

        EntitySpawnShared.LogStriderGait("reconcile-stop", _entity, "to=wait");

        // Debug.Log(
            // "[MOVE_ANIM_RECONCILE] nick=" + EntityActionCombatLog.NameOf(_entity) +
            // " objectId=" + (_entity.Identity != null ? _entity.Identity.Id : 0) +
            // " action=" + action +
            // " state=" + (state.IsName("run") ? "run" : "walk") +
            // " moving=false -> idle");

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

        // Heal leftover CullCompletely from older spawns (freezes mid-run when
        // briefly off-camera). New spawns already use CullUpdateTransforms.
        UserEntity userForCull = _entity as UserEntity;
        if (userForCull != null)
        {
            NetworkAnimationController nacCull = userForCull.GetAnimatorController();
            Animator anmCull = nacCull != null ? nacCull.GetAnimator() : null;
            if (anmCull != null && anmCull.cullingMode == AnimatorCullingMode.CullCompletely)
                anmCull.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        // [GAIT] diagnostics — per-entity throttle so concurrent CharInfo don't
        // starve each other of sync lines (global throttle hid MMagic freezes).
        int entityId = _entity.Identity != null ? _entity.Identity.Id : 0;
        float lastLogAt;
        _syncPrevLogAt.TryGetValue(entityId, out lastLogAt);
        if (_entity is UserEntity && GaitLog.Enabled &&
            Time.time - lastLogAt >= 0.25f)
        {
            _syncPrevLogAt[entityId] = Time.time;
            UserEntity ue = _entity as UserEntity;
            string clip = "?";
            string stateWant = anim + "_" + ue.WeaponAnim;
            float animSpeed = 0f;
            float runSpeed = 0f;
            float walkSpeed = 0f;
            float normTime = 0f;
            float normDelta = 0f;
            bool inTrans = false;
            bool already = false;
            string cull = "?";
            NetworkAnimationController nac = ue.GetAnimatorController();
            Animator anm = nac != null ? nac.GetAnimator() : null;
            if (anm != null)
            {
                AnimatorClipInfo[] clips = anm.GetCurrentAnimatorClipInfo(0);
                clip = clips != null && clips.Length > 0 && clips[0].clip != null
                    ? clips[0].clip.name : "<none>";
                animSpeed = anm.speed;
                runSpeed = anm.GetFloat("run_speed");
                walkSpeed = anm.GetFloat("walk_speed");
                AnimatorStateInfo st = anm.GetCurrentAnimatorStateInfo(0);
                normTime = st.normalizedTime;
                inTrans = anm.IsInTransition(0);
                already = PlayerLocomotionCrossFade.IsAlreadyPlaying(anm, stateWant);
                cull = anm.cullingMode.ToString();
                if (_syncPrevNorm.TryGetValue(entityId, out float prev))
                    normDelta = normTime - prev;
                _syncPrevNorm[entityId] = normTime;
            }
            GaitLog.Log(
                _entity, "sync",
                "anim=" + anim +
                " cached=" + (_gaitAnim ?? "none") +
                " want=" + stateWant +
                " clip=" + clip +
                " already=" + already +
                " inTrans=" + inTrans +
                " cull=" + cull +
                " animSpeed=" + animSpeed.ToString("F2") +
                " runSpeed=" + runSpeed.ToString("F3") +
                " walkSpeed=" + walkSpeed.ToString("F3") +
                " normT=" + normTime.ToString("F3") +
                " normD=" + normDelta.ToString("F3") +
                " running=" + _entity.Running +
                " moveTimer=" + _moveTimer.ToString("F3") +
                " inCombat=" + _entity.InCombat +
                " attackVis=" + ue.IsAttackVisualPlaying());
            MagicGaitTrace.SyncTick(_entity,
                "anim=" + anim +
                " cached=" + (_gaitAnim ?? "none") +
                " moveTimer=" + _moveTimer.ToString("F3") +
                " offGait=" + IsAnimatorOffGait(anim));
        }

        if (_entity is UserEntity && _gaitAnim != anim && GaitLog.Enabled)
        {
            GaitLog.Log(
                _entity, "transition",
                "from=" + (_gaitAnim ?? "none") +
                " to=" + anim +
                " running=" + _entity.Running +
                " identityRunning=" + (_entity.Identity != null ? _entity.Identity.IsRunning : false) +
                " moveTimer=" + _moveTimer.ToString("F3"));
            MagicGaitTrace.Event(_entity, "gait-transition",
                "from=" + (_gaitAnim ?? "none") + " to=" + anim);
        }

        if (_gaitAnim == anim)
        {
            // Reconcile: something (PlayCombatWait/PlayStandWait/Idle.Enter/
            // Pickup/presentation) may have swapped the animator to a non-locomotion
            // state (atkwait/wait/jatk/pickup) while _gaitAnim still caches run/walk.
            // In that case the pawn keeps translating (MoveToPoint still steps it)
            // but the animator is frozen in a stance. Detect the actual state and replay.
            if (IsAnimatorOffGait(anim))
            {
                // Cooldown so a wait→run CrossFade cannot be ForceReplay'd every
                // frame (that restarts the fade and freezes MMagic mid-stride).
                if (Time.time - _lastForceGaitAt >= 0.35f)
                {
                    _lastForceGaitAt = Time.time;
                    _gaitAnim = anim;
                    MagicGaitTrace.Event(_entity, "FORCE-GAIT", "to=" + anim + " reason=OffGait");
                    EntityActionVisual.StartMove(_entity, walking, force: true);
                    if (GaitLog.Enabled)
                        GaitLog.Log(_entity, "force-gait", "to=" + anim);
                }
                else
                {
                    MagicGaitTrace.Decision(_entity, "force-gait-COOLDOWN",
                        "to=" + anim + " left=" + (0.35f - (Time.time - _lastForceGaitAt)).ToString("F2"));
                }
            }
            return;
        }
        _gaitAnim = anim;
        MagicGaitTrace.Decision(_entity, "StartMove", "walking=" + walking + " force=false");
        EntityActionVisual.StartMove(_entity, walking);
    }

    // True when the cached gait says walk/run but the animator is NOT in (and not
    // fading into) that locomotion state. IMPORTANT: check IsAlreadyPlaying FIRST.
    // During a healthy wait→run CrossFade the current clip is still wait_*, but
    // next=run; treating that as OffGait ForceReplay every frame restarts the fade
    // forever ("half step then freeze") — especially visible on MMagic MoveToPawn
    // after NotifyArrived PlayStandWait.
    bool IsAnimatorOffGait(string wantedGait)
    {
        UserEntity ue = _entity as UserEntity;
        if (ue == null)
            return false;
        NetworkAnimationController nac = ue.GetAnimatorController();
        Animator anm = nac != null ? nac.GetAnimator() : null;
        if (anm == null)
            return false;

        string state = wantedGait + "_" + ue.WeaponAnim;
        if (PlayerLocomotionCrossFade.IsAlreadyPlaying(anm, state))
            return false;

        AnimatorClipInfo[] clips = anm.GetCurrentAnimatorClipInfo(0);
        string clipName = clips != null && clips.Length > 0 && clips[0].clip != null
            ? clips[0].clip.name
            : null;

        // Stuck on a non-locomotion clip with no fade into the wanted gait.
        if (!string.IsNullOrEmpty(clipName))
        {
            bool isWantedLocomotion =
                clipName.IndexOf(".ao_" + wantedGait + "_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("_" + wantedGait + "_", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (isWantedLocomotion)
                return false;
            if (clipName.IndexOf(".ao_wait_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf(".ao_atkwait_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf(".ao_Wait_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("jatk", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("PicItem", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("pickup", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("MagicShot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("CastLong", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("CastMid", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clipName.IndexOf("CastEnd", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return true;
    }

    public void ReplayUserGait()
    {
        MagicGaitTrace.Decision(_entity, "ReplayUserGait", "cached→null then Sync");
        _gaitAnim = null;
        // Safe, non-forcing replay used on frequent retargets (move-point packets).
        // SyncUserGait will re-play only if the animator is not already in the gait
        // state, so this does not restart the run clip every packet.
        SyncUserGait();
    }

    // One-shot forcing replay: guarantees the locomotion clip (re)starts even if
    // the controller/animator reports it as already active or is mid-transition.
    // Used after a direct animator write (e.g. pickup / a no-exit pickup state)
    // which can otherwise leave the pawn frozen while it still translates.
    public void ForceReplayUserGait()
    {
        bool walking = IsWalkGait();
        _gaitAnim = walking ? "walk" : "run";
        MagicGaitTrace.Event(_entity, "ForceReplayUserGait", "walking=" + walking);
        EntityActionVisual.StartMove(_entity, walking, force: true);
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

    public void StepCharInfo(Vector3 destination, float speed)
    {
        Transform t = GetTransform();
        if (t == null)
            return;

        Vector3 location = t.position;
        Vector3 look = destination - location;
        look.y = 0f;
        bool moved = L2PawnWalk.TryStep(location, destination, speed, Time.deltaTime, out Vector3 stepped);
        if (!moved)
            stepped = location;

        // Interlude physWalking keeps Velocity.Z == 0 while grounded, then
        // processLanded / ToFloor settle the pawn onto the floor under it. We
        // reproduce that split: XY move first, then gravity + a downward capsule
        // probe to snap Y. Without this CharInfo players float / spawn underground.
        stepped = ApplyCharInfoGravityAndGround(stepped, Time.deltaTime, out _);

        LookAlong(t, look);

        Quaternion rot = t.rotation;
        bool rotated = _yawDrive != null && _yawDrive.TryConsumeStep(Time.deltaTime, out rot);
        if (moved && rotated)
            t.SetPositionAndRotation(stepped, rot);
        else if (moved)
            t.position = stepped;
        else if (rotated)
            t.rotation = rot;

        CheckLocomotionAnimation();
    }

    /// <summary>
    /// Port of Interlude APawn::physWalking (planar velocity + gravity) and
    /// ULevel::ToFloor / APawn::processLanded (0x086 / 0x297 downward capsule
    /// probe). Applies gravity to _verticalVelocity, then rests the pawn on the
    /// floor. Y is written as the surface height exactly (hit.point.y), matching
    /// GroundSnapHelper / spawn / NetworkTransformReceive so the pawn does not
    /// bounce between two different ground conventions.
    /// </summary>
    private Vector3 ApplyCharInfoGravityAndGround(Vector3 position, float deltaTime, out bool grounded)
    {
        float radius = _entity != null ? _entity.GetCollissionRadius() : 0f;
        if (radius <= 0.001f)
            radius = 0.05f;

        LayerMask mask = GroundSnapHelper.ResolveGroundMask(null);

        // Downward capsule probe starting above the pawn (Interlude ToFloor starts
        // at Location.Z - CollisionHeight, but we probe a safe distance above to
        // catch a pawn that ended up under the terrain surface).
        Vector3 origin = new Vector3(position.x, position.y + GroundCastStartAbove, position.z);
        bool hitFloor = Physics.SphereCast(
            origin, radius, Vector3.down, out RaycastHit hit,
            GroundCastStartAbove + GroundCastMaxFall,
            mask, QueryTriggerInteraction.Ignore);

        float targetY;
        if (hitFloor)
        {
            // Land on the surface. The project-wide ground convention is
            // transform.position.y == hit.point.y (see GroundSnapHelper), so we do
            // not add a capsule half-height here — otherwise the pawn lifts ~30-40cm
            // above the ground each frame vs. its spawn height.
            targetY = hit.point.y;
            _verticalVelocity = 0f;
            grounded = true;
        }
        else
        {
            // No floor below: keep falling (Interlude physFalling branch), capped
            // at terminal velocity like the fixed PlayerController.
            _verticalVelocity -= _gravity * deltaTime;
            if (_verticalVelocity < -TerminalVelocity)
                _verticalVelocity = -TerminalVelocity;
            targetY = position.y + _verticalVelocity * deltaTime;
            grounded = false;
        }

        return new Vector3(position.x, targetY, position.z);
    }

    public void Move(Vector3 direction , float speed)
    {
        Vector3 lookDir = new Vector3(direction.x, 0f, direction.z);
        if (L2PawnWalk.IsCharInfoPawn(_entity))
        {
            if (_movementTarget != null && _movementTarget.TryGetTarget(out Vector3 dest))
                StepCharInfo(dest, speed);
            return;
        }

        CharacterMove(_entity, direction, speed);

        CheckLocomotionAnimation();
        LookAlong(GetTransform(), lookDir);
    }

    void LookAlong(Transform t, Vector3 lookDir)
    {
        if (t == null || _pawnYaw == null)
            return;
        int id = _entity.Identity != null ? _entity.Identity.Id : 0;
        if (id != 0 && CombatFacingService.Instance != null && CombatFacingService.Instance.HasFollow(id))
        {
            // CombatFacingService.ApplyYaw owns this rotation. A stale drive would
            // overwrite it from StepCharInfo when this Update runs later.
            if (_yawDrive != null)
                _yawDrive.Stop();
            return;
        }
        if (lookDir.sqrMagnitude <= 0.0001f)
            return;
        if (_yawDrive == null)
            _yawDrive = _pawnYaw.EnsureDrive(t);
        _pawnYaw.RequestLook(_yawDrive, t, t.position + lookDir, L2PawnYaw.RotationRateYaw);
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

        // Debug.Log(
            // "MONSTER_WALK result=" + (playingExpected ? "OK" : "MISS") +
            // " nick=" + EntityActionCombatLog.NameOf(_entity) +
            // " objectId=" + (_entity.Identity != null ? _entity.Identity.Id : 0) +
            // " expected=" + expected +
            // " actual=" + EntityActionSkill.AnimatorStateName(animator) +
            // " action=" + (_entity.ActionSlot != null ? _entity.ActionSlot.Action.ToString() : "-") +
            // " moving=" + _isMove +
            // " running=" + _entity.Running +
            // " moved=" + moved.ToString("F3") +
            // " distance=" + distanceToPlayer.ToString("F1") +
            // " animator=" + (animator != null) +
            // " enabled=" + (animator != null && animator.enabled) +
            // " speed=" + (animator != null ? animator.speed.ToString("F3") : "-") +
            // " walkBool=" + (animator != null ? animator.GetBool("walk").ToString() : "-") +
            // " runBool=" + (animator != null ? animator.GetBool("run").ToString() : "-") +
            // " hasState=" + (animator != null && animator.HasState(0, Animator.StringToHash(expected))));
    }

    private void CharacterMove(Entity entity , Vector3 direction , float speed)
    {
        CharacterController character = GetControllerToTypeEntity(entity);
        // Match PlayerController: gravity (Y) is integrated separately so the
        // vertical velocity is NOT scaled by run speed. Combining them made the
        // monsters/NPCs bob "light" — the faster they ran, the weaker gravity felt.
        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        float vy = ApplyGravity(Time.deltaTime);
        StartMove(character, planar, speed, vy);
    }

   

    private void StartMove(CharacterController character, Vector3 direction, float speed, float verticalVelocity)
    {
        if (character == null || !character.enabled || !character.gameObject.activeInHierarchy)
            return;

        if (character.radius <= 0.001f || character.height <= 0.001f)
            return;

        Vector3 planar = direction * speed * Time.deltaTime;
        planar.y = verticalVelocity * Time.deltaTime;
        character.Move(planar);
    }

    /// <summary>
    /// Interlude physWalking / physFalling vertical integrator (matches
    /// PlayerController.ApplyGravity). Grounded: small downward bias keeps the
    /// CharacterController touching the floor. Airborne: gravity capped at
    /// PhysicsVolume.TerminalVelocity (2500 UU/s → 47.62 m/s).
    /// </summary>
    private float ApplyGravity(float deltaTime)
    {
        var character = GetControllerToTypeEntity(_entity);

        if (character == null)
            return 0f;

        if (character.isGrounded)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity -= _gravity * deltaTime;
            if (_verticalVelocity < -TerminalVelocity)
                _verticalVelocity = -TerminalVelocity;
        }
        return _verticalVelocity;
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
        if (_entity is UserEntity)
            EntitySpawnShared.ApplyGroundedTransform(_entity.gameObject, dest, _entity.transform.rotation);
        else
            _entity.transform.position = dest;
        SetEventToTypeEntity(_entity);
        EntitySpawnShared.LogStriderGait("arrived", _entity);
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
