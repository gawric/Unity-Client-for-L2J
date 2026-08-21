using UnityEngine;

public sealed class EntityActionStop : IEntityActionProcess
{
    private readonly L2PawnRange _pawnRange;

    public EntityActionStop(L2PawnRange pawnRange)
    {
        _pawnRange = pawnRange;
    }

    public void Enter(Entity entity, object payload)
    {
        if (entity == null || entity.IsDead())
            return;

        EntityActionVisual.CancelMove(entity);
        EntityActionVisual.PlayStandWait(entity);

        L2PawnRange.ClearIgnoredPawn(entity);

        StopMoveDto dto = payload as StopMoveDto;
        if (dto == null)
            return;

        Vector3 stop = GroundSnapHelper.SnapToGroundOrKeep(dto.StopPos);
        float snapD = VectorUtils.Distance2D(entity.transform.position, stop);
        Entity pawn = EntityActionCombatLog.PawnOf(entity);
        bool skipSnap = _pawnRange != null && _pawnRange.ShouldSkipCharInfoPositionSnap(entity, stop);
        EntityActionCombatLog.LogCiPawn(entity,
            "Stop.Enter nick=" + EntityActionCombatLog.NameOf(entity) +
            " snapD=" + snapD.ToString("F2") +
            " bow=" + (_pawnRange != null && _pawnRange.IsBow(entity)) +
            " stop=" + EntityActionCombatLog.Vec(stop) +
            " now=" + EntityActionCombatLog.Vec(entity.transform.position) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " nowToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2")
                : "-") +
            " stopToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(stop, pawn.transform.position).ToString("F2")
                : "-") +
            (skipSnap ? " SKIP_SNAP" : " SNAP"));
        if (skipSnap)
        {
            if (pawn != null && VectorUtils.Distance2D(entity.transform.position, pawn.transform.position) >= 2f)
                EntityActionCombatLog.LogGap(entity, "Stop SKIP_SNAP_FAR", pawn,
                    " snapD=" + snapD.ToString("F2") +
                    " stop=" + EntityActionCombatLog.Vec(stop));
            return;
        }

        entity.Identity.Position = stop;
        EntitySpawnShared.ApplyGroundedTransform(entity.gameObject, stop, entity.transform.rotation);
    }

    public void Tick(Entity entity)
    {
    }
}
