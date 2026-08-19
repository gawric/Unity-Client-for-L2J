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
        EntityActionVisual.StartMove(entity, !entity.Running);
        MovementTarget target = new MovementTarget(destination, 0.1f, entity.Running);
        MoveAllCharacters.Instance.AddMoveData(entity.Identity.Id, new MovementData(entity, target));
    }

    void StartMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        Entity pawn = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(dto.TarObjid)
            : null;
        if (pawn == null || pawn.IsDead())
            return;

        entity.Running = true;
        entity.AttackTarget = pawn.transform;
        entity.ActionSlot.Target = pawn;

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
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.NotifyArrived(entity);
            return;
        }

        EntityActionVisual.StartMove(entity, false);
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
        MovementTarget target = new MovementTarget(pawn, stopDistance);
        MoveAllCharacters.Instance.AddMoveData(entity.Identity.Id, new MovementData(entity, target));
    }
}
