using UnityEngine;

public sealed class EntityActionMove : IEntityActionProcess
{
    private readonly L2PawnRange _pawnRange;

    public EntityActionMove(L2PawnRange pawnRange)
    {
        _pawnRange = pawnRange;
    }

    public void Enter(Entity entity, object payload)
    {
        if (entity == null || entity.IsDead() || MoveAllCharacters.Instance == null)
            return;

        if (payload is CharMoveToLocationDto moveDto)
        {
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

        float distance = VectorUtils.Distance2D(entity.transform.position, destination);
        Entity pawn = EntityActionCombatLog.ResolvePawn(entity, destination);
        EntityActionCombatLog.LogCiPawn(entity,
            "Move.Point nick=" + EntityActionCombatLog.NameOf(entity) +
            " dist2d=" + distance.ToString("F2") +
            " dest=" + EntityActionCombatLog.Vec(destination) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " distToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2")
                : "-") +
            " " + EntityActionCombatLog.ClassifyDest(destination, pawn) +
            (distance <= 0.12f ? " SKIP_ARRIVED" : " START"));
        CharInfoSpeedLog.LogMoveStart(entity, "Point", distance,
            pawn != null ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position) : -1f, 0.1f);
        CharInfoMoveBudgetLog.StartPoint(entity as UserEntity, destination);
        if (distance <= 0.12f)
        {
            entity.ActionSlot.PawnDist = 0f;
            L2PawnRange.ClearIgnoredPawn(entity);
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.NotifyArrived(entity);
            return;
        }

        entity.ActionSlot.PawnDist = 0f;
        L2PawnRange.ClearIgnoredPawn(entity);

        entity.ActionSlot.Destination = destination;
        MovementTarget target = new MovementTarget(destination, 0.1f, entity.Running);
        MovementData data = new MovementData(entity, target);
        MoveAllCharacters.Instance.AddMoveData(entity.Identity.Id, data);
        if (entity is UserEntity)
            data.SyncUserGait();
        else
            EntityActionVisual.StartMove(entity, !entity.Running);
    }

    void StartMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        Entity pawn = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(dto.TarObjid)
            : null;
        if (pawn == null || pawn.IsDead())
        {
            EntityActionCombatLog.LogGap(entity, "Move.Pawn ABORT", pawn,
                " tarId=" + dto.TarObjid +
                " pawnNull=" + (pawn == null) +
                " pawnDead=" + (pawn != null && pawn.IsDead()));
            return;
        }

        entity.Running = true;
        entity.AttackTarget = pawn.transform;
        entity.ActionSlot.Target = pawn;
        L2PawnRange.TrySnapUserToPacket(entity, dto.ObjPos, "MoveToPawn origin");

        float stopDistance = dto.Distance > 0.01f ? dto.Distance : 0.1f;
        entity.ActionSlot.PawnDist = stopDistance;
        L2PawnRange.IgnorePawnCollision(entity, pawn);

        float toPawn = VectorUtils.Distance2D(entity.transform.position, pawn.transform.position);
        if (toPawn <= stopDistance)
        {
            EntityActionCombatLog.MarkChaseStart(entity, pawn, entity.transform.position);
            EntityActionCombatLog.LogCiPawn(entity,
                "Move.Pawn FOLLOW SKIP_IN_RANGE nick=" + EntityActionCombatLog.NameOf(entity) +
                " pawn=" + EntityActionCombatLog.Describe(pawn) +
                " stopDist=" + stopDistance.ToString("F2") +
                " toPawn=" + toPawn.ToString("F2") +
                " pawnMoving=" + EntityActionCombatLog.IsPawnMoving(pawn));
            EntityActionCombatLog.LogGap(entity, "Move.Pawn SKIP_IN_RANGE", pawn,
                " stopDist=" + stopDistance.ToString("F2") +
                " pktOrigin=" + EntityActionCombatLog.Vec(dto.ObjPos) +
                " originToPawn=" + VectorUtils.Distance2D(dto.ObjPos, pawn.transform.position).ToString("F2"));
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.NotifyArrived(entity);
            return;
        }

        entity.ActionSlot.Destination = _pawnRange.StopPointOnDistRing(
            entity.transform.position, pawn.transform.position, stopDistance);
        EntityActionCombatLog.MarkChaseStart(entity, pawn, entity.ActionSlot.Destination);
        EntityActionCombatLog.LogCiPawn(entity,
            "Move.Pawn FOLLOW START nick=" + EntityActionCombatLog.NameOf(entity) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " stopDist=" + stopDistance.ToString("F2") +
            " toPawn=" + toPawn.ToString("F2") +
            " destHint=" + EntityActionCombatLog.Vec(entity.ActionSlot.Destination) +
            " destHintToPawn=" + VectorUtils.Distance2D(
                entity.ActionSlot.Destination, pawn.transform.position).ToString("F2") +
            " pawnMoving=" + EntityActionCombatLog.IsPawnMoving(pawn) +
            " pawnPos=" + EntityActionCombatLog.Vec(pawn.transform.position));
        CharInfoSpeedLog.LogMoveStart(entity, "Pawn", toPawn, toPawn, stopDistance);
        CharInfoMoveBudgetLog.StartPawn(entity as UserEntity, pawn, dto, stopDistance);
        MovementTarget target = new MovementTarget(pawn, stopDistance);
        MovementData data = new MovementData(entity, target);
        MoveAllCharacters.Instance.AddMoveData(entity.Identity.Id, data);
        data.SyncUserGait();
    }
}
