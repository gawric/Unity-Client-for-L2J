using UnityEngine;

public sealed class EntityActionIdle : IEntityActionProcess
{
    public void Enter(Entity entity, object payload)
    {
        if (entity != null && entity.IsDead())
            return;
        if (EntityActionMachine.IsFinishingSwing(entity))
            return;
        string restore = payload as string;
        if (!string.IsNullOrEmpty(restore) &&
            (entity is MonsterEntity || entity is NpcEntity))
        {
            CrasherSlideLog.Event(entity, "Idle.Restore", "state=" + restore);
            EntityActionVisual.RestoreLocomotion(entity, restore);
            return;
        }
        MagicGaitTrace.Event(entity, "Idle.Enter→PlayStandWait",
            "inCombat=" + entity.InCombat + " payload=" + (restore ?? "-"));
        CrasherSlideLog.Event(entity, "Idle.Enter",
            "inCombat=" + entity.InCombat + " → " + (entity.InCombat ? "atkwait" : "wait"));
        EntityActionVisual.PlayStandWait(entity);
    }

    public void Tick(Entity entity)
    {
    }
}
