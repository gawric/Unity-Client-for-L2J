using UnityEngine;

public sealed class EntityActionMove : IEntityActionProcess
{
    private readonly L2PawnRange _pawnRange;
    private readonly L2PawnYaw _pawnYaw;

    public EntityActionMove(L2PawnRange pawnRange, L2PawnYaw pawnYaw)
    {
        _pawnRange = pawnRange;
        _pawnYaw = pawnYaw;
    }

    public void Enter(Entity entity, object payload)
    {
        if (entity == null || entity.IsDead() || MoveAllCharacters.Instance == null)
            return;

        if (payload is CharMoveToLocationDto moveDto)
        {
            L2PawnRange.TrySnapUserToPacket(entity, moveDto.OldPosition, "MoveTo origin");
            StartMoveToPoint(entity, moveDto.NewPosition);
            return;
        }

        if (payload is Vector3 point)
        {
            StartMoveToPoint(entity, point);
            return;
        }

        if (payload is MoveToPawnDto pawnDto)
            StartMoveToPawn(entity, pawnDto);
    }

    public void Tick(Entity entity)
    {
    }

    void StartMoveToPoint(Entity entity, Vector3 destination)
    {
        if (entity is UserEntity && entity.Identity != null)
            entity.Running = entity.Identity.IsRunning;

        if (entity is UserEntity && GaitLog.Enabled)
            GaitLog.Log(entity, "move-point",
                "running=" + entity.Running +
                " identityRunning=" + entity.Identity.IsRunning);

        float distance = VectorUtils.Distance2D(entity.transform.position, destination);
        MagicGaitTrace.Event(entity, "move-point",
            "dist=" + distance.ToString("F2") +
            " dest=" + destination.x.ToString("F1") + "," + destination.z.ToString("F1"));
        // Entity pawn = EntityPawnLookup.ResolvePawn(entity, destination);
        // EntityActionCombatLog.LogCiPawn(entity,
            // "Move.Point nick=" + EntityActionCombatLog.NameOf(entity) +
            // " dist2d=" + distance.ToString("F2") +
            // " dest=" + EntityActionCombatLog.Vec(destination) +
            // " pawn=" + EntityActionCombatLog.Describe(pawn) +
            // " distToPawn=" + (pawn != null
                // ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2")
                // : "-") +
            // " " + EntityActionCombatLog.ClassifyDest(destination, pawn) +
            // (distance <= 0.12f ? " SKIP_ARRIVED" : " START"));
        // CharInfoSpeedLog.LogMoveStart(entity, "Point", distance,
            // pawn != null ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position) : -1f, 0.1f);
        // CharInfoMoveBudgetLog.StartPoint(entity as UserEntity, destination);
        if (distance <= 0.12f)
        {
            CrasherSlideLog.Event(entity, "move-point-ARRIVED",
                "dist=" + distance.ToString("F2") + " walkRequested=false");
            entity.ActionSlot.PawnDist = 0f;
            L2PawnRange.ClearIgnoredPawn(entity);
            if (entity.Identity != null)
                MoveAllCharacters.Instance.CancelMove(entity.Identity.Id);
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.NotifyArrived(entity);
            return;
        }

        entity.ActionSlot.PawnDist = 0f;
        L2PawnRange.ClearIgnoredPawn(entity);

        entity.ActionSlot.Destination = destination;
        MovementData existing = entity.Identity != null
            ? MoveAllCharacters.Instance.GetMove(entity.Identity.Id)
            : null;
        if (existing != null && existing.IsMove())
        {
            MagicGaitTrace.Decision(entity, "move-point-retarget", "existing.IsMove=true");
            CrasherSlideLog.Event(entity, "move-point-retarget",
                "walkRequested=" + (entity is UserEntity) + " running=" + entity.Running);
            existing.RetargetPoint(destination, entity.Running);
            // Re-apply the locomotion clip. After a target death the actor was
            // switched to wait/idle (EntityActionIdle.Enter → PlayStandWait), which
            // desyncs the visual from MovementData's cached gait. ReplayUserGait
            // forces a fresh StartMove so the run/walk clip plays again.
            if (entity is UserEntity)
                existing.ReplayUserGait();
            return;
        }

        MagicGaitTrace.Decision(entity, "move-point-new",
            "existing=" + (existing != null) + " isMove=" + (existing != null && existing.IsMove()));
        MovementTarget target = new MovementTarget(destination, 0.1f, entity.Running);
        MovementData data = new MovementData(entity, target, _pawnYaw);
        MoveAllCharacters.Instance.AddMoveData(entity.Identity.Id, data);
        if (entity is UserEntity)
        {
            data.SyncUserGait();
        }
        else
        {
            CrasherSlideLog.Event(entity, "move-point-new",
                "walkRequested=true walking=" + (!entity.Running));
            EntityActionVisual.StartMove(entity, !entity.Running);
        }
    }

    void StartMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        Entity pawn = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(dto.TarObjid)
            : null;
        if (pawn == null)
        {
            // EntityActionCombatLog.LogGap(entity, "Move.Pawn ABORT", pawn,
                // " tarId=" + dto.TarObjid +
                // " pawnNull=true");
            return;
        }

        if (pawn.IsDead())
        {
            if (entity.Identity != null)
                MoveAllCharacters.Instance.CancelMove(entity.Identity.Id);
            return;
        }

        if (entity.Identity != null)
            entity.Running = entity.Identity.IsRunning;

        entity.AttackTarget = pawn.transform;
        entity.ActionSlot.Target = pawn;
        L2PawnRange.TrySnapUserToPacket(entity, dto.ObjPos, "MoveToPawn origin");

        float stopDistance = dto.Distance > 0.01f ? dto.Distance : 0.1f;
        entity.ActionSlot.PawnDist = stopDistance;
        L2PawnRange.IgnorePawnCollision(entity, pawn);

        float toPawn = VectorUtils.Distance2D(entity.transform.position, pawn.transform.position);
        bool inRange = toPawn <= stopDistance;

        if (entity is UserEntity && GaitLog.Enabled)
            GaitLog.Log(entity, "move-pawn",
                "running=" + entity.Running +
                " identityRunning=" + (entity.Identity != null ? entity.Identity.IsRunning : false) +
                " toPawn=" + toPawn.ToString("F2") +
                " stop=" + stopDistance.ToString("F2") +
                " inRange=" + inRange +
                " inCombat=" + entity.InCombat);

        MagicGaitTrace.Event(entity, "move-pawn",
            "toPawn=" + toPawn.ToString("F2") +
            " stop=" + stopDistance.ToString("F2") +
            " inRange=" + inRange +
            " tarId=" + dto.TarObjid);

        if (inRange)
        {
            CrasherSlideLog.Event(entity, "move-pawn-INRANGE",
                "toPawn=" + toPawn.ToString("F2") +
                " stop=" + stopDistance.ToString("F2") +
                " walkRequested=false");
            MagicGaitTrace.Event(entity, "move-pawn-INRANGE→NotifyArrived",
                "toPawn=" + toPawn.ToString("F2") + " stop=" + stopDistance.ToString("F2"));
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.NotifyArrived(entity);
            return;
        }

        entity.ActionSlot.Destination = _pawnRange.StopPointOnDistRing(
            entity.transform.position, pawn.transform.position, stopDistance);

        // Reuse any prior MovementData (even after CancelMove from an in-range
        // NotifyArrived). Recreating + ForceReplay every out-of-range packet was
        // restarting MMagic run mid-stride on the chase boundary.
        MovementData existing = entity.Identity != null
            ? MoveAllCharacters.Instance.GetMove(entity.Identity.Id)
            : null;
        if (existing != null)
        {
            MagicGaitTrace.Decision(entity, "move-pawn-retarget",
                "reuse existing isMove=" + existing.IsMove());
            CrasherSlideLog.Event(entity, "move-pawn-retarget",
                "isMove=" + existing.IsMove() +
                " walkRequested=" + (entity is UserEntity) +
                " toPawn=" + toPawn.ToString("F2"));
            existing.RetargetPawn(pawn.transform, stopDistance, entity.Running);
            if (entity is UserEntity)
                existing.ReplayUserGait();
            return;
        }

        MagicGaitTrace.Decision(entity, "move-pawn-new", "no existing MovementData");
        CrasherSlideLog.Event(entity, "move-pawn-new",
            "toPawn=" + toPawn.ToString("F2") +
            " stop=" + stopDistance.ToString("F2") +
            " walkRequested=" + (entity is UserEntity));
        MovementTarget target = new MovementTarget(pawn.transform, stopDistance, entity.Running);
        MovementData data = new MovementData(entity, target, _pawnYaw);
        MoveAllCharacters.Instance.AddMoveData(entity.Identity.Id, data);
        // Non-forcing: SyncUserGait OffGait force-replays once if still on wait.
        // Unconditional ForceReplay restarted the run clip every new chase packet.
        data.SyncUserGait();
    }
}
