using UnityEngine;

public sealed class EntityActionIdle : IEntityActionProcess
{
    public void Enter(Entity entity, object payload)
    {
        if (entity != null && entity.IsDead())
            return;
        if (EntityActionMachine.IsFinishingSwing(entity))
            return;
        EntityActionCombatLog.LogCiPawn(entity,
            "Idle.Enter PlayStandWait nick=" + EntityActionCombatLog.NameOf(entity) +
            " inCombat=" + entity.InCombat +
            " " + EntityActionCombatLog.AnimDump(entity) +
            " " + EntityActionCombatLog.VisualDump(entity));
        EntityActionVisual.PlayStandWait(entity);
    }

    public void Tick(Entity entity)
    {
    }
}
