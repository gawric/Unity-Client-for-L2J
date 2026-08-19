using UnityEngine;

public sealed class EntityActionAttack : IEntityActionProcess
{
    public void Enter(Entity entity, object payload)
    {
        if (entity == null || entity.IsDead())
            return;

        EntityActionVisual.CancelMove(entity);

        if (payload is ReviveDto)
        {
            EntityActionVisual.PlayRevive(entity);
            return;
        }

        if (payload is AttackDto attack)
        {
            Entity target = IncomingPacketActions.GameWorld != null
                ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(attack.TargetId)
                : null;
            if (target == null || target.IsDead())
            {
                EntityActionCombatLog.LogIfWatch(entity, target,
                    "Attack.Enter SKIP swing attacker=" + EntityActionCombatLog.NameOf(entity) +
                    " target=" + EntityActionCombatLog.NameOf(target) +
                    " targetNull=" + (target == null) +
                    " targetDead=" + (target != null && target.IsDead()));
                return;
            }

            entity.InCombat = true;
            entity.AttackTarget = target.transform;
            entity.ActionSlot.Target = target;
            EntityActionVisual.FaceTowards(entity, target.transform.position);
            if (entity is MonsterEntity && IncomingPacketActions.Moves != null)
                IncomingPacketActions.Moves.AddRotate(entity.Identity.Id, new RotateData(target, entity));
            EntityActionCombatLog.Watch(entity);
            EntityActionCombatLog.LogCiPawn(entity,
                "Attack.Enter nick=" + EntityActionCombatLog.NameOf(entity) +
                " target=" + EntityActionCombatLog.Describe(target) +
                " dist2d=" + VectorUtils.Distance2D(entity.transform.position, target.transform.position).ToString("F2") +
                " now=" + EntityActionCombatLog.Vec(entity.transform.position) +
                " pawnPos=" + EntityActionCombatLog.Vec(target.transform.position) +
                " dest=" + EntityActionCombatLog.Vec(entity.ActionSlot.Destination) +
                " destToPawn=" + VectorUtils.Distance2D(entity.ActionSlot.Destination, target.transform.position).ToString("F2") +
                " " + EntityActionCombatLog.ClassifyDest(entity.transform.position, target) +
                EntityActionCombatLog.AttackDump(entity, target));
            EntityActionCombatLog.LogIfWatch(entity, target,
                "Attack.Enter PLAY atk01 attacker=" + EntityActionCombatLog.Describe(entity) +
                " target=" + EntityActionCombatLog.Describe(target));
            EntityActionVisual.PlayMeleeAttack(entity);
            if (HitManager.Instance != null)
                HitManager.Instance.ArmRemoteMeleeHit(entity, target, attack);
            return;
        }

        if (payload is MagicSkillUseDto magic)
        {
            entity.InCombat = true;
            Entity target = IncomingPacketActions.GameWorld != null
                ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(magic.TargetId)
                : null;
            if (target != null)
            {
                entity.AttackTarget = target.transform;
                entity.ActionSlot.Target = target;
                EntityActionVisual.FaceTowards(entity, target.transform.position);
            }

            if (magic.SkillGrp != null && magic.SkillGrp.IsMagic != 1)
                EntityActionVisual.PlayPhysicalSkill(entity, magic);

            EffectManager effects = IncomingPacketActions.Effects;
            if (effects != null)
                effects.PlayEffect(magic.SkillId, entity.transform, entity.GetMagicCastData());
        }
    }

    public void Tick(Entity entity)
    {
        if (entity == null || entity.IsDead())
            return;

        UserEntity user = entity as UserEntity;
        if (user != null && user.IsAttackVisualPlaying())
            return;

        Entity target = entity.ActionSlot.Target;
        if (target == null && entity.AttackTarget != null)
            target = entity.AttackTarget.GetComponent<Entity>();
        if (target != null && target.IsDead())
        {
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.CancelAttackKeepStance(entity);
            return;
        }

        if (user == null || !user.ShouldReturnFromAttack())
            return;

        EntityActionCombatLog.LogCiPawn(entity,
            "Attack.Tick→Idle nick=" + EntityActionCombatLog.NameOf(entity) +
            EntityActionCombatLog.IdleFromSwingDump(entity));
        user.ClearAttackVisual();
        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Set(entity, EntityActionKind.Idle, null);
    }
}
